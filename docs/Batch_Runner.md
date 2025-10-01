---
title: Batch Simulation Runner
outline: deep
---

# Batch Simulation Runner

This guide walks through the batch execution workflow for micromissiles-unity and complements the high-level [Simulator Overview](./Simulator_Overview.md). Pair it with the [Simulation Configuration Guide](./Simulation_Configuration_Guide.md) for authoring configs and [Simulation Logging](./Simulation_Logging.md) for understanding the emitted data.

## Overview
- Run multiple simulations sequentially from the command line or the Unity Editor.
- Deterministic per-run seeding, optional JSON overrides, and per-run logs with metadata.
- Integrates with downstream analysis such as `Tools/aggregate_runs.py`.

### Build Prerequisite
Batch execution operates on a standalone player, so compile the project before invoking batch mode. Use the helper script `Tools/build_standalone.py` to mirror the CI build: it detects installed Unity editors, optionally compiles native plugins, and produces packaged binaries under `build/`. Once the target player is generated, launch it with the CLI flags described below (for example, `./build/StandaloneLinux64/micromissiles.x86_64 --batchConfig ...`).

## CLI Flags (Player or Editor `-executeMethod`)
- `--batchConfig <path>`: JSON file describing the batch (see schema below).
- `--config <nameOrPath>`: Single config file (StreamingAssets/Configs relative name or absolute path).
- `--runs <int>`: Number of replicated runs (with `--config`).
- `--seed <int>`: Base seed (default 1).
- `--seedStride <int>`: Seed increment between runs (default 1).
- `--labels <json>`: Flat JSON object of string labels to tag all runs.
- `--overrides <json>`: Flat JSON of SelectToken paths to values (e.g., `{ "timeScale": 0.1 }`).
- `--batchId <string>`: Optional batch identifier. If omitted, one is generated.
- `--labels <json>` and per-run labels surface inside telemetry metadata. See [Simulation Logging](./Simulation_Logging.md#batch-run-metadata) for field details.

## Notes on Paths
- `--batchConfig` accepts absolute paths or paths relative to the working directory used to launch the player/editor.
- Paths declared inside a batch JSON (such as `runs[].config`) are resolved relative to the location of that batch JSON. This keeps runs portable when the whole `StreamingAssets/Configs` tree ships with the build.
- For the single-config mode (`--config`), you can pass either an absolute path, a path relative to the working directory (e.g., `./micromissiles_Data/StreamingAssets/Configs/7_quadcopters.json`), or just the filename if it lives under `StreamingAssets/Configs/` (e.g., `--config 7_quadcopters.json`).
- `-logFile` is handled by Unity. Relative paths are resolved from the current working directory.

## Examples

::: tip Run from the `Build` directory so relative paths resolve cleanly.
:::

#### Windows (PowerShell)

```powershell
cd .\Build
.\micromissiles.exe --batchConfig ".\micromissiles_Data\StreamingAssets\Configs\Batches\sample_batch.json" -batchmode -nographics -logFile micromissiles.log
```

#### macOS / Linux (Bash)

```bash
cd Build
./micromissiles --batchConfig "./micromissiles_Data/StreamingAssets/Configs/Batches/sample_batch.json" -batchmode -nographics -logFile micromissiles.log
```

Both commands stream Unity’s log to stdout (`-logFile -`) and write the output to `micromissiles.log` for later inspection.

## Batch File Schema (flexible)
- Replicated form:
```
{
  "batchId": "exp_quad_v1",
  "config": "../7_quadcopters.json",
  "runs": 3,
  "seed": 10,
  "seedStride": 5,
  "labels": { "exp": "A" },
  "overrides": { "timeScale": 0.05 }
}
```
- Explicit runs form:
```
{
  "batchId": "exp_sweep_v2",
  "runs": [
    { "config": "../7_ucav.json", "seed": 1, "overrides": { "timeScale": 0.1 } },
    { "config": "../7_ucav.json", "seed": 2, "overrides": { "timeScale": 0.2 } }
  ]
}
```

See the checked-in sample at `Assets/StreamingAssets/Configs/Batches/sample_batch.json` for a working template.

## Logs and Metadata
- Per-run logs are written to: `<persistentDataPath>/Telemetry/Logs/<BatchID>/<RunID>/`.
- Files include:
  - `sim_telemetry_<timestamp>.bin` + converted CSV
  - `sim_events_<timestamp>.csv`
  - `run_meta.json` containing batch/run identifiers, labels, seed, config name, and timestamps.

Consult [Simulation Logging](./Simulation_Logging.md#persistent-storage-layout) for a deep dive into the telemetry folder structure.

## Post-Run Tools
- `Tools/aggregate_runs.py` auto-discovers the persistent Telemetry/Logs directory (cross-platform) and emits hit/miss aggregates per batch, plus overall hit rate. Combine its output with the metadata columns described above to automate experiment summaries.

## Overrides
- Supply a flat JSON object where keys are JSONPath-style `SelectToken` paths into the base config and values are the replacements. Example:
  `{ "threat_swarm_configs[0].num_agents": 12, "timeScale": 0.02 }`

## Determinism
- Unity RNG is seeded via `Random.InitState(seed)` and a shared `System.Random` is available as `RunContext.SystemRandom`.
- `KMeansClusterer` now accepts the seeded RNG implicitly via `RunContext` for stable clustering.