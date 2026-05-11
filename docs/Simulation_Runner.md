# Simulation Runner

This guide describes how to build a standalone Unity player and launch deterministic batches of simulation runs from Python.

## Standalone Build

While the simulation can be executed interactively in the Unity Editor, a standalone player allows the simulation to be invoked from the command line.
`build_standalone.py` is a helper script included in the `Tools` directory of the release to generate a standalone build of the simulator for the current operating system.

### Requirements

To run `build_standalone.py`, ensure that you have Python 3 installed on your system and that you have the `absl-py` library.

### Building

Run the script as follows:
```bash
python3 Tools/build_standalone.py
```

You can specify three optional flags:
- **--unity_path**: Path to the Unity installation. If not provided, defaults to `$UNITY_PATH`. If `$UNITY_PATH` is not provided, the script will automatically search through a list of common directories for a Unity installation.
- **--build_name**: Build name. If not provided, defaults to `micromissiles`.
- **--build_root**: Build root directory. If not provided, defaults to `Build/`.

The standalone build is stored in a timestamped subdirectory of the build root directory.
```
# Example build directory on a Mac.
Build/
  ├── 20251117_104914/
  │   ├── micromissiles.app
  │   ├── micromissiles_BurstDebugInformation_DoNotShip/
  │   └── unity.log
  │
  └── ...
```

## Run Configuration

Run configurations are consumed by `Tools/run_batch.py`, which launches one standalone Unity process per simulation run.
Batch runs are only supported on the standalone player and not in the interactive mode in the Unity Editor.

### Requirements

To run `Tools/run_batch.py`, ensure that you have Python 3 installed and that the `protobuf` Python package is available in that environment.

The run configuration defines the simulation configuration to use, the number of runs to perform, the seed to use for the random number generator, and the maximum number of concurrent worker processes.
The [`run_config.proto`](https://github.com/PisterLab/micromissiles-unity/blob/master/Assets/Proto/Configs/run_config.proto) file defines the run config proto message used by the batch run launcher.
The main benefit of a run configuration is that repeated runs of a single simulation configuration can be scheduled deterministically and aggregated afterwards.

The run configuration includes:
- **name**: Run configuration name used to name the log directory.
- **simulation_config_file**: Simulation configuration to use for this run.
- **num_runs**: Number of times to run the simulation configuration.
- **seed**: Random number generator seed.
- **seed_stride**: The seed increment for subsequent runs.
- **max_parallel**: Maximum number of Unity worker processes to run concurrently. If omitted or set to `0`, the launcher runs one worker at a time.

## Running from Command Line

#### Flags

- **`--binary_path <path>`**: Path to the standalone Unity executable.
- **`--run-config <config_file>`**: Path to the run configuration file, or its filename relative to `Assets/StreamingAssets/Configs/Runs`.
- **`--log_root_dir <directory>`** (optional): Root directory in which to create the batch output directory. Defaults to the Unity persistent data path.
- **`--unity-log-dir <directory>`** (optional): Directory in which to store the per-run Unity logs.

#### Windows

```powershell
cd .\Build\<timestamp>
python3 Tools/run_batch.py `
    --unity_path .\micromissiles.exe `
    --run_config batch_7_quadcopters.pbtxt
```

#### Mac

```bash
cd Build/<timestamp>
python3 ../../Tools/run_batch.py \
    --unity_path ./micromissiles.app \
    --run_config batch_7_quadcopters.pbtxt
```

#### Linux

```bash
cd Build/<timestamp>
python3 ../../Tools/run_batch.py \
    --unity_path ./micromissiles \
    --run_config batch_7_quadcopters.pbtxt
```

Logs are exported to the `Logs` directory in your operating system's [persistent data path](https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html).
The batch run launcher creates a parent directory called `<run_config_name>_<timestamp>`, in which the per-run logs are stored in separate subdirectories called `run_<run_index>_seed_<seed>`.

## Single-Run CLI

The standalone Unity player now supports a single-run mode instead of Unity-side batch orchestration.
This is the interface used by `Tools/run_batch.py` for each worker process.

- `--simulation_config <file>`: Simulation configuration filename from `Assets/StreamingAssets/Configs/Simulations`.
- `--seed <int>`: Explicit deterministic seed for the run.
- `--output_dir <directory>`: Absolute directory in which to store the run's telemetry and event logs.

For more details on logging and log processing, consult the [Simulation Logging](./Simulation_Logging.md) guide.
