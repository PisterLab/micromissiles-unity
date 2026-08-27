"""Compares upward and downward communication-latency sensitivity."""

import argparse
from pathlib import Path

import pandas as pd
from latency_dataset import (DIRECTIONAL_LATENCIES, REPO_ROOT,
                             add_common_cli_arguments, build_run_dataset,
                             configure_logging)
from latency_metrics import (DEFAULT_ANALYSIS_METRICS, DEFAULT_PLOT_METRICS,
                             METRIC_LABELS)
from latency_plots import plot_latency_position_maps, plot_metric_curves
from latency_statistics import (available_metrics, paired_differences,
                                print_mean_std_summary, summarize_conditions)

DIRECTIONAL_LINKS = {
    "All Tiers Upward": [
        "Vessel -> IADS",
        "Carrier Interceptor -> Vessel",
        "Missile Interceptor -> Carrier Interceptor",
    ],
    "All Tiers Downward": [
        "IADS -> Vessel",
        "IADS -> Carrier Interceptor",
        "IADS -> Missile Interceptor",
        "Vessel -> Carrier Interceptor",
        "Carrier Interceptor -> Missile Interceptor",
    ],
    "Vessel Upward": ["Vessel -> IADS"],
    "IADS Downward": [
        "IADS -> Vessel",
        "IADS -> Carrier Interceptor",
        "IADS -> Missile Interceptor",
    ],
    "Carrier Interceptor Upward": ["Carrier Interceptor -> Vessel"],
    "Vessel Downward": ["Vessel -> Carrier Interceptor"],
    "Missile Interceptor Upward": [
        "Missile Interceptor -> Carrier Interceptor"
    ],
    "Carrier Interceptor Downward": [
        "Carrier Interceptor -> Missile Interceptor"
    ],
}

DIRECTION_SCOPE_LABELS = {
    ("all", "upward"): "All Tiers Upward",
    ("all", "downward"): "All Tiers Downward",
    ("top", "upward"): "Vessel Upward",
    ("top", "downward"): "IADS Downward",
    ("middle", "upward"): "Carrier Interceptor Upward",
    ("middle", "downward"): "Vessel Downward",
    ("bottom", "upward"): "Missile Interceptor Upward",
    ("bottom", "downward"): "Carrier Interceptor Downward",
}

DEFAULT_OUTPUT_DIR = REPO_ROOT / "Logs/Analysis/Directional_Latencies"


def print_directional_definitions() -> None:
    """Prints the exact link overrides represented by directional labels."""
    print("\nDirectional Latency Link Definitions")
    for scope, links in DIRECTIONAL_LINKS.items():
        print(f"  {scope}: {', '.join(links)}")
    print("  All links not listed for a condition retain zero latency.")


def analyze(
        data: pd.DataFrame,
        bootstrap_samples: int = 2000,
        output_dir: Path | None = None) -> tuple[pd.DataFrame, pd.DataFrame]:
    """Creates directional curves and returns summaries and asymmetry."""
    family_data = data[data["family"] == DIRECTIONAL_LATENCIES].copy()
    if family_data.empty:
        raise ValueError("The dataset contains no directional latency runs.")
    print_directional_definitions()
    family_data["direction_scope"] = [
        DIRECTION_SCOPE_LABELS[(str(tier), str(direction))] for tier, direction
        in family_data[["tier", "direction"]].itertuples(index=False, name=None)
    ]

    group_columns = ["tier", "direction", "direction_scope", "mean_latency_s"]
    summary = summarize_conditions(
        family_data,
        group_columns,
        DEFAULT_ANALYSIS_METRICS,
        bootstrap_samples=bootstrap_samples,
    )
    upward = family_data[family_data["direction"] == "upward"]
    downward = family_data[family_data["direction"] == "downward"]
    asymmetry = paired_differences(
        upward,
        downward,
        on=["tier", "mean_latency_s", "seed"],
        metrics=DEFAULT_ANALYSIS_METRICS,
    )
    delta_metrics = [f"{metric}_delta" for metric in DEFAULT_ANALYSIS_METRICS]
    asymmetry_summary = summarize_conditions(
        asymmetry,
        ["tier", "mean_latency_s"],
        delta_metrics,
        bootstrap_samples=bootstrap_samples,
    )
    if output_dir is not None:
        output_dir.mkdir(parents=True, exist_ok=True)
        summary.to_csv(output_dir / "condition_summary.csv", index=False)
        asymmetry_summary.to_csv(output_dir /
                                 "upward_minus_downward_summary.csv",
                                 index=False)

    plot_metrics = available_metrics(family_data, DEFAULT_PLOT_METRICS,
                                     "Directional latency")
    print_mean_std_summary(
        summary,
        ["tier", "direction", "mean_latency_s"],
        plot_metrics,
        "Directional latency mean and standard deviation",
    )
    for metric in plot_metrics:
        plot_metric_curves(
            summary,
            "mean_latency_s",
            metric,
            series_column="direction_scope",
            x_label="One-Way Directional Latency [s]",
            title=f"Directional Latency: {METRIC_LABELS[metric]}",
            legend_title="Delayed Direction",
            output_path=(output_dir /
                         f"{metric}.png" if output_dir is not None else None),
        )
    plot_latency_position_maps(
        family_data,
        "mean_latency_s",
        "One-Way Directional Latency [s]",
        "Directional Latency",
        output_dir=output_dir,
        filename_prefix="directional_latency",
    )
    return summary, asymmetry_summary


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    add_common_cli_arguments(parser)
    parser.add_argument("--output_dir", type=Path, default=DEFAULT_OUTPUT_DIR)
    args = parser.parse_args()
    configure_logging()
    data = build_run_dataset(
        args.log_root,
        args.config_root,
        families=[DIRECTIONAL_LATENCIES],
    )
    analyze(data, args.bootstrap_samples,
            args.output_dir.expanduser().resolve())


if __name__ == "__main__":
    main()
