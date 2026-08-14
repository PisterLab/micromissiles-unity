import sys
import tempfile
import unittest
from pathlib import Path

TOOLS_DIR = Path(__file__).resolve().parent
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

import run_batch
from Configs import communication_config_pb2, run_config_pb2, static_config_pb2


class RunBatchTest(unittest.TestCase):

    def test_plans_every_scenario_for_every_seed(self):
        run_config = run_config_pb2.RunConfig(
            name="latency_sweep",
            simulation_config_file="simulation.pbtxt",
            num_runs=2,
            seed=100,
            seed_stride=1,
            max_parallel=8,
        )
        for name, latency in (("ideal", 0.0), ("delayed", 0.25)):
            scenario = run_config.communication_scenarios.add(name=name)
            scenario.communication_config.link_config.latency_seconds = latency
            scenario.communication_config.link_config.packet_delivery_ratio = 1.0

        delayed_config = run_config.communication_scenarios[
            1].communication_config
        link_override = delayed_config.link_overrides.add()
        setattr(link_override, "from", static_config_pb2.VESSEL)
        link_override.to = static_config_pb2.CARRIER_INTERCEPTOR
        link_override.link_config.latency_seconds = 0.5
        link_override.link_config.packet_delivery_ratio = 1.0

        run_batch._validate_run_config(run_config)
        with tempfile.TemporaryDirectory() as temp_dir:
            batch_output_dir = Path(temp_dir) / "batch"
            batch_output_dir.mkdir()
            run_batch._write_communication_scenarios(run_config,
                                                     batch_output_dir)
            descriptors = run_batch._plan_run_descriptors(
                run_config, batch_output_dir)

            self.assertEqual(len(descriptors), 4)
            self.assertEqual([descriptor.seed for descriptor in descriptors],
                             [100, 101, 100, 101])
            self.assertEqual(
                descriptors[2].output_dir,
                batch_output_dir / "delayed" / "run_1_seed_100",
            )
            override_path = descriptors[2].communication_config_path
            self.assertTrue(override_path.is_file())
            parsed_config = communication_config_pb2.CommunicationConfig.FromString(
                override_path.read_bytes())
            self.assertAlmostEqual(parsed_config.link_config.latency_seconds,
                                   0.25)
            self.assertEqual(len(parsed_config.link_overrides), 1)

            command = run_batch._build_worker_command(
                Path("/tmp/micromissiles"), descriptors[2],
                Path(temp_dir) / "unity_logs")
            self.assertIn("--communication_config_override", command)
            self.assertIn(str(override_path), command)
            self.assertEqual(run_batch._compute_max_parallel(run_config), 4)

    def test_no_scenarios_preserves_existing_batch_layout(self):
        run_config = run_config_pb2.RunConfig(
            name="existing",
            simulation_config_file="simulation.pbtxt",
            num_runs=1,
            seed=7,
            max_parallel=1,
        )

        run_batch._validate_run_config(run_config)
        batch_output_dir = Path("/tmp/batch")
        descriptors = run_batch._plan_run_descriptors(run_config,
                                                      batch_output_dir)

        self.assertEqual(len(descriptors), 1)
        self.assertEqual(descriptors[0].output_dir,
                         batch_output_dir / "run_1_seed_7")
        self.assertIsNone(descriptors[0].communication_config_path)


if __name__ == "__main__":
    unittest.main()
