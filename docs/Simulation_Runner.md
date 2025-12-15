# Simulation Runner

This guide describes how to build a standalone Unity player to kick off multiple simulation runs using a run configuration.

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

Run configurations are used to invoke simulation runs on the standalone player using the command line.
Run configurations can only be used on the standalone player and not in the interactive mode in the Unity Editor.

The run configuration defines the simulation configuration to use, the number of runs to perform, and the seed to use for the random number generator.
The [`run_config.proto`](https://github.com/PisterLab/micromissiles-unity/blob/master/Assets/Proto/Configs/run_config.proto) file defines the run config proto message that is used to parse the run configuration files in Protobuf text format.
The main benefit of a run configuration is that multiple simulation runs can be performed sequentially on a single simulation configuration to generate aggregated statistics.

The run configuration includes:
- **name**: Run configuration name used to name the log directory.
- **simulation_config_file**: Simulation configuration to use for this run.
- **num_runs**: Number of times to run the simulation configuration.
- **seed**: Random number generator seed.
- **seed_stride**: The seed increment for subsequent runs.

## Running from Command Line

#### Flags

- **`--config <config_file>`**: Specifies the path to the run configuration file relative to the `Assets/StreamingAssets/Configs/Runs` directory.
- **`-logFile <log_file>`** (optional): Specifies where Unity will place its [Editor log](https://docs.unity3d.com/540/Documentation/Manual/LogFiles.html).

#### Windows
```powershell
cd .\Build\<timestamp>
.\micromissiles.exe --config batch_7_quadcopters.txt `
    -batchmode `
    -nographics `
    -logFile micromissiles.log
```

#### Mac
```bash
cd Build/<timestamp>
./micromissiles.app/Contents/MacOS/micromissiles \
    --config batch_7_quadcopters.txt \
    -batchmode \
    -nographics \
    -logFile micromissiles.log
```

#### Linux
```bash
cd Build/<timestamp>
./micromissiles --config batch_7_quadcopters.txt \
    -batchmode \
    -nographics \
    -logFile micromissiles.log
```

Logs are exported to the `Logs` directory in your operating system's [persistent data path](https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html).
The run configuration creates a parent directory called `<run_config_name>_<timestamp>`, in which the per-run logs are stored in separate subdirectories called `run_<run_index>_seed_<seed>`.

For more details on logging and log processing, consult the [Simulation Logging](./Simulation_Logging.md) guide.
