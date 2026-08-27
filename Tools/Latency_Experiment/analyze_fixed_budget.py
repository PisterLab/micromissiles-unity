"""Compares tier allocations under a fixed end-to-end latency budget."""

import argparse
from pathlib import Path

import pandas as pd
from latency_dataset import (FIXED_BUDGET, REPO_ROOT, add_common_cli_arguments,
                             build_run_dataset, configure_logging)
from latency_metrics import (DEFAULT_ANALYSIS_METRICS, DEFAULT_PLOT_METRICS,
                             METRIC_LABELS)
from latency_plots import plot_latency_position_maps, plot_metric_curves
from latency_statistics import (available_metrics, paired_differences,
                                print_mean_std_summary, summarize_conditions)

ALLOCATION_LABELS = {
    "equal": "Equal (33.3% / 33.3% / 33.3%)",
    "top-heavy": "Top Heavy (80% / 10% / 10%)",
    "middle-heavy": "Middle Heavy (10% / 80% / 10%)",
    "bottom-heavy": "Bottom Heavy (10% / 10% / 80%)",
    "top-tier-only": "Top Tier Only (100% / 0% / 0%)",
    "middle-tier-only": "Middle Tier Only (0% / 100% / 0%)",
    "bottom-tier-only": "Bottom Tier Only (0% / 0% / 100%)",
}

DEFAULT_OUTPUT_DIR = REPO_ROOT / "Logs/Analysis/Fixed_Budget"


def analyze(
    data: pd.DataFrame,
    bootstrap_samples: int = 2000,
    output_dir: Path | None = None
) -> tuple[pd.DataFrame, pd.DataFrame, pd.DataFrame]:
    """Creates allocation plots and returns summaries and rankings."""
    family_data = data[data["family"] == FIXED_BUDGET].copy()
    if family_data.empty:
        raise ValueError("The dataset contains no fixed-budget runs.")

    family_data["allocation_label"] = family_data["allocation"].map(
        ALLOCATION_LABELS)
    if family_data["allocation_label"].isna().any():
        unknown = sorted(family_data.loc[family_data["allocation_label"].isna(),
                                         "allocation"].unique())
        raise ValueError(f"Unknown fixed-budget allocations: {unknown}")

    group_columns = ["allocation", "allocation_label", "latency_budget_s"]
    summary = summarize_conditions(
        family_data,
        group_columns,
        DEFAULT_ANALYSIS_METRICS,
        bootstrap_samples=bootstrap_samples,
    )
    plot_metrics = available_metrics(family_data, DEFAULT_PLOT_METRICS,
                                     "Fixed-budget latency")
    print_mean_std_summary(
        summary,
        group_columns,
        plot_metrics,
        "Fixed-budget mean and standard deviation",
    )
    rankings = summary.copy()
    rankings["penetration_rank"] = rankings.groupby(
        "latency_budget_s")["threat_penetration_rate_mean"].rank(method="dense",
                                                                 ascending=True)
    rankings["efficiency_rank"] = rankings.groupby(
        "latency_budget_s")["interceptor_efficiency_mean"].rank(method="dense",
                                                                ascending=False)
    for metric in plot_metrics:
        plot_metric_curves(
            summary,
            "latency_budget_s",
            metric,
            series_column="allocation_label",
            x_label="End-to-End Latency Budget [s]",
            title=f"Fixed Budget: {METRIC_LABELS[metric]}",
            legend_title="Allocation (Top / Middle / Bottom)",
            output_path=(output_dir /
                         f"{metric}.png" if output_dir is not None else None),
        )
    plot_latency_position_maps(
        family_data,
        "latency_budget_s",
        "End-to-End Latency Budget [s]",
        "Fixed Budget Latency",
        output_dir=output_dir,
        filename_prefix="fixed_budget",
    )
    baseline = family_data[family_data["latency_budget_s"] == 0]
    paired = paired_differences(
        family_data,
        baseline,
        on=["allocation", "seed"],
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
        rankings.to_csv(output_dir / "allocation_rankings.csv", index=False)
        delta_summary.to_csv(output_dir / "zero_budget_delta_summary.csv",
                             index=False)
    return summary, rankings, delta_summary


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    add_common_cli_arguments(parser)
    parser.add_argument("--output_dir", type=Path, default=DEFAULT_OUTPUT_DIR)
    args = parser.parse_args()
    configure_logging()
    data = build_run_dataset(
        args.log_root,
        args.config_root,
        families=[FIXED_BUDGET],
    )
    analyze(data, args.bootstrap_samples,
            args.output_dir.expanduser().resolve())


if __name__ == "__main__":
    main()
