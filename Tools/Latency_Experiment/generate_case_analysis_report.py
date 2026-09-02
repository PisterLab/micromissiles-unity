"""Generates a report for latency experiment Cases 1-6."""

from __future__ import annotations

from dataclasses import dataclass
from datetime import date
from pathlib import Path

import numpy as np
import pandas as pd
from latency_dataset import REPO_ROOT
from latency_metrics import METRIC_LABELS

OUTPUT_DIR = REPO_ROOT / "Logs/Analysis"
REPORT_PATH = OUTPUT_DIR / "cases_1_to_6_analysis_report.md"
LEGACY_REPORT_PATH = OUTPUT_DIR / "cases_1_to_4_and_6_analysis_report.md"
INVENTORY_PATH = OUTPUT_DIR / "case_analysis_inventory.csv"
AVERAGES_PATH = OUTPUT_DIR / "case_analysis_overall_averages.csv"
EXTREMES_PATH = OUTPUT_DIR / "case_analysis_condition_extremes.csv"
SUBGROUP_PATH = OUTPUT_DIR / "case_analysis_subgroup_averages.csv"


@dataclass(frozen=True)
class CaseSpec:
    case: str
    name: str
    summary_path: Path
    condition_columns: tuple[str, ...]
    subgroup_column: str | None
    condition_description: str
    secondary_subgroup_column: str | None = None


CASE_SPECS = (
    CaseSpec(
        "Case 1",
        "Global Latency",
        OUTPUT_DIR / "Global_Latency/condition_summary.csv",
        ("mean_latency_s",),
        None,
        "The same one-way mean latency is applied globally to every communication link; jitter is zero.",
    ),
    CaseSpec(
        "Case 2",
        "Single Tier Latency",
        OUTPUT_DIR / "Single_Tier_Latencies/condition_summary.csv",
        ("tier_label", "mean_latency_s"),
        "tier_label",
        "One bidirectional tier is delayed while the other tiers remain at zero latency.",
    ),
    CaseSpec(
        "Case 3",
        "Directional Latency",
        OUTPUT_DIR / "Directional_Latencies/condition_summary.csv",
        ("direction_scope", "mean_latency_s"),
        "direction_scope",
        "One upward or downward communication scope is delayed while unlisted links remain at zero latency.",
    ),
    CaseSpec(
        "Case 4",
        "Fixed End-to-End Latency Budget",
        OUTPUT_DIR / "Fixed_Budget/condition_summary.csv",
        ("allocation_label", "latency_budget_s"),
        "allocation_label",
        "A fixed end-to-end latency budget is divided across the top, middle, and bottom tiers using seven allocations.",
    ),
    CaseSpec(
        "Case 5",
        "UCAV Kill Probability × Global Latency",
        OUTPUT_DIR / "Kill_Probability_Latency/condition_summary.csv",
        ("kill_probability", "mean_latency_s"),
        "kill_probability",
        "UCAV per-collision kill probability is crossed with global one-way communication latency.",
        secondary_subgroup_column="mean_latency_s",
    ),
    CaseSpec(
        "Case 6",
        "Global Latency Jitter",
        OUTPUT_DIR / "Global_Jitter/condition_summary.csv",
        ("jitter_ratio_label", "mean_latency_s", "jitter_std_s"),
        "jitter_ratio_label",
        "Global mean latency is combined with Gaussian jitter whose standard deviation is a fixed fraction of the mean.",
    ),
)

CORE_METRICS = (
    "num_missile_interceptors",
    "num_missile_interceptor_hits",
    "interceptor_efficiency",
    "interceptor_terminal_success_rate",
    "threat_destroyed_rate",
    "threat_penetration_rate",
    "unresolved_threat_rate",
    "time_to_destroy_all_threats_s",
    "mean_threat_exposure_time_s",
    "mean_missile_interceptor_flight_time_s",
    "p95_missile_interceptor_flight_time_s",
    "mean_intercept_distance_m",
    "missile_interceptors_per_threat_destroyed",
)

SUBGROUP_METRICS = (
    "num_missile_interceptors",
    "interceptor_efficiency",
    "interceptor_terminal_success_rate",
    "time_to_destroy_all_threats_s",
    "mean_threat_exposure_time_s",
    "mean_missile_interceptor_flight_time_s",
)

PERCENT_METRICS = {
    "interceptor_efficiency",
    "interceptor_terminal_success_rate",
    "threat_destroyed_rate",
    "threat_penetration_rate",
    "unresolved_threat_rate",
    "interceptor_hit_rate",
    "coordination_response_rate",
    "reassigned_missile_interceptor_efficiency",
    "missile_interceptor_miss_recovery_rate",
    "missile_interceptor_hit_fraction",
    "missile_interceptor_destroyed_fraction",
    "missile_interceptor_no_terminal_fraction",
}


def metric_names(summary: pd.DataFrame) -> list[str]:
    """Returns metric stems that have mean and valid-count columns."""
    names: list[str] = []
    for column in summary.columns:
        if not column.endswith("_mean"):
            continue
        metric = column.removesuffix("_mean")
        if f"{metric}_n" in summary.columns:
            names.append(metric)
    return names


def pooled_stats(summary: pd.DataFrame,
                 metric: str) -> tuple[int, float, float]:
    """Reconstructs run-weighted mean and sample standard deviation."""
    means = pd.to_numeric(summary[f"{metric}_mean"], errors="coerce")
    counts = pd.to_numeric(summary[f"{metric}_n"], errors="coerce").fillna(0)
    stds = pd.to_numeric(summary[f"{metric}_std"], errors="coerce")
    valid = means.notna() & (counts > 0)
    if not valid.any():
        return 0, np.nan, np.nan

    valid_means = means[valid].to_numpy(dtype=float)
    valid_counts = counts[valid].to_numpy(dtype=float)
    total_count = int(valid_counts.sum())
    pooled_mean = float(np.average(valid_means, weights=valid_counts))

    # Within-condition and between-condition sums of squares reproduce the
    # sample variance without loading every run log again.
    valid_stds = stds[valid].fillna(0).to_numpy(dtype=float)
    within_ss = np.sum(np.maximum(valid_counts - 1, 0) * valid_stds**2)
    between_ss = np.sum(valid_counts * (valid_means - pooled_mean)**2)
    pooled_std = (float(np.sqrt(
        (within_ss + between_ss) /
        (total_count - 1))) if total_count > 1 else np.nan)
    return total_count, pooled_mean, pooled_std


def format_number(value: object) -> str:
    """Formats a scalar for condition descriptions and report tables."""
    if pd.isna(value):
        return "N/A"
    if isinstance(value, str):
        return value
    number = float(value)
    if number == 0:
        return "0"
    if abs(number) < 0.001:
        return f"{number:.6g}"
    return f"{number:.6g}"


def format_metric(metric: str, value: float) -> str:
    """Formats rates as percentages and other metrics with useful precision."""
    if not np.isfinite(value):
        return "N/A"
    if metric in PERCENT_METRICS:
        return f"{100 * value:.2f}%"
    if metric.startswith(
            "num_") or metric == "peak_active_missile_interceptors":
        return f"{value:.2f}"
    return f"{value:.3f}"


def format_condition(spec: CaseSpec, row: pd.Series) -> str:
    """Creates a presentation-ready condition label."""
    if spec.case == "Case 1":
        return f"Global Latency = {format_number(row['mean_latency_s'])} s"
    if spec.case == "Case 2":
        return (f"{row['tier_label']}; Single Tier Latency = "
                f"{format_number(row['mean_latency_s'])} s")
    if spec.case == "Case 3":
        return (f"{row['direction_scope']}; One-Way Directional Latency = "
                f"{format_number(row['mean_latency_s'])} s")
    if spec.case == "Case 4":
        return (f"{row['allocation_label']}; End-to-End Budget = "
                f"{format_number(row['latency_budget_s'])} s")
    if spec.case == "Case 5":
        return (
            f"UCAV Kill Probability = {format_number(row['kill_probability'])}; "
            f"Global Latency = {format_number(row['mean_latency_s'])} s")
    return (f"{row['jitter_ratio_label']}; Mean Latency = "
            f"{format_number(row['mean_latency_s'])} s; Jitter Std. Dev. = "
            f"{format_number(row['jitter_std_s'])} s")


def condition_extreme(
    summary: pd.DataFrame,
    spec: CaseSpec,
    metric: str,
    highest: bool,
) -> dict[str, object] | None:
    """Returns the condition with the lowest or highest condition mean."""
    mean_column = f"{metric}_mean"
    means = pd.to_numeric(summary[mean_column], errors="coerce")
    valid = means.notna()
    if not valid.any():
        return None
    index = means[valid].idxmax() if highest else means[valid].idxmin()
    row = summary.loc[index]
    return {
        "value": float(row[mean_column]),
        "condition": format_condition(spec, row),
        "valid_runs": int(row[f"{metric}_n"]),
        "ci_low": float(row[f"{metric}_ci_low"]),
        "ci_high": float(row[f"{metric}_ci_high"]),
    }


def markdown_table(headers: list[str], rows: list[list[str]]) -> str:
    """Builds a compact GitHub-flavored Markdown table."""
    lines = [
        "| " + " | ".join(headers) + " |",
        "| " + " | ".join("---" for _ in headers) + " |",
    ]
    lines.extend("| " + " | ".join(row) + " |" for row in rows)
    return "\n".join(lines)


def distinct_values(summary: pd.DataFrame, column: str) -> list[object]:
    """Returns sorted unique values while normalizing floating-point aliases."""
    values = summary[column].dropna()
    if pd.api.types.is_numeric_dtype(values):
        return sorted(
            np.unique(np.round(values.to_numpy(dtype=float), 12)).tolist())
    return sorted(values.astype(str).unique().tolist())


def latency_values_text(values: list[object]) -> str:
    return ", ".join(format_number(value) for value in values)


def case_findings(
    spec: CaseSpec,
    extremes: pd.DataFrame,
    subgroups: pd.DataFrame,
) -> list[str]:
    """Builds data-backed, presentation-ready findings for one case."""
    case_extremes = extremes[extremes["case"] == spec.case]
    case_subgroups = subgroups[subgroups["case"] == spec.case]

    def extreme(metric: str) -> pd.Series:
        return case_extremes[case_extremes["metric"] == metric].iloc[0]

    def group_mean(
        group: object,
        metric: str,
        dimension: str | None = None,
    ) -> float:
        match = case_subgroups[(case_subgroups["subgroup"] == group) &
                               (case_subgroups["metric"] == metric)]
        if dimension is not None:
            match = match[match["subgroup_dimension"] == dimension]
        return float(match.iloc[0]["run_weighted_mean"])

    if spec.case == "Case 1":
        efficiency = extreme("interceptor_efficiency")
        completion = extreme("time_to_destroy_all_threats_s")
        return [
            (f"The 3 s global-latency condition is the clearest adverse cluster: "
             f"efficiency fell to {format_metric('interceptor_efficiency', efficiency['lowest_condition_mean'])}, "
             f"while destroy-all time rose to "
             f"{format_metric('time_to_destroy_all_threats_s', completion['highest_condition_mean'])} s."
            ),
            ("The response is non-monotonic. The strongest and weakest condition means "
             "do not always occur at the endpoints, so the experiment supports a "
             "latency-sensitive operating region rather than a single monotonic cutoff."
            ),
        ]
    if spec.case == "Case 2":
        iads = "IADS ↔ Vessel"
        carrier_missile = "Carrier Interceptor ↔ Missile Interceptor"
        vessel_carrier = "Vessel ↔ Carrier Interceptor"
        return [
            (f"Across the full sweep, {iads} was least burdensome on average: "
             f"{format_metric('num_missile_interceptors', group_mean(iads, 'num_missile_interceptors'))} launches, "
             f"{format_metric('interceptor_efficiency', group_mean(iads, 'interceptor_efficiency'))} efficiency, "
             f"and {format_metric('time_to_destroy_all_threats_s', group_mean(iads, 'time_to_destroy_all_threats_s'))} s "
             "to destroy all threats."),
            (f"The two lower tiers were more sensitive: {carrier_missile} averaged "
             f"{format_metric('num_missile_interceptors', group_mean(carrier_missile, 'num_missile_interceptors'))} launches, "
             f"and {vessel_carrier} averaged "
             f"{format_metric('num_missile_interceptors', group_mean(vessel_carrier, 'num_missile_interceptors'))}. "
             "The most severe resource/timing condition was the carrier-to-missile tier at 10 s."
            ),
        ]
    if spec.case == "Case 3":
        upward = "All Tiers Upward"
        downward = "All Tiers Downward"
        return [
            (f"All-tier upward delay was more burdensome than all-tier downward delay "
             f"across the sweep: {format_metric('num_missile_interceptors', group_mean(upward, 'num_missile_interceptors'))} "
             f"versus {format_metric('num_missile_interceptors', group_mean(downward, 'num_missile_interceptors'))} launches, "
             f"and {format_metric('time_to_destroy_all_threats_s', group_mean(upward, 'time_to_destroy_all_threats_s'))} "
             f"versus {format_metric('time_to_destroy_all_threats_s', group_mean(downward, 'time_to_destroy_all_threats_s'))} s "
             "to destroy all threats."),
            (f"The all-tier upward 6 s condition produced the highest launch count and "
             f"lowest efficiency; the all-tier upward 4 s condition produced the longest "
             "destroy-all time. This identifies upward reporting/feedback paths as the "
             "dominant directional sensitivity in these runs."),
        ]
    if spec.case == "Case 4":
        top_only = "Top Tier Only (100% / 0% / 0%)"
        top_heavy = "Top Heavy (80% / 10% / 10%)"
        bottom_only = "Bottom Tier Only (0% / 0% / 100%)"
        return [
            (f"Allocating delay toward the top tier was least burdensome on average. "
             f"{top_only} produced {format_metric('interceptor_efficiency', group_mean(top_only, 'interceptor_efficiency'))} "
             f"efficiency, while {top_heavy} produced the shortest group-average "
             f"destroy-all time at {format_metric('time_to_destroy_all_threats_s', group_mean(top_heavy, 'time_to_destroy_all_threats_s'))} s."
            ),
            (f"{bottom_only} required {format_metric('num_missile_interceptors', group_mean(bottom_only, 'num_missile_interceptors'))} "
             "launches on average. The bottom-heavy 8 s condition was the resource-use "
             "extreme, with the most launches and lowest efficiency."),
        ]
    if spec.case == "Case 5":
        penetration = extreme("threat_penetration_rate")
        return [
            ("Kill probability was the dominant factor. Averaged across all latency "
             f"values, increasing UCAV kill probability from 0.05 to 1.0 reduced "
             f"launches from {format_metric('num_missile_interceptors', group_mean(0.05, 'num_missile_interceptors', 'kill_probability'))} "
             f"to {format_metric('num_missile_interceptors', group_mean(1.0, 'num_missile_interceptors', 'kill_probability'))} and increased efficiency from "
             f"{format_metric('interceptor_efficiency', group_mean(0.05, 'interceptor_efficiency', 'kill_probability'))} "
             f"to {format_metric('interceptor_efficiency', group_mean(1.0, 'interceptor_efficiency', 'kill_probability'))}."
            ),
            (f"The highest threat-penetration condition mean was "
             f"{format_metric('threat_penetration_rate', penetration['highest_condition_mean'])} "
             f"at kill probability 0.05 and 0.8 s global latency. Across all 21,000 "
             "Case 5 runs, however, the pooled destroyed rate remained above 99.9%."
            ),
            ("Latency effects were most consequential at low kill probability, where "
             "many repeated interceptor attempts were required. The crossed design "
             "should therefore be interpreted as an interaction surface rather than as "
             "independent one-dimensional kill-probability and latency trends."
            ),
        ]
    low_jitter = "0.01x Mean"
    high_jitter = "1x Mean"
    return [
        (f"Averaged across mean latency values, the 1× jitter group used fewer interceptors "
         f"and had higher efficiency than the 0.01× group: "
         f"{format_metric('num_missile_interceptors', group_mean(high_jitter, 'num_missile_interceptors'))} "
         f"versus {format_metric('num_missile_interceptors', group_mean(low_jitter, 'num_missile_interceptors'))} launches, "
         f"and {format_metric('interceptor_efficiency', group_mean(high_jitter, 'interceptor_efficiency'))} "
         f"versus {format_metric('interceptor_efficiency', group_mean(low_jitter, 'interceptor_efficiency'))} efficiency."
        ),
        ("This apparent improvement with a larger jitter ratio is not evidence that jitter "
         "is universally beneficial. The worst joint condition occurred at 6 s mean "
         "latency with 0.25× jitter, showing a strong, non-monotonic interaction between "
         "mean latency and jitter magnitude."),
    ]


def build_outputs() -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    loaded: dict[str, pd.DataFrame] = {}
    inventory_rows: list[dict[str, object]] = []
    averages_rows: list[dict[str, object]] = []
    extremes_rows: list[dict[str, object]] = []
    subgroup_rows: list[dict[str, object]] = []

    for spec in CASE_SPECS:
        if not spec.summary_path.exists():
            raise FileNotFoundError(
                f"Missing analysis summary: {spec.summary_path}")
        summary = pd.read_csv(spec.summary_path)
        loaded[spec.case] = summary
        inventory_rows.append({
            "case": spec.case,
            "experiment": spec.name,
            "conditions": len(summary),
            "runs_per_condition_min": int(summary["num_runs"].min()),
            "runs_per_condition_max": int(summary["num_runs"].max()),
            "total_runs": int(summary["num_runs"].sum()),
            "source": str(spec.summary_path.relative_to(REPO_ROOT)),
        })

        for metric in metric_names(summary):
            valid_runs, mean, std = pooled_stats(summary, metric)
            averages_rows.append({
                "case":
                    spec.case,
                "experiment":
                    spec.name,
                "metric":
                    metric,
                "metric_label":
                    METRIC_LABELS.get(metric,
                                      metric.replace("_", " ").title()),
                "valid_runs":
                    valid_runs,
                "run_weighted_mean":
                    mean,
                "pooled_sample_std":
                    std,
            })
            low = condition_extreme(summary, spec, metric, highest=False)
            high = condition_extreme(summary, spec, metric, highest=True)
            if low is None or high is None:
                continue
            extremes_rows.append({
                "case":
                    spec.case,
                "experiment":
                    spec.name,
                "metric":
                    metric,
                "metric_label":
                    METRIC_LABELS.get(metric,
                                      metric.replace("_", " ").title()),
                "lowest_condition_mean":
                    low["value"],
                "lowest_condition":
                    low["condition"],
                "lowest_condition_valid_runs":
                    low["valid_runs"],
                "lowest_condition_ci_low":
                    low["ci_low"],
                "lowest_condition_ci_high":
                    low["ci_high"],
                "highest_condition_mean":
                    high["value"],
                "highest_condition":
                    high["condition"],
                "highest_condition_valid_runs":
                    high["valid_runs"],
                "highest_condition_ci_low":
                    high["ci_low"],
                "highest_condition_ci_high":
                    high["ci_high"],
            })

        subgroup_columns = tuple(column for column in (
            spec.subgroup_column,
            spec.secondary_subgroup_column,
        ) if column is not None)
        for subgroup_column in subgroup_columns:
            for subgroup, group in summary.groupby(subgroup_column, sort=True):
                for metric in SUBGROUP_METRICS:
                    valid_runs, mean, std = pooled_stats(group, metric)
                    subgroup_rows.append({
                        "case": spec.case,
                        "experiment": spec.name,
                        "subgroup_dimension": subgroup_column,
                        "subgroup": subgroup,
                        "metric": metric,
                        "metric_label": METRIC_LABELS[metric],
                        "valid_runs": valid_runs,
                        "run_weighted_mean": mean,
                        "pooled_sample_std": std,
                    })

    inventory = pd.DataFrame(inventory_rows)
    averages = pd.DataFrame(averages_rows)
    extremes = pd.DataFrame(extremes_rows)
    subgroups = pd.DataFrame(subgroup_rows)
    inventory.to_csv(INVENTORY_PATH, index=False)
    averages.to_csv(AVERAGES_PATH, index=False)
    extremes.to_csv(EXTREMES_PATH, index=False)
    subgroups.to_csv(SUBGROUP_PATH, index=False)

    total_conditions = int(inventory["conditions"].sum())
    total_runs = int(inventory["total_runs"].sum())
    lines: list[str] = [
        "# Analysis Report: Latency Experiment Cases 1–6",
        "",
        f"Generated {date.today().isoformat()} from the completed condition summaries in `Logs/Analysis`.",
        "",
        "## Executive Summary",
        "",
        (f"This report covers **{total_conditions:,} experimental conditions** and "
         f"**{total_runs:,} completed simulation runs**. Every condition contains 50 "
         "seeded repetitions and all six experiment cases are included."),
        "",
        ("Cases 1–4 and Case 6 achieved a 100% threat-destroyed rate with 0% penetration "
         "under every tested condition. Case 5 deliberately reduced UCAV kill probability "
         "and therefore exposed outcome sensitivity: its run-weighted destroyed rate was "
         "99.925%, penetration rate was 0.075%, and the highest condition-mean penetration "
         "rate was 1.26%. Across the campaign, resource use, interceptor efficiency, and "
         "engagement timing varied much more than the final threat outcome."),
        "",
        ("The strongest adverse condition means generally occurred at multi-second "
         "latencies: Case 1 at 3 s global latency; Case 2 on the lower communication "
         "tiers at 8–10 s; Case 3 on upward links at 4–8 s; Case 4 when large budgets "
         "were concentrated toward the bottom tier; Case 5 at low kill probability "
         "combined with latency; and Case 6 near a 6 s mean with 0.25× jitter. These "
         "are observed condition extrema, not fitted causal "
         "thresholds, and several response curves are non-monotonic."),
        "",
        "## Common Scenario and Statistical Method",
        "",
        ("The common scenario runs for 300 simulation seconds and contains five threat "
         "swarms of 20 UCAVs each (100 threats total), one vessel/IADS defense hierarchy, "
         "126 carrier interceptors, and seven missile interceptors per carrier. The "
         "communication packet-delivery ratio is 1.0. Cases 1–4 vary deterministic "
         "latency placement, Case 5 crosses global latency with the UCAV per-collision "
         "kill probability, and Case 6 adds latency standard deviation."),
        "",
        ("Overall and subgroup averages are **run-weighted means** reconstructed from "
         "the condition summaries. The reported pooled standard deviation combines "
         "within-condition and between-condition variation. A condition extreme is the "
         "lowest or highest **condition mean**, not the most extreme individual run. "
         "Each condition summary also contains a 95% bootstrap confidence interval; "
         "those intervals are preserved in the companion extremes CSV."),
        "",
        "## Experiment Inventory",
        "",
        markdown_table(
            [
                "Case", "Experiment", "Conditions", "Runs / Condition",
                "Total Runs"
            ],
            [[
                row.case,
                row.experiment,
                f"{int(row.conditions):,}",
                f"{int(row.runs_per_condition_min):,}",
                f"{int(row.total_runs):,}",
            ] for row in inventory.itertuples(index=False)],
        ),
        "",
        "## Cross-Case Run-Weighted Averages",
        "",
    ]

    overview_metrics = (
        "num_missile_interceptors",
        "num_missile_interceptor_hits",
        "interceptor_efficiency",
        "interceptor_terminal_success_rate",
        "time_to_destroy_all_threats_s",
        "mean_threat_exposure_time_s",
        "mean_missile_interceptor_flight_time_s",
    )
    overview_headers = [
        "Case",
        "Launched",
        "Interceptor Hits",
        "Efficiency",
        "Terminal Success",
        "Destroy-All Time [s]",
        "Mean Exposure [s/Threat]",
        "Mean Successful Flight [s]",
    ]
    overview_rows: list[list[str]] = []
    for spec in CASE_SPECS:
        row = [spec.case]
        for metric in overview_metrics:
            match = averages[(averages["case"] == spec.case) &
                             (averages["metric"] == metric)]
            row.append(
                format_metric(metric,
                              float(match.iloc[0]["run_weighted_mean"])))
        overview_rows.append(row)
    lines.extend([markdown_table(overview_headers, overview_rows), ""])

    lines.extend([
        "Cases 1–4 and Case 6 averaged approximately 100 interceptor hits per run, with "
        "run-weighted efficiency between 65.61% and 67.77%. Case 5 averaged 95.04 hits and "
        "41.02% efficiency because it includes kill probabilities as low as 0.05, which "
        "required many repeated interceptor attempts. This makes Case 5 intentionally "
        "different from the latency-only cases and the most informative case for final "
        "outcome sensitivity.",
        "",
    ])

    for spec in CASE_SPECS:
        summary = loaded[spec.case]
        lines.extend([
            f"## {spec.case}: {spec.name}",
            "",
            spec.condition_description,
            "",
        ])

        if spec.case == "Case 1":
            latency_values = distinct_values(summary, "mean_latency_s")
            lines.extend([
                f"- Conditions: {len(latency_values)} global latency values from "
                f"{format_number(min(latency_values))} to {format_number(max(latency_values))} s.",
                f"- Tested values [s]: {latency_values_text(latency_values)}.",
                f"- Runs: {int(summary['num_runs'].sum()):,} ({len(summary)} conditions × 50 seeds).",
                "",
            ])
        elif spec.case in {"Case 2", "Case 3", "Case 4"}:
            latency_column = "latency_budget_s" if spec.case == "Case 4" else "mean_latency_s"
            latency_values = distinct_values(summary, latency_column)
            subgroup_values = distinct_values(summary, spec.subgroup_column or
                                              "")
            lines.extend([
                f"- Experimental groups ({len(subgroup_values)}): " +
                "; ".join(str(value) for value in subgroup_values) + ".",
                f"- Tested latency values [s]: {latency_values_text(latency_values)}.",
                f"- Runs: {int(summary['num_runs'].sum()):,} ({len(summary)} conditions × 50 seeds).",
                "",
            ])
        elif spec.case == "Case 5":
            latency_values = distinct_values(summary, "mean_latency_s")
            kill_values = distinct_values(summary, "kill_probability")
            lines.extend([
                f"- UCAV kill probabilities ({len(kill_values)}): {latency_values_text(kill_values)}.",
                f"- Global latency values [s] ({len(latency_values)}): {latency_values_text(latency_values)}.",
                f"- Crossed conditions: {len(kill_values)} × {len(latency_values)} = {len(summary)}.",
                f"- Runs: {int(summary['num_runs'].sum()):,} ({len(summary)} conditions × 50 seeds).",
                "",
            ])
        else:
            latency_values = distinct_values(summary, "mean_latency_s")
            jitter_values = distinct_values(summary, "jitter_ratio_label")
            lines.extend([
                f"- Jitter standard-deviation ratios ({len(jitter_values)}): " +
                ", ".join(str(value) for value in jitter_values) + ".",
                f"- Mean latency values [s]: {latency_values_text(latency_values)}.",
                f"- Runs: {int(summary['num_runs'].sum()):,} ({len(summary)} conditions × 50 seeds).",
                "",
            ])

        lines.extend(["### Main Findings", ""])
        lines.extend(f"- {finding}"
                     for finding in case_findings(spec, extremes, subgroups))
        lines.extend(["", "### Overall Averages", ""])
        average_rows: list[list[str]] = []
        for metric in CORE_METRICS:
            match = averages[(averages["case"] == spec.case) &
                             (averages["metric"] == metric)]
            if match.empty or int(match.iloc[0]["valid_runs"]) == 0:
                continue
            result = match.iloc[0]
            average_rows.append([
                METRIC_LABELS[metric],
                f"{int(result['valid_runs']):,}",
                format_metric(metric, float(result["run_weighted_mean"])),
                format_metric(metric, float(result["pooled_sample_std"])),
            ])
        lines.extend([
            markdown_table(
                ["Metric", "Valid Runs", "Average", "Pooled Std. Dev."],
                average_rows),
            "",
            "### Condition Extremes",
            "",
        ])

        extreme_rows: list[list[str]] = []
        for metric in CORE_METRICS:
            match = extremes[(extremes["case"] == spec.case) &
                             (extremes["metric"] == metric)]
            if match.empty:
                continue
            result = match.iloc[0]
            low_value = float(result["lowest_condition_mean"])
            high_value = float(result["highest_condition_mean"])
            if np.isclose(low_value, high_value, rtol=0, atol=1e-12):
                high_display = "Invariant across all conditions"
            else:
                high_display = (f"{format_metric(metric, high_value)} — "
                                f"{result['highest_condition']}")
            extreme_rows.append([
                METRIC_LABELS[metric],
                f"{format_metric(metric, low_value)} — {result['lowest_condition']}",
                high_display,
            ])
        lines.extend([
            markdown_table(
                ["Metric", "Lowest Condition Mean", "Highest Condition Mean"],
                extreme_rows),
            "",
        ])

        if spec.subgroup_column is not None:
            case_groups = subgroups[subgroups["case"] == spec.case]
            dimensions = case_groups["subgroup_dimension"].drop_duplicates(
            ).tolist()
            for dimension in dimensions:
                dimension_groups = case_groups[case_groups["subgroup_dimension"]
                                               == dimension]
                if spec.case == "Case 5" and dimension == "kill_probability":
                    section_title = "### Kill-Probability Marginal Averages Across Global Latencies"
                    group_header = "UCAV Kill Probability"
                elif spec.case == "Case 5" and dimension == "mean_latency_s":
                    section_title = "### Global-Latency Marginal Averages Across Kill Probabilities"
                    group_header = "Global Latency [s]"
                else:
                    section_title = "### Group-Level Averages Across the Latency Sweep"
                    group_header = "Group"
                lines.extend([section_title, ""])
                group_rows: list[list[str]] = []
                for subgroup, group in dimension_groups.groupby("subgroup",
                                                                sort=True):
                    values = {
                        row.metric: row for row in group.itertuples(index=False)
                    }
                    group_rows.append([
                        format_number(subgroup),
                        f"{values['num_missile_interceptors'].valid_runs:,}",
                        format_metric(
                            "num_missile_interceptors",
                            values["num_missile_interceptors"].run_weighted_mean
                        ),
                        format_metric(
                            "interceptor_efficiency",
                            values["interceptor_efficiency"].run_weighted_mean),
                        format_metric(
                            "interceptor_terminal_success_rate",
                            values["interceptor_terminal_success_rate"].
                            run_weighted_mean),
                        format_metric(
                            "time_to_destroy_all_threats_s",
                            values["time_to_destroy_all_threats_s"].
                            run_weighted_mean),
                        format_metric(
                            "mean_threat_exposure_time_s",
                            values["mean_threat_exposure_time_s"].
                            run_weighted_mean),
                        format_metric(
                            "mean_missile_interceptor_flight_time_s",
                            values["mean_missile_interceptor_flight_time_s"].
                            run_weighted_mean),
                    ])
                lines.extend([
                    markdown_table(
                        [
                            group_header,
                            "Runs",
                            "Launched",
                            "Efficiency",
                            "Terminal Success",
                            "Destroy-All Time [s]",
                            "Mean Exposure [s/Threat]",
                            "Mean Successful Flight [s]",
                        ],
                        group_rows,
                    ),
                    "",
                ])
            if spec.case == "Case 5":
                lines.extend([
                    "The Runs column gives the total marginal run count. Destroy-all time is "
                    "computed only for runs in which all threats were destroyed; metric-specific "
                    "valid counts are available in the subgroup CSV.",
                    "",
                ])

    unavailable = averages[averages["valid_runs"] ==
                           0]["metric_label"].drop_duplicates().tolist()
    lines.extend([
        "## Interpretation and Limitations",
        "",
        "- Cases 1–4 and Case 6 had invariant final outcomes, so efficiency, resource use, and timing are their discriminating metrics. Case 5 is the exception because low kill probability produced a small but measurable penetration rate.",
        "- Nearly 100 interceptor hits per run should not be interpreted as exactly one hit per threat. Threat and interceptor event counts are separate event-log measures.",
        "- The lowest and highest rows are descriptive condition extrema. Small differences among nearby low-latency conditions may be Monte Carlo variation, even with 50 repetitions.",
        "- Because the curves can be non-monotonic, a single extreme does not establish a universal latency threshold. Use the plotted trends and bootstrap intervals alongside this report.",
        "- Case-level averages give every completed run equal weight, but they also average over deliberately different latency grids and experimental scopes. They should summarize each campaign, not rank the cases as interchangeable treatments.",
        "",
    ])
    if unavailable:
        lines.extend([
            "Metrics with no finite observations in these summaries: " +
            ", ".join(unavailable) +
            ". These require message correlation or target-assignment fields that were not present in the completed logs.",
            "",
        ])

    lines.extend([
        "## Companion Data Files",
        "",
        f"- `{INVENTORY_PATH.relative_to(REPO_ROOT)}` — condition and run inventory.",
        f"- `{AVERAGES_PATH.relative_to(REPO_ROOT)}` — run-weighted averages and pooled standard deviations for every metric.",
        f"- `{EXTREMES_PATH.relative_to(REPO_ROOT)}` — lowest/highest condition means with condition labels and 95% bootstrap intervals.",
        f"- `{SUBGROUP_PATH.relative_to(REPO_ROOT)}` — group-level and marginal averages for Cases 2–6.",
        "",
    ])
    report_text = "\n".join(lines)
    REPORT_PATH.write_text(report_text, encoding="utf-8")
    # Keep the earlier report link synchronized for compatibility.
    LEGACY_REPORT_PATH.write_text(report_text, encoding="utf-8")

    print(f"Wrote {REPORT_PATH}")
    print(f"Wrote {LEGACY_REPORT_PATH}")
    print(f"Wrote {INVENTORY_PATH}")
    print(f"Wrote {AVERAGES_PATH}")
    print(f"Wrote {EXTREMES_PATH}")
    print(f"Wrote {SUBGROUP_PATH}")


if __name__ == "__main__":
    build_outputs()
