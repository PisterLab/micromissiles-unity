"""Launches a batch of deterministic Unity simulation runs in parallel."""

import datetime
import math
import os
import platform
import re
import subprocess
import sys
import time
from dataclasses import dataclass
from pathlib import Path

import google.protobuf.text_format
import unity_utils
from absl import app, flags, logging

# Generated Python protobuf modules import their dependencies from this directory.
PB_DIR = Path(__file__).resolve().parent / "pb"
if str(PB_DIR) not in sys.path:
    sys.path.insert(0, str(PB_DIR))

from Configs import run_config_pb2

# Path to the repository root.
REPO_ROOT = Path(__file__).resolve().parents[1]

# Directory with the run configurations.
RUN_CONFIG_DIR = REPO_ROOT / "Assets/StreamingAssets/Configs/Runs"

# Communication scenario names are also used as output directory names.
SCENARIO_NAME_PATTERN = re.compile(r"^[A-Za-z0-9][A-Za-z0-9._-]*$")

FLAGS = flags.FLAGS


@dataclass(frozen=True)
class RunDescriptor:
    """Description of a deterministic simulation run.

    Attributes:
        run_index: Run index.
        seed: Seed.
        simulation_config_file: Simulation configuration file.
        output_dir: Output directory.
        communication_scenario_name: Optional communication scenario name.
        communication_config_path: Optional serialized communication configuration override.
    """

    run_index: int
    seed: int
    simulation_config_file: str
    output_dir: Path
    communication_scenario_name: str | None
    communication_config_path: Path | None


def _resolve_binary_path(path: str) -> Path:
    """Resolves the path to the Unity executable.

    Args:
        path: Path to the Unity executable or the macOS app bundle.

    Returns:
        The absolute resolved path to the Unity executable.

    Raises:
        FileNotFoundError: If the Unity executable cannot be found.
    """
    binary_path = Path(path).expanduser()
    if binary_path.suffix == ".app" and binary_path.is_dir():
        executable_dir = binary_path / "Contents/MacOS"
        executable = executable_dir / binary_path.stem
        if executable.exists():
            return executable.resolve()

        executables = [
            child for child in executable_dir.iterdir()
            if child.is_file() and os.access(child, os.X_OK)
        ]
        if len(executables) == 1:
            return executables[0].resolve()
        raise FileNotFoundError(
            f"Unable to resolve Unity binary inside app bundle: {binary_path}.")

    if not binary_path.exists():
        raise FileNotFoundError(f"Unity binary not found: {path}.")
    return binary_path.resolve()


def _resolve_run_config_path(path: str) -> Path:
    """Resolves the path to a run configuration file.

    Args:
        path: Run configuration path or filename.

    Returns:
        The absolute resolved path to the run configuration file.

    Raises:
        FileNotFoundError: If the run configuration file cannot be found.
    """
    run_config_path = Path(path).expanduser()
    if run_config_path.exists():
        return run_config_path.resolve()
    run_config_path = RUN_CONFIG_DIR / path
    if run_config_path.exists():
        return run_config_path.resolve()
    raise FileNotFoundError(f"Run configuration file not found: {path}.")


def _parse_run_config(path: Path) -> run_config_pb2.RunConfig:
    """Parses a run configuration proto from text format.

    Args:
        path: Path to the run configuration proto in text format.

    Returns:
        The parsed run configuration proto.
    """
    run_config = run_config_pb2.RunConfig()
    return google.protobuf.text_format.Parse(path.read_text(), run_config)


def _validate_link_config(link_config, label: str) -> None:
    """Rejects invalid link values before any worker processes are launched."""
    if (not math.isfinite(link_config.latency_seconds) or
            link_config.latency_seconds < 0):
        raise ValueError(f"{label} latency_seconds must be non-negative.")
    if (not math.isfinite(link_config.latency_std_seconds) or
            link_config.latency_std_seconds < 0):
        raise ValueError(f"{label} latency_std_seconds must be non-negative.")
    if (not math.isfinite(link_config.packet_delivery_ratio) or
            not 0 <= link_config.packet_delivery_ratio <= 1):
        raise ValueError(f"{label} packet_delivery_ratio must be in [0, 1].")


def _validate_run_config(run_config: run_config_pb2.RunConfig) -> None:
    """Validates batch fields and scenarios before creating output directories.

    Scenario names become directory names, and link pairs use first-match routing in
    Unity, so names must be safe and each directional pair must be unique.
    """
    if not run_config.name:
        raise ValueError("Run configuration name must not be empty.")
    if not run_config.simulation_config_file:
        raise ValueError("Simulation configuration file must not be empty.")
    if run_config.num_runs == 0:
        raise ValueError("num_runs must be greater than zero.")

    scenario_names = set()
    for scenario in run_config.communication_scenarios:
        if not SCENARIO_NAME_PATTERN.fullmatch(scenario.name):
            raise ValueError(
                "Communication scenario names must contain only letters, "
                f"numbers, '.', '_' or '-': {scenario.name!r}.")
        if scenario.name in scenario_names:
            raise ValueError(
                f"Duplicate communication scenario name: {scenario.name}.")
        scenario_names.add(scenario.name)

        if not scenario.HasField("communication_config"):
            raise ValueError(
                f"Communication scenario {scenario.name} has no configuration.")
        communication_config = scenario.communication_config
        if not communication_config.HasField("link_config"):
            raise ValueError(
                f"Communication scenario {scenario.name} has no default link_config."
            )
        _validate_link_config(
            communication_config.link_config,
            f"Communication scenario {scenario.name} default link",
        )

        link_pairs = set()
        for link_override in communication_config.link_overrides:
            sender_type = getattr(link_override, "from")
            receiver_type = link_override.to
            if sender_type == 0 or receiver_type == 0:
                raise ValueError(
                    f"Communication scenario {scenario.name} contains an invalid link pair."
                )
            link_pair = (sender_type, receiver_type)
            if link_pair in link_pairs:
                raise ValueError(
                    f"Communication scenario {scenario.name} contains a duplicate link pair."
                )
            link_pairs.add(link_pair)
            if not link_override.HasField("link_config"):
                raise ValueError(
                    f"Communication scenario {scenario.name} contains a link override "
                    "without link_config.")
            _validate_link_config(
                link_override.link_config,
                f"Communication scenario {scenario.name} link override",
            )


def _communication_config_path(batch_output_dir: Path,
                               scenario_name: str) -> Path:
    """Returns the shared binary override path used by every run in a scenario."""
    return batch_output_dir / "_communication_scenarios" / f"{scenario_name}.pb"


def _write_communication_scenarios(
    run_config: run_config_pb2.RunConfig,
    batch_output_dir: Path,
) -> None:
    """Writes binary worker inputs and matching text files for reproducibility.

    Each scenario is serialized once and shared read-only by all of its workers.
    """
    if not run_config.communication_scenarios:
        return

    scenario_dir = batch_output_dir / "_communication_scenarios"
    scenario_dir.mkdir()
    for scenario in run_config.communication_scenarios:
        binary_path = _communication_config_path(batch_output_dir,
                                                 scenario.name)
        binary_path.write_bytes(
            scenario.communication_config.SerializeToString())
        text_path = binary_path.with_suffix(".pbtxt")
        text_path.write_text(
            google.protobuf.text_format.MessageToString(
                scenario.communication_config),
            encoding="utf-8",
        )


def _plan_run_descriptors(
    run_config: run_config_pb2.RunConfig,
    batch_output_dir: Path,
) -> list[RunDescriptor]:
    """Expands the Cartesian product of communication scenarios and seeded runs.

    When no scenarios are configured, one implicit scenario preserves the original
    output layout and uses the communication settings embedded in the simulation.

    Args:
        run_config: Run configuration.
        batch_output_dir: Batch output directory.

    Returns:
        The list of run descriptors.
    """
    descriptors = []
    scenarios = list(run_config.communication_scenarios) or [None]
    for scenario in scenarios:
        scenario_name = scenario.name if scenario is not None else None
        communication_config_path = (_communication_config_path(
            batch_output_dir, scenario_name)
                                     if scenario_name is not None else None)
        output_root = (batch_output_dir / scenario_name
                       if scenario_name is not None else batch_output_dir)
        for run_index in range(1, run_config.num_runs + 1):
            seed = run_config.seed + ((run_index - 1) * run_config.seed_stride)
            descriptors.append(
                RunDescriptor(
                    run_index=run_index,
                    seed=seed,
                    simulation_config_file=(run_config.simulation_config_file),
                    output_dir=(output_root / f"run_{run_index}_seed_{seed}"),
                    communication_scenario_name=scenario_name,
                    communication_config_path=communication_config_path,
                ))
    return descriptors


def _compute_max_parallel(run_config: run_config_pb2.RunConfig) -> int:
    """Computes the number of worker processes to run concurrently.

    Args:
        run_config: Run configuration.

    Returns:
        The maximum number of concurrent worker processes.
    """
    # Apply one global process cap across the fully expanded scenario-by-seed plan.
    # If max_parallel is unset, it will read 0. In that case, default to 16.
    requested_parallel = run_config.max_parallel if run_config.max_parallel > 0 else 16
    num_scenarios = max(1, len(run_config.communication_scenarios))
    return min(requested_parallel, run_config.num_runs * num_scenarios)


def _build_worker_command(
    binary_path: Path,
    descriptor: RunDescriptor,
    unity_log_dir: Path,
) -> list[str]:
    """Builds one worker command with its optional communication override.

    Args:
        binary_path: Path to the Unity executable.
        descriptor: Run descriptor for this worker.
        unity_log_dir: Directory in which Unity logs are stored.

    Returns:
        The command-line argument vector for subprocess.Popen().
    """
    unity_log_root = (unity_log_dir / descriptor.communication_scenario_name
                      if descriptor.communication_scenario_name is not None else
                      unity_log_dir)
    unity_log_root.mkdir(parents=True, exist_ok=True)
    unity_log_path = (unity_log_root /
                      f"run_{descriptor.run_index}_seed_{descriptor.seed}.log")
    command = [
        str(binary_path),
        "--simulation_config",
        descriptor.simulation_config_file,
        "--seed",
        str(descriptor.seed),
        "--output_dir",
        str(descriptor.output_dir),
        "-batchmode",
        "-nographics",
        "-logFile",
        str(unity_log_path),
    ]
    if descriptor.communication_config_path is not None:
        command.extend([
            "--communication_config_override",
            str(descriptor.communication_config_path),
        ])
    return command


def _terminate_processes(processes: list[subprocess.Popen[bytes]]) -> None:
    """Terminates all worker processes.

    Args:
        processes: Running worker subprocesses to terminate.
    """
    for process in processes:
        if process.poll() is None:
            process.terminate()

    deadline = time.monotonic() + 5.0
    for process in processes:
        if process.poll() is not None:
            continue
        remaining = deadline - time.monotonic()
        if remaining <= 0:
            break
        try:
            process.wait(timeout=remaining)
        except subprocess.TimeoutExpired:
            continue

    for process in processes:
        if process.poll() is None:
            process.kill()


def run_batch(
    binary_path: Path,
    descriptors: list[RunDescriptor],
    unity_log_dir: Path,
    max_parallel: int,
) -> None:
    """Executes the planned batch of runs.

    Args:
        binary_path: Path to the Unity executable.
        descriptors: Planned run descriptors.
        unity_log_dir: Directory for Unity log files.
        max_parallel: Maximum number of concurrent workers.

    Raises:
        FileExistsError: If a run output directory already exists.
        RuntimeError: If a worker exits unsuccessfully or fails to
            produce output.
    """
    next_descriptor_index = 0
    running_workers: dict[
        int,
        tuple[RunDescriptor, subprocess.Popen[bytes]],
    ] = {}

    try:
        while next_descriptor_index < len(descriptors) or running_workers:
            while (next_descriptor_index < len(descriptors) and
                   len(running_workers) < max_parallel):
                worker_id = next_descriptor_index
                descriptor = descriptors[next_descriptor_index]
                next_descriptor_index += 1

                if descriptor.output_dir.exists():
                    raise FileExistsError(
                        "Run output directory already exists: "
                        f"{descriptor.output_dir}.")

                command = _build_worker_command(
                    binary_path,
                    descriptor,
                    unity_log_dir,
                )

                logging.info(
                    "Launching worker %d/%d for scenario %s, run %d, seed %d.",
                    worker_id + 1,
                    len(descriptors),
                    descriptor.communication_scenario_name or "embedded",
                    descriptor.run_index,
                    descriptor.seed,
                )

                running_workers[worker_id] = (
                    descriptor,
                    subprocess.Popen(command),
                )

            finished_worker_id = None
            for worker_id, (descriptor, process) in running_workers.items():
                exit_code = process.poll()
                if exit_code is None:
                    continue

                if exit_code != 0:
                    raise RuntimeError(
                        f"Worker {worker_id + 1} exited with code {exit_code}.")
                if not descriptor.output_dir.is_dir():
                    raise RuntimeError(
                        f"Worker {worker_id + 1} did not create an output directory."
                    )
                logging.info(
                    "Completed scenario %s, run %d with seed %d.",
                    descriptor.communication_scenario_name or "embedded",
                    descriptor.run_index,
                    descriptor.seed,
                )

                finished_worker_id = worker_id
                break

            if finished_worker_id is None:
                time.sleep(0.1)
                continue

            del running_workers[finished_worker_id]
    except BaseException:
        _terminate_processes(
            [process for _, process in running_workers.values()])
        raise


def main(argv):
    assert len(argv) == 1, argv

    binary_path = _resolve_binary_path(FLAGS.binary_path)
    run_config_path = _resolve_run_config_path(FLAGS.run_config)
    run_config = _parse_run_config(run_config_path)
    _validate_run_config(run_config)

    # Initialize the log directories.
    timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
    log_root_dir = Path(FLAGS.log_root_dir).expanduser().resolve()
    batch_output_dir = log_root_dir / f"{run_config.name}_{timestamp}"
    unity_log_dir = (Path(FLAGS.unity_log_dir).expanduser().resolve()
                     if FLAGS.unity_log_dir is not None else
                     (log_root_dir / f"{batch_output_dir.name}_unity_logs"))
    batch_output_dir.mkdir(parents=True, exist_ok=False)
    unity_log_dir.mkdir(parents=True, exist_ok=False)

    # Persist the exact communication inputs passed to the standalone workers.
    _write_communication_scenarios(run_config, batch_output_dir)

    # Plan all of the runs to execute.
    descriptors = _plan_run_descriptors(
        run_config,
        batch_output_dir,
    )

    # Compute the maximum number of parallel executions.
    max_parallel = _compute_max_parallel(run_config)

    logging.info("Launching %d runs from %s.", len(descriptors),
                 run_config_path)
    logging.info(
        "Batch output directory: %s.",
        batch_output_dir,
    )
    run_batch(
        binary_path,
        descriptors,
        unity_log_dir,
        max_parallel,
    )

    logging.info("All %d runs completed successfully.", len(descriptors))


if __name__ == "__main__":
    flags.DEFINE_string("binary_path", None, "Path to the Unity executable.")
    flags.DEFINE_string("run_config", None,
                        "Run configuration path or filename.")
    flags.DEFINE_string(
        "log_root_dir", unity_utils.get_persistent_data_directory(),
        "Root directory in which to create the batch output directory.")
    flags.DEFINE_string(
        "unity_log_dir", None,
        "Directory in which to store the per-run Unity logs. "
        "Defaults to a sibling directory next to the batch output directory.")
    flags.mark_flags_as_required(["binary_path", "run_config"])

    app.run(main)
