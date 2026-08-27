"""Builds one run dataset and runs every latency experiment analyzer."""

import argparse
import os
import tempfile
from pathlib import Path

os.environ.setdefault(
    "MPLCONFIGDIR",
    str(Path(tempfile.gettempdir()) / "micromissiles-matplotlib"),
)

from analyze_directional_latencies import analyze as analyze_directional
from analyze_fixed_budget import analyze as analyze_budget
from analyze_global_jitter import analyze as analyze_jitter
from analyze_global_latency import analyze as analyze_global
from analyze_kill_probability_latency import \
    analyze as analyze_kill_probability
from analyze_kill_probability_latency import \
    load_or_build_run_data as load_or_build_kill_probability_data
from analyze_kill_probability_latency import \
    validate_design as validate_kill_probability_design
from analyze_single_pair_latencies import analyze as analyze_single_pair
from latency_dataset import (ANALYSIS_FAMILIES, NO_LATENCY, REPO_ROOT,
                             add_common_cli_arguments, build_run_dataset,
                             configure_logging)

DEFAULT_OUTPUT_ROOT = REPO_ROOT / "Logs/Analysis"


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    add_common_cli_arguments(parser)
    parser.add_argument(
        "--output_root",
        type=Path,
        default=DEFAULT_OUTPUT_ROOT,
        help="Root directory for each experiment family's CSVs and figures.",
    )
    parser.add_argument(
        "--kill_workers",
        type=int,
        default=4,
        help="Worker threads used to build the kill-probability metric cache.",
    )
    parser.add_argument(
        "--refresh_kill_cache",
        action="store_true",
        help="Rebuild the kill-probability run cache from event logs.",
    )
    args = parser.parse_args()
    configure_logging()
    output_root = args.output_root.expanduser().resolve()
    output_root.mkdir(parents=True, exist_ok=True)
    data = build_run_dataset(
        args.log_root,
        args.config_root,
        families=ANALYSIS_FAMILIES + [NO_LATENCY],
    )

    analyze_global(data, args.bootstrap_samples, output_root / "Global_Latency")
    analyze_single_pair(data, args.bootstrap_samples,
                        output_root / "Single_Tier_Latencies")
    analyze_directional(data, args.bootstrap_samples,
                        output_root / "Directional_Latencies")
    analyze_budget(data, args.bootstrap_samples, output_root / "Fixed_Budget")
    analyze_jitter(data, args.bootstrap_samples, output_root / "Global_Jitter")

    kill_output_dir = output_root / "Kill_Probability_Latency"
    kill_output_dir.mkdir(parents=True, exist_ok=True)
    kill_data = load_or_build_kill_probability_data(
        args.log_root,
        args.config_root,
        kill_output_dir / "run_metrics.csv",
        refresh_cache=args.refresh_kill_cache,
        strict=True,
        workers=args.kill_workers,
    )
    problems = validate_kill_probability_design(kill_data)
    if problems:
        message = "\n".join(f"  - {problem}" for problem in problems)
        raise ValueError("Kill-probability design validation failed:\n" +
                         message)
    analyze_kill_probability(
        kill_data,
        kill_output_dir,
        bootstrap_samples=args.bootstrap_samples,
    )


if __name__ == "__main__":
    main()
