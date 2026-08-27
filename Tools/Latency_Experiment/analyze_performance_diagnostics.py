"""Runs advanced performance diagnostics on completed latency experiments."""

import argparse
from dataclasses import dataclass
from pathlib import Path

import numpy as np
import pandas as pd
from analyze_directional_latencies import DIRECTION_SCOPE_LABELS
from analyze_fixed_budget import ALLOCATION_LABELS
from analyze_single_pair_latencies import (SINGLE_TIER_COLORS,
                                           SINGLE_TIER_LABELS)
from latency_dataset import (ANALYSIS_FAMILIES, DIRECTIONAL_LATENCIES,
                             FIXED_BUDGET, GLOBAL_JITTER, GLOBAL_LATENCY,
                             REPO_ROOT, SINGLE_PAIR_LATENCIES,
                             add_common_cli_arguments, build_run_dataset,
                             configure_logging)
from latency_metrics import (ADVANCED_ANALYSIS_METRICS, METRIC_LABELS,
                             PROCESS_RUN_METRICS)
from latency_plots import plot_metric_curves
from latency_statistics import (available_metrics, paired_differences,
                                print_mean_std_summary, summarize_conditions)
from performance_plots import (plot_cost_safety_pareto,
                               plot_interceptor_lifecycle_fractions,
                               plot_threat_clearance_curves)


@dataclass(frozen=True)
class FamilySpec:
    label: str
    x_column: str
    x_label: str
    series_column: str | None
    group_columns: tuple[str, ...]
    series_colors: dict[object, str] | None = None


FAMILY_SPECS = {
    GLOBAL_LATENCY:
        FamilySpec(
            "Global Latency",
            "mean_latency_s",
            "Global Latency [s]",
            None,
            ("mean_latency_s",),
        ),
    SINGLE_PAIR_LATENCIES:
        FamilySpec(
            "Single Tier Latency",
            "mean_latency_s",
            "Single Tier Latency [s]",
            "tier_label",
            ("tier", "tier_label", "mean_latency_s"),
            SINGLE_TIER_COLORS,
        ),
    DIRECTIONAL_LATENCIES:
        FamilySpec(
            "Directional Latency",
            "mean_latency_s",
            "One-Way Directional Latency [s]",
            "direction_scope",
            ("tier", "direction", "direction_scope", "mean_latency_s"),
        ),
    FIXED_BUDGET:
        FamilySpec(
            "Fixed Budget Latency",
            "latency_budget_s",
            "End-to-End Latency Budget [s]",
            "allocation_label",
            ("allocation", "allocation_label", "latency_budget_s"),
        ),
    GLOBAL_JITTER:
        FamilySpec(
            "Global Jitter",
            "mean_latency_s",
            "Mean Global Latency [s]",
            "jitter_ratio_label",
            ("jitter_ratio", "jitter_ratio_label", "mean_latency_s",
             "jitter_std_s"),
        ),
}

FAMILY_OUTPUT_DIRS = {
    GLOBAL_LATENCY: "Global_Latency",
    SINGLE_PAIR_LATENCIES: "Single_Tier_Latencies",
    DIRECTIONAL_LATENCIES: "Directional_Latencies",
    FIXED_BUDGET: "Fixed_Budget",
    GLOBAL_JITTER: "Global_Jitter",
}

DEFAULT_OUTPUT_ROOT = REPO_ROOT / "Logs/Analysis/Performance_Diagnostics"

TIMING_METRICS = [
    "time_to_first_missile_interceptor_launch_s",
    "time_to_first_missile_interceptor_hit_s",
    "time_to_50_percent_threats_resolved_s",
    "time_to_90_percent_threats_resolved_s",
    "time_to_100_percent_threats_resolved_s",
    "mean_threat_exposure_time_s",
    "mean_missile_interceptor_flight_time_s",
    "p95_missile_interceptor_flight_time_s",
    "mean_missile_interceptor_hit_displacement_m",
    "mean_missile_interceptor_hit_speed_proxy_mps",
]

LIFECYCLE_METRICS = [
    "peak_active_missile_interceptors",
    "num_missile_interceptors_with_miss",
    "num_missile_interceptors_missed_then_hit",
    "missile_interceptor_miss_recovery_rate",
    "num_missile_interceptors_no_terminal_outcome",
    "missile_interceptors_per_threat_destroyed",
]

PAIRED_METRICS = [
    "num_missile_interceptors",
    "interceptor_efficiency",
    "minimum_intercept_distance_m",
    "mean_threat_spawn_to_destroyed_time_s",
    "time_to_100_percent_threats_resolved_s",
    "mean_missile_interceptor_flight_time_s",
    "peak_active_missile_interceptors",
    "missile_interceptors_per_threat_destroyed",
]

PAIRED_PLOT_METRICS = [
    "num_missile_interceptors",
    "interceptor_efficiency",
    "minimum_intercept_distance_m",
    "mean_threat_spawn_to_destroyed_time_s",
]


def _prepare_family_data(data: pd.DataFrame, family: str) -> pd.DataFrame:
    family_data = data[data["family"] == family].copy()
    if family_data.empty:
        raise ValueError(f"The dataset contains no {family} runs.")
    if family == SINGLE_PAIR_LATENCIES:
        family_data["tier_label"] = family_data["tier"].map(
            SINGLE_TIER_LABELS).fillna(family_data["tier"])
    if family == DIRECTIONAL_LATENCIES:
        family_data["direction_scope"] = [
            DIRECTION_SCOPE_LABELS[(str(tier), str(direction))]
            for tier, direction in family_data[
                ["tier", "direction"]].itertuples(index=False, name=None)
        ]
    if family == FIXED_BUDGET:
        family_data["allocation_label"] = family_data["allocation"].map(
            ALLOCATION_LABELS).fillna(family_data["allocation"])
    if family == GLOBAL_JITTER:
        family_data["jitter_ratio_label"] = family_data["jitter_ratio"].map(
            lambda value: f"{value:g}x Mean")
    return family_data


def _plot_metric_set(
    summary: pd.DataFrame,
    metrics: list[str],
    spec: FamilySpec,
    section_label: str,
    output_dir: Path | None,
) -> None:
    for metric in metrics:
        plot_metric_curves(
            summary,
            spec.x_column,
            metric,
            series_column=spec.series_column,
            series_colors=spec.series_colors,
            x_label=spec.x_label,
            title=
            f"{spec.label} {section_label.title()}: {METRIC_LABELS[metric]}",
            output_path=(output_dir / f"{section_label}_{metric}.png"
                         if output_dir is not None else None),
        )


def _paired_data(
    all_data: pd.DataFrame,
    family_data: pd.DataFrame,
    family: str,
) -> pd.DataFrame:
    if family == GLOBAL_LATENCY:
        baseline = family_data[family_data["mean_latency_s"] == 0]
        return paired_differences(family_data,
                                  baseline,
                                  on=["seed"],
                                  metrics=PAIRED_METRICS)
    if family == SINGLE_PAIR_LATENCIES:
        baseline = family_data[family_data["mean_latency_s"] == 0]
        return paired_differences(family_data,
                                  baseline,
                                  on=["tier", "seed"],
                                  metrics=PAIRED_METRICS)
    if family == DIRECTIONAL_LATENCIES:
        baseline = family_data[family_data["mean_latency_s"] == 0]
        return paired_differences(
            family_data,
            baseline,
            on=["tier", "direction", "seed"],
            metrics=PAIRED_METRICS,
        )
    if family == FIXED_BUDGET:
        baseline = family_data[family_data["latency_budget_s"] == 0]
        return paired_differences(
            family_data,
            baseline,
            on=["allocation", "seed"],
            metrics=PAIRED_METRICS,
        )
    if family == GLOBAL_JITTER:
        baseline = all_data[all_data["family"] == GLOBAL_LATENCY].copy()
        condition = family_data.copy()
        baseline["latency_pair_key"] = baseline["mean_latency_s"].round(9)
        condition["latency_pair_key"] = condition["mean_latency_s"].round(9)
        return paired_differences(
            condition,
            baseline,
            on=["latency_pair_key", "seed"],
            metrics=PAIRED_METRICS,
        )
    raise ValueError(f"Unsupported family: {family}")


def _plot_paired_deltas(
    all_data: pd.DataFrame,
    family_data: pd.DataFrame,
    family: str,
    spec: FamilySpec,
    bootstrap_samples: int,
    output_dir: Path | None,
) -> pd.DataFrame:
    paired = _paired_data(all_data, family_data, family)
    delta_metrics = [f"{metric}_delta" for metric in PAIRED_METRICS]
    delta_summary = summarize_conditions(
        paired,
        spec.group_columns,
        delta_metrics,
        bootstrap_samples=bootstrap_samples,
    )
    for metric in PAIRED_PLOT_METRICS:
        delta_metric = f"{metric}_delta"
        plot_metric_curves(
            delta_summary,
            spec.x_column,
            delta_metric,
            series_column=spec.series_column,
            series_colors=spec.series_colors,
            x_label=spec.x_label,
            y_label=f"Paired Change in {METRIC_LABELS[metric]}",
            title=f"{spec.label}: Paired Change in {METRIC_LABELS[metric]}",
            reference_y=0.0,
            output_path=(output_dir / f"paired_delta_{metric}.png"
                         if output_dir is not None else None),
        )
    return delta_summary


def _first_threshold_x(
    frame: pd.DataFrame,
    value_column: str,
    multiplier: float,
    lower_is_worse: bool,
    x_column: str,
) -> float:
    ordered = frame.sort_values(x_column)
    values = pd.to_numeric(ordered[value_column], errors="coerce")
    xvalues = pd.to_numeric(ordered[x_column], errors="coerce")
    finite = np.isfinite(values) & np.isfinite(xvalues)
    ordered = ordered.loc[finite]
    if ordered.empty:
        return np.nan
    baseline = float(ordered.iloc[0][value_column])
    threshold = baseline * multiplier
    candidates = ordered[ordered[value_column] <= threshold if lower_is_worse
                         else ordered[value_column] >= threshold]
    if candidates.empty:
        return np.nan
    return float(candidates.iloc[0][x_column])


def _print_breakpoints(summary: pd.DataFrame, spec: FamilySpec) -> None:
    groups = ([('all', summary)] if spec.series_column is None else list(
        summary.groupby(spec.series_column, dropna=False, sort=True)))
    print(f"\n{spec.label}: first threshold crossings")
    print("  Reference is the lowest measured latency/budget in each series.")
    for series_label, frame in groups:
        efficiency = _first_threshold_x(frame, "interceptor_efficiency_mean",
                                        0.95, True, spec.x_column)
        clearance = _first_threshold_x(
            frame, "time_to_90_percent_threats_resolved_s_mean", 1.10, False,
            spec.x_column)
        safety = _first_threshold_x(frame, "minimum_intercept_distance_m_mean",
                                    0.90, True, spec.x_column)

        def display(value: float) -> str:
            return f"{value:g} s" if np.isfinite(value) else "not crossed"

        print(f"  {series_label}: efficiency -5%={display(efficiency)}, "
              f"90%-clearance time +10%={display(clearance)}, "
              f"minimum distance -10%={display(safety)}")


def _print_fixed_budget_winners(summary: pd.DataFrame) -> None:
    print("\nFixed-budget allocation winners by budget")
    print("  budget_s  fewest_spawned  best_efficiency  fastest_clearance  "
          "largest_min_distance")
    for budget, frame in summary.groupby("latency_budget_s", sort=True):
        winners = []
        for column, direction in (
            ("num_missile_interceptors_mean", "min"),
            ("interceptor_efficiency_mean", "max"),
            ("time_to_90_percent_threats_resolved_s_mean", "min"),
            ("minimum_intercept_distance_m_mean", "max"),
        ):
            finite = frame[np.isfinite(
                pd.to_numeric(frame[column], errors="coerce"))]
            if finite.empty:
                winners.append("N/A")
                continue
            index = (finite[column].idxmin()
                     if direction == "min" else finite[column].idxmax())
            winners.append(str(finite.loc[index, "allocation"]))
        print(f"  {budget:8g}  " +
              "  ".join(f"{winner:17s}" for winner in winners))


def analyze_family(
    all_data: pd.DataFrame,
    family: str,
    bootstrap_samples: int,
    sections: set[str],
    output_dir: Path | None = None,
) -> pd.DataFrame:
    """Runs selected diagnostics for one latency experiment family."""
    spec = FAMILY_SPECS[family]
    family_data = _prepare_family_data(all_data, family)
    metrics = list(
        dict.fromkeys(PROCESS_RUN_METRICS + ADVANCED_ANALYSIS_METRICS + [
            "mean_threat_spawn_to_destroyed_time_s",
            "time_to_destroy_all_threats_s"
        ]))
    summary = summarize_conditions(
        family_data,
        spec.group_columns,
        metrics,
        bootstrap_samples=bootstrap_samples,
    )
    if output_dir is not None:
        output_dir.mkdir(parents=True, exist_ok=True)
        summary.to_csv(output_dir / "condition_summary.csv", index=False)

    if "timing" in sections:
        timing_metrics = available_metrics(family_data, TIMING_METRICS,
                                           f"{spec.label} timing")
        print_mean_std_summary(
            summary, spec.group_columns, timing_metrics,
            f"{spec.label} timing mean and standard deviation")
        _plot_metric_set(summary, timing_metrics, spec, "timing", output_dir)
        plot_threat_clearance_curves(
            family_data,
            spec.x_column,
            spec.series_column,
            spec.x_label,
            spec.label,
            output_path=(output_dir / "threat_clearance_curves.png"
                         if output_dir is not None else None),
        )

    if "lifecycle" in sections:
        lifecycle_metrics = available_metrics(family_data, LIFECYCLE_METRICS,
                                              f"{spec.label} lifecycle")
        print_mean_std_summary(
            summary, spec.group_columns, lifecycle_metrics,
            f"{spec.label} lifecycle mean and standard deviation")
        _plot_metric_set(summary, lifecycle_metrics, spec, "lifecycle",
                         output_dir)
        plot_interceptor_lifecycle_fractions(
            summary,
            spec.x_column,
            spec.series_column,
            spec.x_label,
            spec.label,
            output_path=(output_dir / "interceptor_lifecycle.png"
                         if output_dir is not None else None),
        )

    if "comparison" in sections:
        plot_cost_safety_pareto(
            summary,
            spec.x_column,
            spec.series_column,
            spec.x_label,
            spec.label,
            output_path=(output_dir / "resource_cost_versus_safety.png"
                         if output_dir is not None else None),
        )
        delta_summary = _plot_paired_deltas(all_data, family_data, family, spec,
                                            bootstrap_samples, output_dir)
        if output_dir is not None:
            delta_summary.to_csv(output_dir / "paired_delta_summary.csv",
                                 index=False)
        _print_breakpoints(summary, spec)
        if family == FIXED_BUDGET:
            _print_fixed_budget_winners(summary)
    return summary


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    add_common_cli_arguments(parser)
    parser.add_argument(
        "--family",
        choices=ANALYSIS_FAMILIES + ["all"],
        default=GLOBAL_LATENCY,
        help="Latency experiment family to analyze (default: global_latency).",
    )
    parser.add_argument(
        "--sections",
        nargs="+",
        choices=["all", "timing", "lifecycle", "comparison"],
        default=["all"],
        help="Diagnostic plot groups to show (default: all).",
    )
    parser.add_argument(
        "--output_root",
        type=Path,
        default=DEFAULT_OUTPUT_ROOT,
        help="Root directory for diagnostic CSVs and figures.",
    )
    args = parser.parse_args()
    configure_logging()

    requested_families = (ANALYSIS_FAMILIES
                          if args.family == "all" else [args.family])
    load_families = list(requested_families)
    if GLOBAL_JITTER in requested_families and GLOBAL_LATENCY not in load_families:
        load_families.append(GLOBAL_LATENCY)
    data = build_run_dataset(
        args.log_root,
        args.config_root,
        families=load_families,
    )
    sections = ({"timing", "lifecycle", "comparison"}
                if "all" in args.sections else set(args.sections))
    output_root = args.output_root.expanduser().resolve()
    for family in requested_families:
        analyze_family(
            data,
            family,
            args.bootstrap_samples,
            sections,
            output_root / FAMILY_OUTPUT_DIRS[family],
        )


if __name__ == "__main__":
    main()
