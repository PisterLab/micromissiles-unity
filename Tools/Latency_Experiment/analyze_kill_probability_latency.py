"""Analyze the crossed UCAV kill-probability/global-latency experiment.

The experiment occupies IDs 50000--50419 and contains 50 paired seeds for
each of 20 UCAV kill probabilities and 21 global one-way latencies. The first
run extracts run-level metrics from event logs and caches them as CSV. Later
runs reuse that cache unless ``--refresh_cache`` is supplied.

Outputs include run-level and condition-level CSV files, paired changes from
the zero-latency condition, statistically detectable latency breakpoints, a
short text report, heatmaps, and response-curve figures.
"""

import argparse
import logging
import os
import tempfile
from collections.abc import Sequence
from pathlib import Path

os.environ.setdefault(
    "MPLCONFIGDIR",
    str(Path(tempfile.gettempdir()) / "micromissiles-matplotlib"),
)

import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
from latency_dataset import (FAMILY_ID_RANGES, KILL_PROBABILITY, REPO_ROOT,
                             add_common_cli_arguments, build_run_dataset,
                             configure_logging)
from latency_metrics import DEFAULT_PLOT_METRICS, METRIC_LABELS
from latency_plots import (DISTINCT_SERIES_COLORS, metric_subtitle,
                           set_axis_title)
from latency_statistics import paired_differences, summarize_conditions
from matplotlib import colors

CONDITION_COLUMNS = ["kill_probability", "mean_latency_s"]

SUMMARY_METRICS = DEFAULT_PLOT_METRICS

OVERVIEW_METRICS = [
    "threat_penetration_rate",
    "threat_destroyed_rate",
    "interceptor_terminal_success_rate",
    "interceptor_efficiency",
    "num_missile_interceptors",
    "mean_threat_exposure_time_s",
]

RESPONSE_METRICS = DEFAULT_PLOT_METRICS

BREAKPOINT_DIRECTIONS = {
    "threat_penetration_rate": "increase",
    "threat_destroyed_rate": "decrease",
    "interceptor_efficiency": "decrease",
    "num_missile_interceptors": "increase",
}

DEFAULT_OUTPUT_DIR = REPO_ROOT / "Logs/Analysis/Kill_Probability_Latency"


def _atomic_csv(data: pd.DataFrame, path: Path) -> None:
    """Writes a dataframe without leaving a partial cache on interruption."""
    temporary_path = path.with_suffix(path.suffix + ".tmp")
    data.to_csv(temporary_path, index=False)
    temporary_path.replace(path)


def _required_cache_columns() -> set[str]:
    return {
        "family",
        "experiment_id",
        "seed",
        "kill_probability",
        "mean_latency_s",
        *SUMMARY_METRICS,
    }


def load_or_build_run_data(
    log_root: Path,
    config_root: Path,
    cache_path: Path,
    refresh_cache: bool,
    strict: bool,
    workers: int,
) -> pd.DataFrame:
    """Loads a valid run cache or extracts metrics from all event logs."""
    if cache_path.is_file() and not refresh_cache:
        logging.info("Loading run-level metric cache: %s", cache_path)
        data = pd.read_csv(cache_path)
        missing = _required_cache_columns().difference(data.columns)
        if missing:
            raise ValueError(
                f"Metric cache is missing columns {sorted(missing)}. "
                "Re-run with --refresh_cache.")
        return data[data["family"] == KILL_PROBABILITY].copy()

    logging.info("Extracting metrics from the kill-probability event logs.")
    data = build_run_dataset(
        log_root,
        config_root,
        families=[KILL_PROBABILITY],
        strict=strict,
        workers=workers,
    )
    data = data.sort_values(
        ["experiment_id", "seed", "run_index"],
        kind="stable",
    ).reset_index(drop=True)
    _atomic_csv(data, cache_path)
    logging.info("Saved run-level metric cache: %s", cache_path)
    return data


def validate_design(data: pd.DataFrame) -> list[str]:
    """Returns problems found in condition, seed, or experiment-ID coverage."""
    problems: list[str] = []
    if data.empty:
        return ["No kill-probability runs were loaded."]

    expected_ids = set(FAMILY_ID_RANGES[KILL_PROBABILITY])
    experiment_ids = pd.to_numeric(data["experiment_id"],
                                   errors="coerce").dropna().astype(int)
    actual_ids = set(experiment_ids)
    missing_ids = sorted(expected_ids.difference(actual_ids))
    extra_ids = sorted(actual_ids.difference(expected_ids))
    if missing_ids:
        preview = missing_ids[:20]
        suffix = (f" ... ({len(missing_ids)} total)"
                  if len(missing_ids) > len(preview) else "")
        problems.append(f"Missing experiment IDs: {preview}{suffix}")
    if extra_ids:
        preview = extra_ids[:20]
        suffix = (f" ... ({len(extra_ids)} total)"
                  if len(extra_ids) > len(preview) else "")
        problems.append(f"Unexpected experiment IDs: {preview}{suffix}")

    duplicated = data.duplicated(["experiment_id", "seed"], keep=False)
    if duplicated.any():
        duplicate_count = int(duplicated.sum())
        problems.append(
            f"Found {duplicate_count} rows with duplicate experiment-ID/seed "
            "keys.")

    counts = data.groupby(CONDITION_COLUMNS, dropna=False).size()
    incomplete = counts[counts != 50]
    if not incomplete.empty:
        preview = ", ".join(f"kill={kill:g}, latency={latency:g}s: {count} runs"
                            for (kill,
                                 latency), count in incomplete.head(10).items())
        suffix = " ..." if len(incomplete) > 10 else ""
        problems.append(f"Conditions without 50 runs: {preview}{suffix}")

    expected_probabilities = 20
    expected_latencies = 21
    num_probabilities = data["kill_probability"].nunique(dropna=True)
    num_latencies = data["mean_latency_s"].nunique(dropna=True)
    if num_probabilities != expected_probabilities:
        problems.append(
            f"Expected {expected_probabilities} kill probabilities; found "
            f"{num_probabilities}.")
    if num_latencies != expected_latencies:
        problems.append(f"Expected {expected_latencies} latency values; found "
                        f"{num_latencies}.")
    return problems


def summarize_experiment(
    data: pd.DataFrame,
    bootstrap_samples: int,
) -> tuple[pd.DataFrame, pd.DataFrame, pd.DataFrame, pd.DataFrame]:
    """Computes crossed-condition, marginal, and paired-delta summaries."""
    condition_summary = summarize_conditions(
        data,
        CONDITION_COLUMNS,
        SUMMARY_METRICS,
        bootstrap_samples=bootstrap_samples,
    )
    latency_summary = summarize_conditions(
        data,
        ["mean_latency_s"],
        SUMMARY_METRICS,
        bootstrap_samples=bootstrap_samples,
    )
    kill_probability_summary = summarize_conditions(
        data,
        ["kill_probability"],
        SUMMARY_METRICS,
        bootstrap_samples=bootstrap_samples,
    )

    baseline = data[np.isclose(data["mean_latency_s"], 0.0)].copy()
    paired = paired_differences(
        data,
        baseline,
        on=["kill_probability", "seed"],
        metrics=SUMMARY_METRICS,
    )
    delta_metrics = [f"{metric}_delta" for metric in SUMMARY_METRICS]
    delta_summary = summarize_conditions(
        paired,
        CONDITION_COLUMNS,
        delta_metrics,
        bootstrap_samples=bootstrap_samples,
    )
    return (condition_summary, latency_summary, kill_probability_summary,
            delta_summary)


def _first_detectable_change(
    frame: pd.DataFrame,
    metric: str,
    direction: str,
) -> float:
    """Returns the first positive latency whose paired CI excludes zero."""
    low_column = f"{metric}_delta_ci_low"
    high_column = f"{metric}_delta_ci_high"
    ordered = frame[frame["mean_latency_s"] > 0].sort_values("mean_latency_s")
    if direction == "increase":
        detected = ordered[ordered[low_column] > 0]
    elif direction == "decrease":
        detected = ordered[ordered[high_column] < 0]
    else:
        raise ValueError(f"Unknown breakpoint direction: {direction}")
    if detected.empty:
        return np.nan
    return float(detected.iloc[0]["mean_latency_s"])


def latency_breakpoints(delta_summary: pd.DataFrame) -> pd.DataFrame:
    """Finds the first latency with a directional paired 95% CI."""
    rows: list[dict[str, float]] = []
    for kill_probability, frame in delta_summary.groupby("kill_probability",
                                                         sort=True):
        row = {"kill_probability": float(kill_probability)}
        for metric, direction in BREAKPOINT_DIRECTIONS.items():
            row[f"first_detectable_{metric}_{direction}_latency_s"] = (
                _first_detectable_change(frame, metric, direction))
        rows.append(row)
    return pd.DataFrame(rows)


def _format_value(value: object) -> str:
    numeric = pd.to_numeric(pd.Series([value]), errors="coerce").iloc[0]
    if np.isfinite(numeric):
        return f"{float(numeric):g}"
    return "N/A"


def _heatmap(
    axis: plt.Axes,
    summary: pd.DataFrame,
    value_column: str,
    title: str,
    colorbar_label: str,
    cmap: str,
    center_zero: bool = False,
    subtitle: str | None = None,
) -> None:
    pivot = summary.pivot(
        index="kill_probability",
        columns="mean_latency_s",
        values=value_column,
    ).sort_index(axis=0).sort_index(axis=1)
    values = pivot.to_numpy(dtype=float)
    masked = np.ma.masked_invalid(values)
    normalizer: colors.Normalize | None = None
    if center_zero:
        finite = np.abs(values[np.isfinite(values)])
        limit = float(np.max(finite)) if finite.size else 1.0
        limit = limit if limit > 0 else 1.0
        normalizer = colors.TwoSlopeNorm(vmin=-limit, vcenter=0.0, vmax=limit)
    image = axis.imshow(
        masked,
        aspect="auto",
        origin="lower",
        cmap=cmap,
        norm=normalizer,
    )
    axis.set_xticks(np.arange(len(pivot.columns)))
    axis.set_xticklabels(
        [_format_value(value) for value in pivot.columns],
        rotation=45,
        ha="right",
        fontsize=7,
    )
    axis.set_yticks(np.arange(len(pivot.index)))
    axis.set_yticklabels([_format_value(value) for value in pivot.index],
                         fontsize=7)
    axis.set_xlabel("Global Communication Latency [s]")
    axis.set_ylabel("UCAV Kill Probability")
    set_axis_title(axis, title, subtitle)
    axis.figure.colorbar(
        image,
        ax=axis,
        fraction=0.046,
        pad=0.04,
        label=colorbar_label,
    )


def _finish_figure(figure: plt.Figure, output_path: Path | None) -> None:
    """Displays a figure interactively or saves it when a path is supplied."""
    if output_path is None:
        plt.show()
    else:
        output_path.parent.mkdir(parents=True, exist_ok=True)
        figure.savefig(output_path, dpi=200, bbox_inches="tight")
    plt.close(figure)


def plot_overview_heatmaps(
    summary: pd.DataFrame,
    output_dir: Path | None = None,
) -> None:
    """Creates one condition-mean heatmap per main outcome metric."""
    for metric in OVERVIEW_METRICS:
        label = METRIC_LABELS.get(metric, metric.replace("_", " ").title())
        figure, axis = plt.subplots(figsize=(12, 7))
        _heatmap(
            axis,
            summary,
            f"{metric}_mean",
            f"UCAV Kill Probability × Global Communication Latency: {label}",
            label,
            "viridis_r" if metric == "threat_penetration_rate" else "viridis",
            subtitle=metric_subtitle(
                metric, "Cells show condition means across 50 runs."),
        )
        figure.tight_layout()
        output_path = (output_dir / f"condition_heatmap_{metric}.png"
                       if output_dir is not None else None)
        _finish_figure(figure, output_path)


def plot_delta_heatmaps(
    summary: pd.DataFrame,
    output_dir: Path | None = None,
) -> None:
    """Creates one paired zero-latency delta heatmap per outcome metric."""
    for metric in RESPONSE_METRICS:
        label = METRIC_LABELS.get(metric, metric.replace("_", " ").title())
        delta_label = f"Change in {label}"
        figure, axis = plt.subplots(figsize=(12, 7))
        _heatmap(
            axis,
            summary,
            f"{metric}_delta_mean",
            ("Latency Effect Relative to the Same Kill Probability and Seed "
             f"at 0 s: {label}"),
            delta_label,
            "coolwarm",
            center_zero=True,
            subtitle=metric_subtitle(
                f"{metric}_delta",
                "Cells show mean seed-paired changes across 50 runs.",
            ),
        )
        figure.tight_layout()
        output_path = (output_dir / f"zero_latency_delta_{metric}.png"
                       if output_dir is not None else None)
        _finish_figure(figure, output_path)


def _representative_values(values: pd.Series, maximum: int = 5) -> list[float]:
    finite = np.sort(pd.to_numeric(values, errors="coerce").dropna().unique())
    if finite.size <= maximum:
        return [float(value) for value in finite]
    indices = np.unique(
        np.rint(np.linspace(0, finite.size - 1, maximum)).astype(int))
    return [float(finite[index]) for index in indices]


def _plot_response_axis(
    axis: plt.Axes,
    summary: pd.DataFrame,
    x_column: str,
    series_column: str,
    selected_series: Sequence[float],
    metric: str,
) -> None:
    all_xvalues: list[np.ndarray] = []
    for series_index, series_value in enumerate(selected_series):
        frame = summary[np.isclose(summary[series_column], series_value)]
        frame = frame.sort_values(x_column)
        xvalues = frame[x_column].to_numpy(dtype=float)
        means = frame[f"{metric}_mean"].to_numpy(dtype=float)
        lows = frame[f"{metric}_ci_low"].to_numpy(dtype=float)
        highs = frame[f"{metric}_ci_high"].to_numpy(dtype=float)
        finite = (np.isfinite(xvalues) & np.isfinite(means) &
                  np.isfinite(lows) & np.isfinite(highs))
        if not finite.any():
            continue
        xvalues = xvalues[finite]
        means = means[finite]
        color = DISTINCT_SERIES_COLORS[series_index %
                                       len(DISTINCT_SERIES_COLORS)]
        axis.plot(
            xvalues,
            means,
            marker="o",
            markersize=6,
            markeredgecolor="white",
            markeredgewidth=0.5,
            linestyle="-",
            linewidth=2,
            color=color,
            label=f"{series_value:g}",
        )
        axis.fill_between(
            xvalues,
            lows[finite],
            highs[finite],
            color=color,
            alpha=0.08,
        )
        all_xvalues.append(xvalues)
    if all_xvalues and x_column == "mean_latency_s":
        combined = np.concatenate(all_xvalues)
        positive = combined[combined > 0]
        if positive.size:
            axis.set_xscale("symlog", linthresh=float(np.min(positive)) / 2)
    axis.grid(alpha=0.25)


def plot_latency_responses(
    summary: pd.DataFrame,
    output_dir: Path | None = None,
) -> None:
    """Creates one latency-response chart per outcome metric."""
    selected = _representative_values(summary["kill_probability"])
    for metric in RESPONSE_METRICS:
        label = METRIC_LABELS.get(metric, metric.replace("_", " ").title())
        figure, axis = plt.subplots(figsize=(10, 6))
        _plot_response_axis(
            axis,
            summary,
            "mean_latency_s",
            "kill_probability",
            selected,
            metric,
        )
        axis.set_xlabel("Global Communication Latency [s]")
        axis.set_ylabel(label)
        set_axis_title(
            axis,
            f"Latency Response at Representative Kill Probabilities: {label}",
            metric_subtitle(metric),
        )
        axis.legend(title="UCAV Kill Probability", fontsize=8, ncol=2)
        figure.tight_layout()
        output_path = (output_dir / f"latency_response_{metric}.png"
                       if output_dir is not None else None)
        _finish_figure(figure, output_path)


def plot_kill_probability_responses(
    summary: pd.DataFrame,
    output_dir: Path | None = None,
) -> None:
    """Creates one kill-probability response chart per outcome metric."""
    selected = _representative_values(summary["mean_latency_s"])
    for metric in RESPONSE_METRICS:
        label = METRIC_LABELS.get(metric, metric.replace("_", " ").title())
        figure, axis = plt.subplots(figsize=(10, 6))
        _plot_response_axis(
            axis,
            summary,
            "kill_probability",
            "mean_latency_s",
            selected,
            metric,
        )
        axis.set_xlabel("UCAV Kill Probability")
        axis.set_ylabel(label)
        set_axis_title(
            axis,
            f"Kill Probability Response at Representative Latencies: {label}",
            metric_subtitle(metric),
        )
        axis.legend(title="Global Communication Latency [s]",
                    fontsize=8,
                    ncol=2)
        figure.tight_layout()
        output_path = (output_dir / f"kill_probability_response_{metric}.png"
                       if output_dir is not None else None)
        _finish_figure(figure, output_path)


def _extreme_condition(
    summary: pd.DataFrame,
    metric: str,
    maximize: bool,
) -> str:
    column = f"{metric}_mean"
    finite = summary[np.isfinite(pd.to_numeric(summary[column],
                                               errors="coerce"))]
    if finite.empty:
        return "N/A"
    index = finite[column].idxmax() if maximize else finite[column].idxmin()
    row = finite.loc[index]
    return (f"kill={row['kill_probability']:g}, "
            f"latency={row['mean_latency_s']:g} s, mean={row[column]:.6g}")


def write_text_report(
    data: pd.DataFrame,
    summary: pd.DataFrame,
    breakpoints: pd.DataFrame,
    output_path: Path,
) -> None:
    """Writes a compact, reproducible headline summary."""
    lines = [
        "Kill-probability and global-latency experiment",
        "",
        f"Runs analyzed: {len(data)}",
        f"Experiment conditions: {len(summary)}",
        f"Seeds: {data['seed'].nunique()}",
        f"Kill probabilities: {data['kill_probability'].nunique()}",
        f"Latency values: {data['mean_latency_s'].nunique()}",
        "",
        "Condition extremes (descriptive sample means):",
        "  Lowest threat penetration: " +
        _extreme_condition(summary, "threat_penetration_rate", maximize=False),
        "  Highest threat penetration: " +
        _extreme_condition(summary, "threat_penetration_rate", maximize=True),
        "  Highest threat destroyed rate: " +
        _extreme_condition(summary, "threat_destroyed_rate", maximize=True),
        "  Highest interceptor efficiency: " +
        _extreme_condition(summary, "interceptor_efficiency", maximize=True),
        "",
        "Breakpoint definition:",
        "  First positive latency where the paired bootstrap 95% confidence",
        "  interval excludes zero in the pre-specified adverse direction.",
        "  These exploratory intervals are not adjusted for multiple tests.",
        f"  Breakpoint rows written: {len(breakpoints)}",
        "",
        "Interpretation note:",
        "  kill_probability is the configured UCAV per-collision parameter;",
        "  threat_penetration_rate and threat_destroyed_rate are observed",
        "  engagement outcomes calculated from the event logs.",
    ]
    output_path.write_text("\n".join(lines) + "\n")


def analyze(
    data: pd.DataFrame,
    output_dir: Path,
    bootstrap_samples: int = 2000,
    make_plots: bool = True,
    save_plots: bool = True,
) -> dict[str, pd.DataFrame]:
    """Runs the analysis and saves tables and individual figures."""
    output_dir.mkdir(parents=True, exist_ok=True)
    (condition_summary, latency_summary, kill_probability_summary,
     delta_summary) = summarize_experiment(data, bootstrap_samples)
    breakpoints = latency_breakpoints(delta_summary)

    outputs = {
        "condition_summary.csv": condition_summary,
        "latency_marginal_summary.csv": latency_summary,
        "kill_probability_marginal_summary.csv": kill_probability_summary,
        "zero_latency_paired_delta_summary.csv": delta_summary,
        "latency_breakpoints.csv": breakpoints,
    }
    for filename, frame in outputs.items():
        _atomic_csv(frame, output_dir / filename)

    write_text_report(
        data,
        condition_summary,
        breakpoints,
        output_dir / "analysis_report.txt",
    )

    if make_plots:
        plot_output_dir = output_dir if save_plots else None
        plot_overview_heatmaps(condition_summary, plot_output_dir)
        plot_delta_heatmaps(delta_summary, plot_output_dir)
        plot_latency_responses(condition_summary, plot_output_dir)
        plot_kill_probability_responses(condition_summary, plot_output_dir)
    return outputs


def main() -> None:
    parser = argparse.ArgumentParser(
        description=__doc__,
        formatter_class=argparse.ArgumentDefaultsHelpFormatter,
    )
    add_common_cli_arguments(parser)
    parser.add_argument(
        "--output_dir",
        type=Path,
        default=DEFAULT_OUTPUT_DIR,
        help="Directory for the metric cache, summaries, report, and plots.",
    )
    parser.add_argument(
        "--refresh_cache",
        action="store_true",
        help="Re-read every event log even if run_metrics.csv exists.",
    )
    parser.add_argument(
        "--workers",
        type=int,
        default=4,
        help="Worker threads used while extracting event-log metrics.",
    )
    parser.add_argument(
        "--allow_incomplete",
        action="store_true",
        help="Analyze available runs even if the 420 x 50 design is incomplete.",
    )
    plot_group = parser.add_mutually_exclusive_group()
    plot_group.add_argument(
        "--skip_plots",
        action="store_true",
        help="Write tabular outputs without opening or saving figures.",
    )
    plot_group.add_argument(
        "--show_plots",
        action="store_true",
        help="Open interactive plot windows instead of saving PNG files.",
    )
    args = parser.parse_args()
    configure_logging()

    output_dir = args.output_dir.expanduser().resolve()
    output_dir.mkdir(parents=True, exist_ok=True)
    data = load_or_build_run_data(
        args.log_root,
        args.config_root,
        output_dir / "run_metrics.csv",
        args.refresh_cache,
        strict=not args.allow_incomplete,
        workers=args.workers,
    )
    problems = validate_design(data)
    if problems:
        message = "\n".join(f"  - {problem}" for problem in problems)
        if not args.allow_incomplete:
            raise ValueError(
                "Experiment design validation failed:\n" + message +
                "\nUse --allow_incomplete to analyze the available runs.")
        logging.warning("Experiment design is incomplete:\n%s", message)
    else:
        logging.info(
            "Validated 420 conditions, 50 seeds per condition, and %d runs.",
            len(data),
        )

    analyze(
        data,
        output_dir,
        bootstrap_samples=args.bootstrap_samples,
        make_plots=not args.skip_plots,
        save_plots=not args.show_plots,
    )
    logging.info("Analysis complete. Results: %s", output_dir)


if __name__ == "__main__":
    main()
