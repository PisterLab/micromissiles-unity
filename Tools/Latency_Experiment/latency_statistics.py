"""Statistical summaries for run-level latency experiment results."""

from collections.abc import Sequence

import numpy as np
import pandas as pd

_TERMINAL_METRIC_NAMES = {
    "num_missile_interceptors": "spawned",
    "num_missile_interceptor_hits": "hits",
    "num_missile_interceptor_misses": "misses",
    "num_missile_interceptors_destroyed": "destroyed",
    "interceptor_hit_rate": "hit_rate",
    "interceptor_efficiency": "efficiency",
    "minimum_intercept_distance_m": "min_distance_m",
    "mean_threat_spawn_to_destroyed_time_s": "mean_threat_destroy_s",
    "time_to_destroy_all_threats_s": "all_threats_destroy_s",
    "time_to_50_percent_threats_resolved_s": "threat_t50_s",
    "time_to_90_percent_threats_resolved_s": "threat_t90_s",
    "time_to_100_percent_threats_resolved_s": "threat_t100_s",
    "mean_threat_exposure_time_s": "mean_threat_exposure_s",
    "mean_missile_interceptor_flight_time_s": "mean_flight_s",
    "p95_missile_interceptor_flight_time_s": "p95_flight_s",
    "mean_missile_interceptor_hit_displacement_m": "mean_displacement_m",
    "mean_missile_interceptor_hit_speed_proxy_mps": "mean_speed_proxy_mps",
    "time_to_first_missile_interceptor_launch_s": "first_launch_s",
    "time_to_first_missile_interceptor_hit_s": "first_hit_s",
    "peak_active_missile_interceptors": "peak_active",
    "num_missile_interceptors_with_miss": "interceptors_with_miss",
    "num_missile_interceptors_missed_then_hit": "missed_then_hit",
    "missile_interceptor_miss_recovery_rate": "miss_recovery_rate",
    "num_missile_interceptors_no_terminal_outcome": "no_terminal_outcome",
    "missile_interceptors_per_threat_destroyed": "interceptors_per_destroyed",
}

_METRIC_UNAVAILABLE_REASONS = {
    "num_coordination_requests":
        "message send events and request correlation IDs were not logged",
    "num_coordination_responses":
        "message delivery events and request correlation IDs were not logged",
    "coordination_response_rate":
        "message send/delivery events and request correlation IDs were not logged",
    "mean_coordination_response_time_s":
        "message send/delivery events and request correlation IDs were not logged",
    "median_coordination_response_time_s":
        "message send/delivery events and request correlation IDs were not logged",
    "p95_coordination_response_time_s":
        "message send/delivery events and request correlation IDs were not logged",
    "num_reassigned_missile_interceptors":
        "target-assignment events and target IDs were not logged",
    "num_reassigned_missile_interceptor_hits":
        "target-assignment events and target IDs were not logged",
    "num_reassigned_missile_interceptor_misses":
        "target-assignment events and target IDs were not logged",
    "num_reassigned_missile_interceptors_destroyed":
        "target-assignment events and target IDs were not logged",
    "reassigned_missile_interceptor_efficiency":
        "target-assignment events and target IDs were not logged",
    "num_missile_interceptor_hits_on_non_original_targets":
        "original and hit target IDs were not logged",
}


def bootstrap_mean_interval(
    values: np.ndarray,
    num_samples: int = 2000,
    confidence: float = 0.95,
    rng: np.random.Generator | None = None,
) -> tuple[float, float]:
    """Returns a percentile-bootstrap confidence interval for a mean."""
    finite_values = np.asarray(values, dtype=float)
    finite_values = finite_values[np.isfinite(finite_values)]
    if finite_values.size == 0:
        return np.nan, np.nan
    if finite_values.size == 1 or num_samples <= 0:
        value = float(finite_values[0])
        return value, value
    if rng is None:
        rng = np.random.default_rng(0)
    samples = rng.choice(
        finite_values,
        size=(num_samples, finite_values.size),
        replace=True,
    )
    sample_means = samples.mean(axis=1)
    tail_probability = (1.0 - confidence) / 2.0
    low, high = np.quantile(sample_means,
                            [tail_probability, 1.0 - tail_probability])
    return float(low), float(high)


def summarize_conditions(
    data: pd.DataFrame,
    group_columns: Sequence[str],
    metrics: Sequence[str],
    bootstrap_samples: int = 2000,
    random_seed: int = 0,
) -> pd.DataFrame:
    """Summarizes run metrics for every experimental condition."""
    if data.empty:
        return pd.DataFrame()
    missing_columns = set(group_columns).union(metrics).difference(data.columns)
    if missing_columns:
        raise ValueError(f"Missing summary columns: {sorted(missing_columns)}")

    rng = np.random.default_rng(random_seed)
    rows: list[dict[str, object]] = []
    grouper: str | list[str] = (group_columns[0] if len(group_columns) == 1 else
                                list(group_columns))
    for group_key, group in data.groupby(grouper, dropna=False, sort=True):
        group_values = (group_key if isinstance(group_key, tuple) else
                        (group_key,))
        row: dict[str, object] = dict(zip(group_columns, group_values))
        row["num_runs"] = len(group)
        for metric in metrics:
            values = pd.to_numeric(group[metric],
                                   errors="coerce").to_numpy(dtype=float)
            finite_values = values[np.isfinite(values)]
            row[f"{metric}_n"] = finite_values.size
            if finite_values.size == 0:
                row.update({
                    f"{metric}_mean": np.nan,
                    f"{metric}_median": np.nan,
                    f"{metric}_std": np.nan,
                    f"{metric}_ci_low": np.nan,
                    f"{metric}_ci_high": np.nan,
                })
                continue
            ci_low, ci_high = bootstrap_mean_interval(
                finite_values,
                num_samples=bootstrap_samples,
                rng=rng,
            )
            row.update({
                f"{metric}_mean": float(np.mean(finite_values)),
                f"{metric}_median": float(np.median(finite_values)),
                f"{metric}_std": (float(np.std(finite_values, ddof=1))
                                  if finite_values.size > 1 else np.nan),
                f"{metric}_ci_low": ci_low,
                f"{metric}_ci_high": ci_high,
            })
        rows.append(row)
    return pd.DataFrame(rows)


def print_mean_std_summary(
    summary: pd.DataFrame,
    condition_columns: Sequence[str],
    metrics: Sequence[str],
    title: str,
) -> None:
    """Prints condition means and sample standard deviations to the terminal."""
    columns = list(condition_columns) + ["num_runs"]
    renamed_columns: dict[str, str] = {}
    for metric in metrics:
        short_name = _TERMINAL_METRIC_NAMES.get(metric, metric)
        mean_column = f"{metric}_mean"
        std_column = f"{metric}_std"
        columns.extend([mean_column, std_column])
        renamed_columns[mean_column] = f"{short_name}_mean"
        renamed_columns[std_column] = f"{short_name}_std"

    missing_columns = set(columns).difference(summary.columns)
    if missing_columns:
        raise ValueError(
            f"Missing terminal summary columns: {sorted(missing_columns)}")
    display = summary[columns].rename(columns=renamed_columns)
    print(f"\n{title}")
    print(
        display.to_string(index=False,
                          float_format=lambda value: f"{value:.6g}"))


def available_metrics(
    data: pd.DataFrame,
    metrics: Sequence[str],
    analysis_name: str,
) -> list[str]:
    """Returns metrics with data and explains unavailable metrics."""
    available: list[str] = []
    unavailable: list[str] = []
    for metric in metrics:
        if metric not in data.columns:
            unavailable.append(metric)
            continue
        values = pd.to_numeric(data[metric],
                               errors="coerce").to_numpy(dtype=float)
        if np.isfinite(values).any():
            available.append(metric)
        else:
            unavailable.append(metric)

    if unavailable:
        print(f"\n{analysis_name}: unavailable metrics")
        for metric in unavailable:
            reason = _METRIC_UNAVAILABLE_REASONS.get(
                metric,
                "no finite values are available in these completed runs")
            print(f"  {metric}: N/A ({reason}).")
    return available


def paired_differences(
    conditions: pd.DataFrame,
    baseline: pd.DataFrame,
    on: Sequence[str],
    metrics: Sequence[str],
    delta_suffix: str = "_delta",
) -> pd.DataFrame:
    """Adds condition-minus-baseline deltas after pairing rows by seed."""
    required_columns = set(on).union(metrics)
    for name, frame in (("conditions", conditions), ("baseline", baseline)):
        missing_columns = required_columns.difference(frame.columns)
        if missing_columns:
            raise ValueError(
                f"{name} is missing columns: {sorted(missing_columns)}")

    baseline_columns = list(on) + list(metrics)
    baseline_values = baseline[baseline_columns].copy()
    if baseline_values.duplicated(list(on)).any():
        raise ValueError(f"Baseline has duplicate pairing keys: {list(on)}")
    baseline_values = baseline_values.rename(
        columns={metric: f"{metric}_baseline" for metric in metrics})
    paired = conditions.merge(
        baseline_values,
        how="inner",
        on=list(on),
        validate="many_to_one",
    )
    for metric in metrics:
        paired[f"{metric}{delta_suffix}"] = (paired[metric] -
                                             paired[f"{metric}_baseline"])
    return paired
