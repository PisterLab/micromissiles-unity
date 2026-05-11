"""Launches a batch of deterministic Unity simulation runs in parallel."""

import datetime
import os
import platform
import subprocess
import time
from dataclasses import dataclass
from pathlib import Path

import google.protobuf.text_format
import unity_utils
from absl import app, flags, logging
from pb import run_config_pb2

# Path to the repository root.
REPO_ROOT = Path(__file__).resolve().parents[1]

# Directory with the run configurations.
RUN_CONFIG_DIR = REPO_ROOT / "Assets/StreamingAssets/Configs/Runs"

FLAGS = flags.FLAGS


@dataclass(frozen=True)
class RunDescriptor:
    """Description of a deterministic simulation run.

    Attributes:
        run_index: Run index.
        seed: Seed.
        simulation_config_file: Simulation configuration file.
        output_dir: Output directory.
    """

    run_index: int
    seed: int
    simulation_config_file: str
    output_dir: Path


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


def _plan_run_descriptors(
    run_config: run_config_pb2.RunConfig,
    batch_output_dir: Path,
) -> list[RunDescriptor]:
    """Returns the deterministic plan of runs to execute.

    Args:
        run_config: Run configuration.
        batch_output_dir: Batch output directory.

    Returns:
        The list of run descriptors.
    """
    descriptors = []
    for run_index in range(1, run_config.num_runs + 1):
        seed = run_config.seed + ((run_index - 1) * run_config.seed_stride)
        descriptors.append(
            RunDescriptor(
                run_index=run_index,
                seed=seed,
                simulation_config_file=(run_config.simulation_config_file),
                output_dir=batch_output_dir / (f"run_{run_index}_seed_{seed}"),
            ))
    return descriptors


def _compute_max_parallel(run_config: run_config_pb2.RunConfig) -> int:
    """Computes the number of worker processes to run concurrently.

    Args:
        run_config: Run configuration.

    Returns:
        The maximum number of concurrent worker processes.
    """
    requested_parallelism = max(1, run_config.max_parallel)
    return min(requested_parallelism, run_config.num_runs)


def _build_worker_command(
    binary_path: Path,
    descriptor: RunDescriptor,
    unity_log_dir: Path,
) -> list[str]:
    """Builds the standalone Unity command for a single run.

    Args:
        binary_path: Path to the Unity executable.
        descriptor: Run descriptor for this worker.
        unity_log_dir: Directory in which Unity logs are stored.

    Returns:
        The command-line argument vector for subprocess.Popen().
    """
    unity_log_path = (unity_log_dir /
                      f"run_{descriptor.run_index}_seed_{descriptor.seed}.log")
    return [
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

                logging.info("Launching run %d/%d with seed %d.",
                             descriptor.run_index, len(descriptors),
                             descriptor.seed)

                running_workers[descriptor.run_index] = (
                    descriptor,
                    subprocess.Popen(command),
                )

            finished_run_index = None
            for run_index, (descriptor, process) in running_workers.items():
                exit_code = process.poll()
                if exit_code is None:
                    continue

                if exit_code != 0:
                    raise RuntimeError(
                        f"Run {run_index} exited with code {exit_code}.")
                if not descriptor.output_dir.is_dir():
                    raise RuntimeError(
                        f"Run {run_index} did not create an output directory.")
                logging.info(
                    "Completed run %d with seed %d.",
                    descriptor.run_index,
                    descriptor.seed,
                )

                finished_run_index = run_index
                break

            if finished_run_index is None:
                time.sleep(0.1)
                continue

            del running_workers[finished_run_index]
    except BaseException:
        _terminate_processes(
            [process for _, process in running_workers.values()])
        raise


def main(argv):
    assert len(argv) == 1, argv

    binary_path = _resolve_binary_path(FLAGS.binary_path)
    run_config_path = _resolve_run_config_path(FLAGS.run_config)
    run_config = _parse_run_config(run_config_path)

    # Initialize the log directories.
    timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
    log_root_dir = Path(FLAGS.log_root_dir).expanduser().resolve()
    batch_output_dir = log_root_dir / f"{run_config.name}_{timestamp}"
    unity_log_dir = (Path(FLAGS.unity_log_dir).expanduser().resolve()
                     if FLAGS.unity_log_dir is not None else
                     (log_root_dir / f"{batch_output_dir.name}_unity_logs"))
    batch_output_dir.mkdir(parents=True, exist_ok=False)
    unity_log_dir.mkdir(parents=True, exist_ok=False)

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
