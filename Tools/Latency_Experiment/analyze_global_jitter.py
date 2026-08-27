"""Analyzes global latency jitter independently of mean latency."""

import argparse
from pathlib import Path

import pandas as pd
from latency_dataset import (GLOBAL_JITTER, GLOBAL_LATENCY, REPO_ROOT,
                             add_common_cli_arguments, build_run_dataset,
                             configure_logging)
from latency_metrics import (DEFAULT_ANALYSIS_METRICS, DEFAULT_PLOT_METRICS,
                             METRIC_LABELS)
from latency_plots import plot_latency_position_maps, plot_metric_curves
from latency_statistics import (available_metrics, paired_differences,
                                print_mean_std_summary, summarize_conditions)

DEFAULT_OUTPUT_DIR = REPO_ROOT / "Logs/Analysis/Global_Jitter"


def analyze(
        data: pd.DataFrame,
        bootstrap_samples: int = 2000,
        output_dir: Path | None = None) -> tuple[pd.DataFrame, pd.DataFrame]:
    """Creates jitter plots and returns summaries and paired comparisons."""
    jitter_data = data[data["family"] == GLOBAL_JITTER].copy()
    global_data = data[data["family"] == GLOBAL_LATENCY].copy()
    if jitter_data.empty:
        raise ValueError("The dataset contains no global-jitter runs.")
    if global_data.empty:
        raise ValueError(
            "Global-latency runs are required as zero-jitter baselines.")

    jitter_data["jitter_ratio_label"] = jitter_data["jitter_ratio"].map(
        lambda value: f"{value:g}x Mean")
    group_columns = [
        "jitter_ratio",
        "jitter_ratio_label",
        "mean_latency_s",
        "jitter_std_s",
    ]
    summary = summarize_conditions(
        jitter_data,
        group_columns,
        DEFAULT_ANALYSIS_METRICS,
        bootstrap_samples=bootstrap_samples,
    )
    # Decimal textproto values are rounded to protect the pairing key from
    # floating-point representation differences between the two datasets.
    jitter_data["latency_pair_key"] = jitter_data["mean_latency_s"].round(9)
    global_data["latency_pair_key"] = global_data["mean_latency_s"].round(9)
    paired = paired_differences(
        jitter_data,
        global_data,
        on=["latency_pair_key", "seed"],
        metrics=DEFAULT_ANALYSIS_METRICS,
    )
    delta_metrics = [f"{metric}_delta" for metric in DEFAULT_ANALYSIS_METRICS]
    delta_summary = summarize_conditions(
        paired,
        ["jitter_ratio", "mean_latency_s", "jitter_std_s"],
        delta_metrics,
        bootstrap_samples=bootstrap_samples,
    )
    if output_dir is not None:
        output_dir.mkdir(parents=True, exist_ok=True)
        summary.to_csv(output_dir / "condition_summary.csv", index=False)
        delta_summary.to_csv(output_dir / "zero_jitter_delta_summary.csv",
                             index=False)

    plot_metrics = available_metrics(jitter_data, DEFAULT_PLOT_METRICS,
                                     "Global jitter")
    print_mean_std_summary(
        summary,
        ["jitter_ratio", "mean_latency_s", "jitter_std_s"],
        plot_metrics,
        "Global jitter mean and standard deviation",
    )
    for metric in plot_metrics:
        plot_metric_curves(
            summary,
            "mean_latency_s",
            metric,
            series_column="jitter_ratio_label",
            x_label="Mean Global Latency [s]",
            title=f"Global Jitter: {METRIC_LABELS[metric]}",
            legend_title="Jitter Standard Deviation",
            output_path=(output_dir /
                         f"{metric}.png" if output_dir is not None else None),
        )
    plot_latency_position_maps(
        jitter_data,
        "mean_latency_s",
        "Mean Global Latency [s]",
        "Global Jitter (All Jitter Ratios)",
        output_dir=output_dir,
        filename_prefix="global_jitter",
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
        families=[GLOBAL_JITTER, GLOBAL_LATENCY],
    )
    analyze(data, args.bootstrap_samples,
            args.output_dir.expanduser().resolve())


if __name__ == "__main__":
    main()
