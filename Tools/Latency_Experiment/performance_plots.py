"""Plots for advanced latency-performance diagnostics."""

import math
import textwrap
from pathlib import Path

import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
from latency_metrics import (AGENT_ID, EVENT, NEW_THREAT, THREAT_DESTROYED,
                             THREAT_HIT, TIME)
from latency_plots import DISTINCT_SERIES_COLORS, set_axis_title
from matplotlib import colors
from matplotlib.lines import Line2D


def _set_figure_title(
    fig: plt.Figure,
    title: str,
    subtitle: str,
) -> None:
    """Adds a figure heading above multi-panel diagnostic plots."""
    wrapped = textwrap.fill(subtitle, width=125)
    fig.suptitle(title, y=0.995)
    fig.text(
        0.5,
        0.965,
        wrapped,
        ha="center",
        va="top",
        fontsize=8.5,
        color="0.3",
    )
    setattr(fig, "_latency_subtitle", True)


def _finish_figure(fig: plt.Figure, output_path: Path | None) -> None:
    """Displays a figure or saves it when an output path is supplied."""
    if getattr(fig, "_latency_subtitle", False):
        fig.tight_layout(rect=(0.0, 0.0, 1.0, 0.92))
    else:
        fig.tight_layout()
    if output_path is None:
        plt.show()
    else:
        output_path.parent.mkdir(parents=True, exist_ok=True)
        fig.savefig(output_path, dpi=200, bbox_inches="tight")
    plt.close(fig)


def _series_groups(
    data: pd.DataFrame,
    series_column: str | None,
) -> list[tuple[str, pd.DataFrame]]:
    if series_column is None:
        return [("All conditions", data)]
    return [
        (str(label), frame)
        for label, frame in data.groupby(series_column, dropna=False, sort=True)
    ]


def _representative_values(values: pd.Series, maximum: int = 5) -> list[float]:
    finite = np.sort(pd.to_numeric(values, errors="coerce").dropna().unique())
    if finite.size <= maximum:
        return [float(value) for value in finite]
    indices = np.unique(
        np.rint(np.linspace(0, finite.size - 1, maximum)).astype(int))
    return [float(finite[index]) for index in indices]


def _apply_x_scale(ax: plt.Axes, xvalues: np.ndarray) -> None:
    finite = xvalues[np.isfinite(xvalues)]
    positive = finite[finite > 0]
    if positive.size and finite.size and np.max(finite) / np.min(
            positive) >= 20:
        ax.set_xscale("symlog", linthresh=float(np.min(positive)) / 2)


def _subplot_grid(num_panels: int) -> tuple[plt.Figure, np.ndarray]:
    num_columns = min(3, max(1, num_panels))
    num_rows = int(math.ceil(num_panels / num_columns))
    fig, axes = plt.subplots(
        num_rows,
        num_columns,
        figsize=(6 * num_columns, 4.2 * num_rows),
        squeeze=False,
        sharex=True,
        sharey=True,
    )
    return fig, axes.ravel()


def _run_resolution_times(event_path: str) -> np.ndarray:
    events = pd.read_csv(event_path, usecols=[TIME, EVENT, AGENT_ID])
    events[EVENT] = events[EVENT].astype(str).str.upper().str.strip()
    events[TIME] = pd.to_numeric(events[TIME], errors="coerce")
    spawned = events[events[EVENT] == NEW_THREAT]
    outcomes = events[events[EVENT].isin([THREAT_HIT, THREAT_DESTROYED])]
    spawn_times = spawned.groupby(AGENT_ID)[TIME].min().dropna()
    outcome_times = outcomes.groupby(AGENT_ID)[TIME].min().dropna()
    if spawn_times.empty:
        return np.array([], dtype=float)
    first_spawn = float(spawn_times.min())
    return np.asarray([
        max(0.0,
            float(outcome_times[threat_id]) -
            first_spawn) if threat_id in outcome_times.index else np.inf
        for threat_id in spawn_times.index
    ],
                      dtype=float)


def plot_threat_clearance_curves(
    run_data: pd.DataFrame,
    x_column: str,
    series_column: str | None,
    x_label: str,
    experiment_label: str,
    max_conditions_per_panel: int = 5,
    output_path: Path | None = None,
) -> None:
    """Plots mean fraction of threats remaining over engagement time."""
    groups = _series_groups(run_data, series_column)
    fig, axes = _subplot_grid(len(groups))
    selected_frames: list[pd.DataFrame] = []
    for _, frame in groups:
        selected_values = _representative_values(frame[x_column],
                                                 max_conditions_per_panel)
        selected_frames.append(frame[frame[x_column].isin(selected_values)])
    selected_data = pd.concat(selected_frames, ignore_index=True)
    max_elapsed = pd.to_numeric(selected_data["last_threat_outcome_time_s"],
                                errors="coerce").max()
    if not np.isfinite(max_elapsed) or max_elapsed <= 0:
        plt.close(fig)
        return
    time_grid = np.linspace(0.0, float(max_elapsed), 300)

    for axis, (series_label, frame) in zip(axes, groups):
        selected_values = _representative_values(frame[x_column],
                                                 max_conditions_per_panel)
        for series_index, xvalue in enumerate(selected_values):
            condition_runs = frame[np.isclose(
                pd.to_numeric(frame[x_column], errors="coerce"), xvalue)]
            run_curves: list[np.ndarray] = []
            for event_path in condition_runs["event_log_path"]:
                resolution_times = _run_resolution_times(str(event_path))
                if resolution_times.size:
                    run_curves.append(
                        np.mean(resolution_times[:, None] > time_grid[None, :],
                                axis=0))
            if not run_curves:
                continue
            axis.plot(
                time_grid,
                np.mean(np.vstack(run_curves), axis=0),
                color=DISTINCT_SERIES_COLORS[series_index %
                                             len(DISTINCT_SERIES_COLORS)],
                marker="o",
                markevery=(series_index, max(1,
                                             len(selected_values) * 8)),
                markersize=5,
                linestyle="-",
                label=f"{xvalue:g} s",
            )
        axis.set_title(series_label if series_column else experiment_label)
        axis.set_xlabel("Elapsed Time Since First Threat Spawn [s]")
        axis.set_ylabel("Mean Fraction of Threats Remaining")
        axis.set_ylim(-0.02, 1.02)
        axis.grid(alpha=0.25)
        axis.legend(title=x_label, fontsize=8)

    for unused_axis in axes[len(groups):]:
        unused_axis.set_visible(False)
    _set_figure_title(
        fig,
        f"{experiment_label}: Threat Clearance Curves",
        ("At elapsed time t from the first threat spawn, the curve is the "
         "mean fraction of spawned threats without a THREAT_HIT or "
         "THREAT_DESTROYED outcome."),
    )
    _finish_figure(fig, output_path)


def plot_interceptor_lifecycle_fractions(
    summary: pd.DataFrame,
    x_column: str,
    series_column: str | None,
    x_label: str,
    experiment_label: str,
    output_path: Path | None = None,
) -> None:
    """Plots mutually exclusive terminal lifecycle fractions."""
    required = {
        x_column,
        "missile_interceptor_hit_fraction_mean",
        "missile_interceptor_destroyed_fraction_mean",
        "missile_interceptor_no_terminal_fraction_mean",
    }
    if not required.issubset(summary.columns):
        return
    groups = _series_groups(summary, series_column)
    fig, axes = _subplot_grid(len(groups))
    for axis, (series_label, frame) in zip(axes, groups):
        frame = frame.sort_values(x_column)
        xvalues = pd.to_numeric(frame[x_column],
                                errors="coerce").to_numpy(dtype=float)
        hit = frame["missile_interceptor_hit_fraction_mean"].to_numpy(
            dtype=float)
        destroyed = frame[
            "missile_interceptor_destroyed_fraction_mean"].to_numpy(dtype=float)
        no_terminal = frame[
            "missile_interceptor_no_terminal_fraction_mean"].to_numpy(
                dtype=float)
        finite = (np.isfinite(xvalues) & np.isfinite(hit) &
                  np.isfinite(destroyed) & np.isfinite(no_terminal))
        if not finite.any():
            continue
        xvalues = xvalues[finite]
        axis.stackplot(
            xvalues,
            hit[finite],
            destroyed[finite],
            no_terminal[finite],
            labels=[
                "Eventually Hit", "Destroyed Without Hit", "No Terminal Outcome"
            ],
            colors=["tab:green", "tab:red", "tab:gray"],
            alpha=0.72,
        )
        _apply_x_scale(axis, xvalues)
        axis.set_title(series_label if series_column else experiment_label)
        axis.set_xlabel(x_label)
        axis.set_ylabel("Fraction of Spawned Missile Interceptors")
        axis.set_ylim(0.0, 1.0)
        axis.grid(alpha=0.25)
        axis.legend(fontsize=8, loc="best")
    for unused_axis in axes[len(groups):]:
        unused_axis.set_visible(False)
    _set_figure_title(
        fig,
        f"{experiment_label}: Interceptor Lifecycle",
        ("Spawned interceptors are classified as eventually hit, destroyed "
         "without hit, or without a terminal event; the fractions sum to 1."),
    )
    _finish_figure(fig, output_path)


def _color_normalizer(values: np.ndarray) -> colors.Normalize:
    finite = values[np.isfinite(values)]
    if finite.size == 0:
        return colors.Normalize(0, 1)
    minimum = float(np.min(finite))
    maximum = float(np.max(finite))
    if minimum == maximum:
        return colors.Normalize(minimum - 0.5, maximum + 0.5)
    positive = finite[finite > 0]
    if positive.size and maximum / float(np.min(positive)) >= 100:
        return colors.SymLogNorm(
            linthresh=float(np.min(positive)) / 2,
            vmin=min(0.0, minimum),
            vmax=maximum,
        )
    return colors.Normalize(minimum, maximum)


def plot_cost_safety_pareto(
    summary: pd.DataFrame,
    x_column: str,
    series_column: str | None,
    latency_label: str,
    experiment_label: str,
    output_path: Path | None = None,
) -> None:
    """Plots interceptor cost against minimum intercept safety distance."""
    cost_column = "missile_interceptors_per_threat_destroyed_mean"
    safety_column = "minimum_intercept_distance_m_mean"
    required = {x_column, cost_column, safety_column}
    if not required.issubset(summary.columns):
        return
    latency_values = pd.to_numeric(summary[x_column],
                                   errors="coerce").to_numpy(dtype=float)
    norm = _color_normalizer(latency_values)
    cmap = "viridis"
    markers = ["o", "^", "s", "D", "P", "X", "v", "<", ">"]
    fig, axis = plt.subplots(figsize=(10, 7))
    groups = _series_groups(summary, series_column)
    legend_handles: list[Line2D] = []
    for index, (series_label, frame) in enumerate(groups):
        xvalues = pd.to_numeric(frame[cost_column],
                                errors="coerce").to_numpy(dtype=float)
        yvalues = pd.to_numeric(frame[safety_column],
                                errors="coerce").to_numpy(dtype=float)
        colors_values = pd.to_numeric(frame[x_column],
                                      errors="coerce").to_numpy(dtype=float)
        finite = (np.isfinite(xvalues) & np.isfinite(yvalues) &
                  np.isfinite(colors_values))
        marker = markers[index % len(markers)]
        axis.scatter(
            xvalues[finite],
            yvalues[finite],
            c=colors_values[finite],
            cmap=cmap,
            norm=norm,
            marker=marker,
            s=48,
            alpha=0.8,
            edgecolors="none",
        )
        if series_column is not None:
            legend_handles.append(
                Line2D([], [],
                       marker=marker,
                       linestyle="None",
                       color="gray",
                       label=series_label))
    axis.set_xlabel("Missile Interceptors Spawned per Threat Destroyed")
    axis.set_ylabel("Mean Minimum Intercept Distance [m]")
    set_axis_title(
        axis,
        f"{experiment_label}: Resource Cost Versus Safety",
        ("X = missile interceptors launched per threat destroyed; Y = mean "
         "minimum hit distance from the coordinate origin; color = latency."),
    )
    axis.grid(alpha=0.25)
    if legend_handles:
        legend_title = series_column.replace("_", " ").title()
        axis.legend(handles=legend_handles, title=legend_title, fontsize=8)
    mappable = plt.cm.ScalarMappable(norm=norm, cmap=cmap)
    mappable.set_array([])
    fig.colorbar(mappable, ax=axis, label=latency_label)
    _finish_figure(fig, output_path)
