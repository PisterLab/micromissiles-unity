"""Discovery and loading for the communication-latency experiments."""

import argparse
import logging
import platform
import re
from concurrent.futures import ThreadPoolExecutor, as_completed
from dataclasses import dataclass
from pathlib import Path

import numpy as np
import pandas as pd
from latency_metrics import summarize_event_log

REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_CONFIG_ROOT = REPO_ROOT / "Assets/StreamingAssets/Configs"

GLOBAL_LATENCY = "global_latency"
SINGLE_PAIR_LATENCIES = "single_pair_latencies"
DIRECTIONAL_LATENCIES = "directional_latencies"
FIXED_BUDGET = "fixed_budget"
GLOBAL_JITTER = "global_jitter"
KILL_PROBABILITY = "kill_probability"
NO_LATENCY = "no_latency"

ANALYSIS_FAMILIES = [
    GLOBAL_LATENCY,
    SINGLE_PAIR_LATENCIES,
    DIRECTIONAL_LATENCIES,
    FIXED_BUDGET,
    GLOBAL_JITTER,
]

FAMILY_ID_RANGES = {
    GLOBAL_LATENCY: range(0, 47),
    SINGLE_PAIR_LATENCIES: range(100, 163),
    DIRECTIONAL_LATENCIES: range(1000, 1168),
    FIXED_BUDGET: range(10000, 10147),
    KILL_PROBABILITY: range(50000, 50420),
    GLOBAL_JITTER: range(100000, 100126),
}

FAMILY_DIRECTORY_NAMES = {
    GLOBAL_LATENCY: "Global_Latency",
    SINGLE_PAIR_LATENCIES: "Single_Pair_Latencies",
    DIRECTIONAL_LATENCIES: "Directional_Latencies",
    FIXED_BUDGET: "Fixed_Budget",
    KILL_PROBABILITY: "Kill Probability",
    GLOBAL_JITTER: "Global_jitter",
    NO_LATENCY: "No_Latency",
}

_BATCH_TIMESTAMP_PATTERN = re.compile(
    r"^(?P<run_config_name>.+)_(?P<date>\d{8})_(?P<time>\d{6})$")
_EXPERIMENT_ID_PATTERN = re.compile(r"_(?P<experiment_id>\d+)$")
_RUN_DIRECTORY_PATTERN = re.compile(
    r"run_(?P<run_index>\d+)_seed_(?P<seed>\d+)")
_SIMULATION_CONFIG_PATTERN = re.compile(
    r'^\s*simulation_config_file:\s*"(?P<filename>[^"]+)"', re.MULTILINE)
_NUM_RUNS_PATTERN = re.compile(r"^\s*num_runs:\s*(?P<num_runs>\d+)",
                               re.MULTILINE)
_FLOAT_PATTERN = r"[+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?"


@dataclass(frozen=True)
class BatchDescriptor:
    """Metadata needed to load one batch of seeded simulation runs."""

    path: Path
    stored_category: str
    run_config_name: str
    run_config_path: Path | None
    simulation_config_path: Path | None
    experiment_id: int | None
    family: str
    expected_runs: int | None


def default_log_root() -> Path:
    """Returns Unity's persistent log directory for this platform."""
    system = platform.system()
    if system == "Windows":
        return (Path.home() / "AppData/LocalLow/BAMLAB/micromissiles/Logs")
    if system == "Darwin":
        return (Path.home() /
                "Library/Application Support/BAMLAB/micromissiles/Logs")
    if system == "Linux":
        return Path.home() / ".config/unity3d/BAMLAB/micromissiles/Logs"
    raise NotImplementedError(f"Unsupported platform: {system}.")


def family_for_experiment_id(experiment_id: int | None) -> str | None:
    """Maps an experiment ID to its experiment family."""
    if experiment_id is None:
        return None
    for family, experiment_ids in FAMILY_ID_RANGES.items():
        if experiment_id in experiment_ids:
            return family
    return None


def _normalise_category(category: str) -> str:
    return re.sub(r"[^a-z0-9]+", "_", category.lower()).strip("_")


def _is_batch_directory(path: Path) -> bool:
    if not path.is_dir() or path.name.endswith("_unity_logs"):
        return False
    return any(child.is_dir() and _RUN_DIRECTORY_PATTERN.fullmatch(child.name)
               for child in path.iterdir())


def _read_run_config(
        run_config_path: Path | None) -> tuple[str | None, int | None]:
    if run_config_path is None or not run_config_path.is_file():
        return None, None
    text = run_config_path.read_text()
    simulation_match = _SIMULATION_CONFIG_PATTERN.search(text)
    num_runs_match = _NUM_RUNS_PATTERN.search(text)
    simulation_filename = (simulation_match.group("filename")
                           if simulation_match else None)
    num_runs = (int(num_runs_match.group("num_runs"))
                if num_runs_match else None)
    return simulation_filename, num_runs


def describe_batch(batch_path: Path, stored_category: str,
                   config_root: Path) -> BatchDescriptor:
    """Builds a descriptor for one timestamped batch directory."""
    timestamp_match = _BATCH_TIMESTAMP_PATTERN.fullmatch(batch_path.name)
    if timestamp_match is None:
        raise ValueError(
            f"Unrecognized batch directory name: {batch_path.name}")
    run_config_name = timestamp_match.group("run_config_name")
    experiment_match = _EXPERIMENT_ID_PATTERN.search(run_config_name)
    experiment_id = (int(experiment_match.group("experiment_id"))
                     if experiment_match else None)

    family = family_for_experiment_id(experiment_id)
    if family is None:
        normalised_category = _normalise_category(stored_category)
        if normalised_category == NO_LATENCY:
            family = NO_LATENCY
        else:
            raise ValueError(
                f"Cannot classify experiment batch: {batch_path.name}")

    run_config_path = config_root / "Runs" / f"{run_config_name}.pbtxt"
    if not run_config_path.is_file():
        run_config_path = None
    simulation_filename, expected_runs = _read_run_config(run_config_path)
    simulation_config_path = (config_root / "Simulations" / simulation_filename
                              if simulation_filename else None)
    if simulation_config_path is not None and not simulation_config_path.is_file(
    ):
        simulation_config_path = None

    return BatchDescriptor(
        path=batch_path,
        stored_category=stored_category,
        run_config_name=run_config_name,
        run_config_path=run_config_path,
        simulation_config_path=simulation_config_path,
        experiment_id=experiment_id,
        family=family,
        expected_runs=expected_runs,
    )


def discover_batches(
        log_root: Path,
        config_root: Path = DEFAULT_CONFIG_ROOT) -> list[BatchDescriptor]:
    """Discovers timestamped experiment batches below a log root."""
    log_root = Path(log_root).expanduser().resolve()
    config_root = Path(config_root).expanduser().resolve()
    if not log_root.is_dir():
        raise FileNotFoundError(f"Log root does not exist: {log_root}")

    candidates: list[tuple[Path, str]] = []
    for child in sorted(log_root.iterdir()):
        if _is_batch_directory(child):
            candidates.append((child, log_root.name))
            continue
        if not child.is_dir():
            continue
        for grandchild in sorted(child.iterdir()):
            if _is_batch_directory(grandchild):
                candidates.append((grandchild, child.name))

    descriptors: list[BatchDescriptor] = []
    for batch_path, stored_category in candidates:
        try:
            descriptors.append(
                describe_batch(batch_path, stored_category, config_root))
        except ValueError as error:
            # The Unity log root may also contain unrelated experiment
            # families. They should not prevent the latency analyzers from
            # discovering their own recognized experiment ID ranges.
            if str(error).startswith("Cannot classify experiment batch:"):
                logging.debug("Skipping unrelated batch: %s", batch_path)
                continue
            raise
    return sorted(
        descriptors,
        key=lambda descriptor: (
            descriptor.experiment_id is None,
            descriptor.experiment_id
            if descriptor.experiment_id is not None else -1,
            str(descriptor.path),
        ),
    )


def _communication_config_text(config_text: str) -> str:
    marker = "communication_config {"
    marker_index = config_text.rfind(marker)
    return config_text[marker_index:] if marker_index >= 0 else ""


def _extract_float_values(text: str, field_name: str) -> list[float]:
    pattern = re.compile(rf"\b{re.escape(field_name)}:\s*({_FLOAT_PATTERN})")
    return [float(match.group(1)) for match in pattern.finditer(text)]


def _first_comment(config_text: str) -> str:
    first_line = config_text.splitlines()[0].strip() if config_text else ""
    return first_line.removeprefix("#").strip() if first_line.startswith(
        "#") else ""


def _fixed_budget_weights(allocation: str) -> tuple[float, float, float]:
    weights = {
        "equal": (1 / 3, 1 / 3, 1 / 3),
        "top-heavy": (0.8, 0.1, 0.1),
        "middle-heavy": (0.1, 0.8, 0.1),
        "bottom-heavy": (0.1, 0.1, 0.8),
        "top-tier-only": (1.0, 0.0, 0.0),
        "middle-tier-only": (0.0, 1.0, 0.0),
        "bottom-tier-only": (0.0, 0.0, 1.0),
    }
    if allocation not in weights:
        raise ValueError(f"Unknown fixed-budget allocation: {allocation}")
    return weights[allocation]


def parse_experiment_metadata(descriptor: BatchDescriptor) -> dict[str, object]:
    """Extracts condition metadata from a batch's simulation configuration."""
    metadata: dict[str, object] = {
        "description": "",
        "condition_label": descriptor.family,
        "mean_latency_s": np.nan,
        "jitter_std_s": np.nan,
        "jitter_ratio": np.nan,
        "tier": "",
        "direction": "",
        "latency_budget_s": np.nan,
        "allocation": "",
        "top_tier_latency_s": np.nan,
        "middle_tier_latency_s": np.nan,
        "bottom_tier_latency_s": np.nan,
        "kill_probability": np.nan,
        "nominal_miss_probability": np.nan,
    }
    if descriptor.simulation_config_path is None:
        if descriptor.family == NO_LATENCY:
            metadata.update({
                "description": "No communication latency baseline",
                "condition_label": "No latency",
                "mean_latency_s": 0.0,
                "jitter_std_s": 0.0,
            })
            return metadata
        raise FileNotFoundError(
            f"Missing simulation config for {descriptor.path.name}")

    config_text = descriptor.simulation_config_path.read_text()
    description = _first_comment(config_text)
    communication_text = _communication_config_text(config_text)
    latency_values = _extract_float_values(communication_text,
                                           "latency_seconds")
    jitter_values = _extract_float_values(communication_text,
                                          "latency_std_seconds")
    default_latency = latency_values[0] if latency_values else np.nan
    default_jitter = jitter_values[0] if jitter_values else np.nan
    metadata.update({
        "description": description,
        "mean_latency_s": default_latency,
        "jitter_std_s": default_jitter,
    })

    if descriptor.family in (GLOBAL_LATENCY, GLOBAL_JITTER):
        ratio = (_safe_ratio(default_jitter, default_latency)
                 if descriptor.family == GLOBAL_JITTER else 0.0)
        metadata.update({
            "jitter_ratio":
                ratio,
            "condition_label": (f"mean={default_latency:g}s, "
                                f"jitter={default_jitter:g}s"),
        })
        return metadata

    if descriptor.family == KILL_PROBABILITY:
        match = re.search(
            rf"UCAV kill probability\s+({_FLOAT_PATTERN})\s+"
            rf"\(nominal per-collision miss probability\s+"
            rf"({_FLOAT_PATTERN})\)",
            description,
        )
        if match is None:
            raise ValueError(
                f"Cannot parse kill-probability config: {description}")
        kill_probability = float(match.group(1))
        nominal_miss_probability = float(match.group(2))
        metadata.update({
            "jitter_ratio":
                0.0,
            "kill_probability":
                kill_probability,
            "nominal_miss_probability":
                nominal_miss_probability,
            "condition_label":
                (f"kill={kill_probability:g}, latency={default_latency:g}s"),
        })
        return metadata

    if descriptor.family == SINGLE_PAIR_LATENCIES:
        match = re.search(
            rf"isolates\s+({_FLOAT_PATTERN})-second.*?on the (.+?) tier\.",
            description,
        )
        if match is None:
            raise ValueError(f"Cannot parse single-pair config: {description}")
        latency = float(match.group(1))
        tier = match.group(2)
        metadata.update({
            "mean_latency_s": latency,
            "jitter_std_s": 0.0,
            "jitter_ratio": 0.0,
            "tier": tier,
            "condition_label": f"{tier}: {latency:g}s",
        })
        return metadata

    if descriptor.family == DIRECTIONAL_LATENCIES:
        match = re.search(
            rf"applies\s+({_FLOAT_PATTERN})-second latency to "
            r"(all|top-tier|middle-tier|bottom-tier) "
            r"(upward|downward) communication links?\.",
            description,
        )
        if match is None:
            # The tier-specific descriptions omit the word "communication".
            match = re.search(
                rf"applies\s+({_FLOAT_PATTERN})-second latency to "
                r"(all|top-tier|middle-tier|bottom-tier) "
                r"(upward|downward) links?\.",
                description,
            )
        if match is None:
            raise ValueError(f"Cannot parse directional config: {description}")
        latency = float(match.group(1))
        tier = match.group(2).removesuffix("-tier")
        direction = match.group(3)
        metadata.update({
            "mean_latency_s": latency,
            "jitter_std_s": 0.0,
            "jitter_ratio": 0.0,
            "tier": tier,
            "direction": direction,
            "condition_label": f"{tier} {direction}: {latency:g}s",
        })
        return metadata

    if descriptor.family == FIXED_BUDGET:
        match = re.search(
            rf"fixed\s+({_FLOAT_PATTERN})-second latency budget with "
            r"(.+?) distribution(?: across all three tiers)?\.",
            description,
        )
        if match is None:
            raise ValueError(f"Cannot parse fixed-budget config: {description}")
        budget = float(match.group(1))
        allocation_description = match.group(2)
        allocation = ("equal" if allocation_description == "equal" else
                      allocation_description.split()[0])
        top_weight, middle_weight, bottom_weight = _fixed_budget_weights(
            allocation)
        metadata.update({
            "mean_latency_s": np.nan,
            "jitter_std_s": 0.0,
            "latency_budget_s": budget,
            "allocation": allocation,
            "top_tier_latency_s": budget * top_weight,
            "middle_tier_latency_s": budget * middle_weight,
            "bottom_tier_latency_s": budget * bottom_weight,
            "condition_label": f"{allocation}: {budget:g}s",
        })
        return metadata

    return metadata


def _safe_ratio(numerator: float, denominator: float) -> float:
    if not np.isfinite(denominator) or denominator == 0:
        return np.nan
    return numerator / denominator


def _run_sort_key(run_path: Path) -> tuple[int, int]:
    match = _RUN_DIRECTORY_PATTERN.fullmatch(run_path.name)
    if match is None:
        return (1, 0)
    return (0, int(match.group("run_index")))


def _load_batch_rows(
    descriptor: BatchDescriptor,
    strict: bool,
) -> list[dict[str, object]]:
    """Loads run-level metadata and metrics for one experiment batch."""
    metadata = parse_experiment_metadata(descriptor)
    run_paths = sorted(
        (path for path in descriptor.path.iterdir()
         if path.is_dir() and _RUN_DIRECTORY_PATTERN.fullmatch(path.name)),
        key=_run_sort_key,
    )
    rows: list[dict[str, object]] = []
    for run_path in run_paths:
        run_match = _RUN_DIRECTORY_PATTERN.fullmatch(run_path.name)
        assert run_match is not None
        event_paths = sorted(run_path.glob("sim_events_*.csv"))
        if len(event_paths) != 1:
            message = (f"Expected one event log in {run_path}; found "
                       f"{len(event_paths)}.")
            if strict:
                raise ValueError(message)
            logging.warning(message)
            if not event_paths:
                continue
        event_path = event_paths[-1]
        event_df = pd.read_csv(event_path)
        metrics = summarize_event_log(event_df)
        rows.append({
            "family":
                descriptor.family,
            "stored_category":
                descriptor.stored_category,
            "experiment_id":
                descriptor.experiment_id,
            "batch_name":
                descriptor.path.name,
            "batch_path":
                str(descriptor.path),
            "simulation_config_path":
                str(descriptor.simulation_config_path)
                if descriptor.simulation_config_path else "",
            "run_index":
                int(run_match.group("run_index")),
            "seed":
                int(run_match.group("seed")),
            "event_log_path":
                str(event_path),
            **metadata,
            **metrics,
        })
    return rows


def build_run_dataset(
    log_root: Path,
    config_root: Path = DEFAULT_CONFIG_ROOT,
    families: list[str] | None = None,
    strict: bool = True,
    workers: int = 1,
) -> pd.DataFrame:
    """Builds a tidy dataframe with one row per simulation run.

    Args:
        log_root: Root containing timestamped batch directories.
        config_root: Root containing the run and simulation configurations.
        families: Optional experiment-family filter.
        strict: Whether malformed or missing event logs should stop loading.
        workers: Batch-level worker threads. Use one for serial loading.
    """
    descriptors = discover_batches(log_root, config_root)
    if families is not None:
        descriptors = [
            descriptor for descriptor in descriptors
            if descriptor.family in families
        ]
    if not descriptors:
        raise ValueError("No matching latency experiment batches were found.")

    total_batches = len(descriptors)
    if workers < 1:
        raise ValueError("workers must be at least 1")

    rows_by_batch: dict[int, list[dict[str, object]]] = {}
    if workers == 1:
        for batch_number, descriptor in enumerate(descriptors, start=1):
            logging.info("Loading batch %d/%d: %s", batch_number, total_batches,
                         descriptor.path.name)
            rows_by_batch[batch_number - 1] = _load_batch_rows(
                descriptor, strict)
    else:
        logging.info("Loading %d batches with %d worker threads.",
                     total_batches, workers)
        with ThreadPoolExecutor(max_workers=workers) as executor:
            futures = {
                executor.submit(_load_batch_rows, descriptor, strict): index
                for index, descriptor in enumerate(descriptors)
            }
            for completed, future in enumerate(as_completed(futures), start=1):
                index = futures[future]
                rows_by_batch[index] = future.result()
                if completed == total_batches or completed % 10 == 0:
                    logging.info("Loaded %d/%d batches.", completed,
                                 total_batches)

    rows = [
        row for batch_index in range(total_batches)
        for row in rows_by_batch[batch_index]
    ]
    return pd.DataFrame(rows)


def add_common_cli_arguments(parser: argparse.ArgumentParser) -> None:
    """Adds common dataset arguments to an analyzer CLI."""
    parser.add_argument(
        "--log_root",
        type=Path,
        default=default_log_root(),
        help="Root containing the latency experiment log directories.",
    )
    parser.add_argument(
        "--config_root",
        type=Path,
        default=DEFAULT_CONFIG_ROOT,
        help="Directory containing Runs/ and Simulations/ configs.",
    )
    parser.add_argument(
        "--bootstrap_samples",
        type=int,
        default=2000,
        help="Number of bootstrap resamples used for confidence intervals.",
    )


def configure_logging() -> None:
    """Configures concise command-line logging for analysis scripts."""
    logging.basicConfig(level=logging.INFO, format="%(levelname)s: %(message)s")
