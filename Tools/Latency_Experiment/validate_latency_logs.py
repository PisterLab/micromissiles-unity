"""Validates latency experiment placement, configs, runs, and log counts."""

import argparse
import logging
from pathlib import Path

import pandas as pd
from latency_dataset import (DEFAULT_CONFIG_ROOT, FAMILY_DIRECTORY_NAMES,
                             FAMILY_ID_RANGES, NO_LATENCY, configure_logging,
                             default_log_root, discover_batches)


def validate(
    log_root: Path,
    config_root: Path = DEFAULT_CONFIG_ROOT
) -> tuple[pd.DataFrame, list[str], list[str]]:
    """Returns per-batch validation rows, errors, and warnings."""
    descriptors = discover_batches(log_root, config_root)
    rows: list[dict[str, object]] = []
    errors: list[str] = []
    warnings: list[str] = []

    for descriptor in descriptors:
        run_paths = sorted(path for path in descriptor.path.iterdir()
                           if path.is_dir() and path.name.startswith("run_"))
        expected_runs = descriptor.expected_runs
        if expected_runs is None and descriptor.family == NO_LATENCY:
            expected_runs = 50
        event_log_count = sum(
            len(list(run_path.glob("sim_events_*.csv")))
            for run_path in run_paths)
        telemetry_log_count = sum(
            len(list(run_path.glob("sim_telemetry_*.csv")))
            for run_path in run_paths)
        expected_category = FAMILY_DIRECTORY_NAMES[descriptor.family]
        placement_ok = descriptor.stored_category == expected_category
        config_required = descriptor.experiment_id is not None
        config_ok = (descriptor.run_config_path is not None and
                     descriptor.simulation_config_path is not None)
        counts_ok = (expected_runs is not None and
                     len(run_paths) == expected_runs and
                     event_log_count == expected_runs and
                     telemetry_log_count == expected_runs)

        if not placement_ok:
            warnings.append(
                f"{descriptor.path.name} is stored in "
                f"{descriptor.stored_category}; expected {expected_category}.")
        if config_required and not config_ok:
            errors.append(f"Missing config for {descriptor.path.name}.")
        if not counts_ok:
            errors.append(
                f"Unexpected run/log count in {descriptor.path.name}: "
                f"runs={len(run_paths)}, events={event_log_count}, "
                f"telemetry={telemetry_log_count}, expected={expected_runs}.")

        rows.append({
            "family": descriptor.family,
            "experiment_id": descriptor.experiment_id,
            "batch_name": descriptor.path.name,
            "stored_category": descriptor.stored_category,
            "expected_category": expected_category,
            "placement_ok": placement_ok,
            "config_ok": config_ok or not config_required,
            "expected_runs": expected_runs,
            "run_directory_count": len(run_paths),
            "event_log_count": event_log_count,
            "telemetry_log_count": telemetry_log_count,
            "counts_ok": counts_ok,
        })

    for family, expected_range in FAMILY_ID_RANGES.items():
        actual_ids = [
            descriptor.experiment_id for descriptor in descriptors if
            descriptor.family == family and descriptor.experiment_id is not None
        ]
        missing_ids = sorted(set(expected_range).difference(actual_ids))
        duplicated_ids = sorted({
            experiment_id for experiment_id in actual_ids
            if actual_ids.count(experiment_id) > 1
        })
        if missing_ids:
            errors.append(f"{family} is missing experiment IDs: {missing_ids}")
        if duplicated_ids:
            errors.append(
                f"{family} has duplicated experiment IDs: {duplicated_ids}")

    return pd.DataFrame(rows), errors, warnings


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--log_root", type=Path, default=default_log_root())
    parser.add_argument("--config_root", type=Path, default=DEFAULT_CONFIG_ROOT)
    args = parser.parse_args()
    configure_logging()

    validation, errors, warnings = validate(args.log_root, args.config_root)
    valid_batches = int(validation["counts_ok"].sum())
    logging.info("Validated %d/%d batches successfully.", valid_batches,
                 len(validation))
    for warning in warnings:
        logging.warning(warning)
    for error in errors:
        logging.error(error)
    if errors:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
