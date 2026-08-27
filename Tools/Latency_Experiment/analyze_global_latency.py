"""Analyzes performance as a function of global communication latency."""

import argparse
from pathlib import Path

import pandas as pd
from latency_dataset import (GLOBAL_LATENCY, REPO_ROOT,
                             add_common_cli_arguments, build_run_dataset,
                             configure_logging)
from latency_metrics import (DEFAULT_ANALYSIS_METRICS, DEFAULT_PLOT_METRICS,
                             METRIC_LABELS)
from latency_plots import plot_latency_position_maps, plot_metric_curves
from latency_statistics import (available_metrics, paired_differences,
                                print_mean_std_summary, summarize_conditions)

DEFAULT_OUTPUT_DIR = REPO_ROOT / "Logs/Analysis/Global_Latency"


def analyze(
        data: pd.DataFrame,
        bootstrap_samples: int = 2000,
        output_dir: Path | None = None) -> tuple[pd.DataFrame, pd.DataFrame]:
    """Creates global-latency plots and returns their summaries."""
    family_data = data[data["family"] == GLOBAL_LATENCY].copy()
    if family_data.empty:
        raise ValueError("The dataset contains no global-latency runs.")

    summary = summarize_conditions(
        family_data,
        ["mean_latency_s"],
        DEFAULT_ANALYSIS_METRICS,
        bootstrap_samples=bootstrap_samples,
    )
    baseline = family_data[family_data["mean_latency_s"] == 0]
    paired = paired_differences(
        family_data,
        baseline,
        on=["seed"],
        metrics=DEFAULT_ANALYSIS_METRICS,
    )
    delta_metrics = [f"{metric}_delta" for metric in DEFAULT_ANALYSIS_METRICS]
    delta_summary = summarize_conditions(
        paired,
        ["mean_latency_s"],
        delta_metrics,
        bootstrap_samples=bootstrap_samples,
    )
    if output_dir is not None:
        output_dir.mkdir(parents=True, exist_ok=True)
        summary.to_csv(output_dir / "condition_summary.csv", index=False)
        delta_summary.to_csv(output_dir / "zero_latency_delta_summary.csv",
                             index=False)

    plot_metrics = available_metrics(family_data, DEFAULT_PLOT_METRICS,
                                     "Global latency")
    print_mean_std_summary(
        summary,
        ["mean_latency_s"],
        plot_metrics,
        "Global latency mean and standard deviation",
    )
    for metric in plot_metrics:
        plot_metric_curves(
            summary,
            "mean_latency_s",
            metric,
            x_label="Global Latency [s]",
            title=f"Global Latency: {METRIC_LABELS[metric]}",
            output_path=(output_dir /
                         f"{metric}.png" if output_dir is not None else None),
        )
    plot_latency_position_maps(
        family_data,
        "mean_latency_s",
        "Global Latency [s]",
        "Global Latency",
        output_dir=output_dir,
        filename_prefix="global_latency",
    )
    return summary, delta_summary


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    add_common_cli_arguments(parser)
    parser.add_argument("--output_dir", type=Path, default=DEFAULT_OUTPUT_DIR)
    args = parser.parse_args()
    configure_logging()
    data = build_run_dataset(
        args.log_root,
        args.config_root,
        families=[GLOBAL_LATENCY],
    )
    analyze(data, args.bootstrap_samples,
            args.output_dir.expanduser().resolve())


if __name__ == "__main__":
    main()
