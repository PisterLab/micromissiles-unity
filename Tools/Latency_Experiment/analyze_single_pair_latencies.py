"""Compares latency sensitivity across the three bidirectional tiers."""

import argparse
from pathlib import Path

import pandas as pd
from latency_dataset import (REPO_ROOT, SINGLE_PAIR_LATENCIES,
                             add_common_cli_arguments, build_run_dataset,
                             configure_logging)
from latency_metrics import (DEFAULT_ANALYSIS_METRICS, DEFAULT_PLOT_METRICS,
                             METRIC_LABELS)
from latency_plots import plot_category_position_maps, plot_metric_curves
from latency_statistics import (available_metrics, paired_differences,
                                print_mean_std_summary, summarize_conditions)

SINGLE_TIER_LABELS = {
    "Carrier <-> Missile": "Carrier Interceptor ↔ Missile Interceptor",
    "IADS <-> Vessel": "IADS ↔ Vessel",
    "Vessel <-> Carrier": "Vessel ↔ Carrier Interceptor",
}

SINGLE_TIER_COLORS = {
    "Carrier Interceptor ↔ Missile Interceptor": "tab:blue",
    "IADS ↔ Vessel": "tab:green",
    "Vessel ↔ Carrier Interceptor": "tab:orange",
}

DEFAULT_OUTPUT_DIR = REPO_ROOT / "Logs/Analysis/Single_Tier_Latencies"


def analyze(
        data: pd.DataFrame,
        bootstrap_samples: int = 2000,
        output_dir: Path | None = None) -> tuple[pd.DataFrame, pd.DataFrame]:
    """Creates single-tier response plots and returns their summaries."""
    family_data = data[data["family"] == SINGLE_PAIR_LATENCIES].copy()
    if family_data.empty:
        raise ValueError("The dataset contains no single-pair latency runs.")
    family_data["tier_label"] = family_data["tier"].map(
        SINGLE_TIER_LABELS).fillna(family_data["tier"])

    group_columns = ["tier", "tier_label", "mean_latency_s"]
    summary = summarize_conditions(
        family_data,
        group_columns,
        DEFAULT_ANALYSIS_METRICS,
        bootstrap_samples=bootstrap_samples,
    )
    baseline = family_data[family_data["mean_latency_s"] == 0]
    paired = paired_differences(
        family_data,
        baseline,
        on=["tier", "seed"],
        metrics=DEFAULT_ANALYSIS_METRICS,
    )
    delta_metrics = [f"{metric}_delta" for metric in DEFAULT_ANALYSIS_METRICS]
    delta_summary = summarize_conditions(
        paired,
        group_columns,
        delta_metrics,
        bootstrap_samples=bootstrap_samples,
    )
    if output_dir is not None:
        output_dir.mkdir(parents=True, exist_ok=True)
        summary.to_csv(output_dir / "condition_summary.csv", index=False)
        delta_summary.to_csv(output_dir / "zero_latency_delta_summary.csv",
                             index=False)

    plot_metrics = available_metrics(family_data, DEFAULT_PLOT_METRICS,
                                     "Single-pair latency")
    print_mean_std_summary(
        summary,
        group_columns,
        plot_metrics,
        "Single-pair latency mean and standard deviation",
    )
    for metric in plot_metrics:
        plot_metric_curves(
            summary,
            "mean_latency_s",
            metric,
            series_column="tier_label",
            series_colors=SINGLE_TIER_COLORS,
            x_label="Single Tier Latency [s]",
            title=f"Single Tier Latency: {METRIC_LABELS[metric]}",
            legend_title="Communication Tier",
            output_path=(output_dir /
                         f"{metric}.png" if output_dir is not None else None),
        )
    plot_category_position_maps(
        family_data,
        "tier_label",
        "Communication Tier",
        "Single Tier Latency",
        category_colors=SINGLE_TIER_COLORS,
        output_dir=output_dir,
        filename_prefix="single_tier_latency",
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
        families=[SINGLE_PAIR_LATENCIES],
    )
    analyze(data, args.bootstrap_samples,
            args.output_dir.expanduser().resolve())


if __name__ == "__main__":
    main()
