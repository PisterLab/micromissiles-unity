"""Reusable plots for latency experiment summaries."""

import logging
import os
import tempfile
import textwrap
from pathlib import Path

os.environ.setdefault(
    "MPLCONFIGDIR",
    str(Path(tempfile.gettempdir()) / "micromissiles-matplotlib"),
)

import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
from latency_metrics import (AGENT_TYPE, EVENT, INTERCEPTOR_HIT,
                             METRIC_DESCRIPTIONS, METRIC_LABELS,
                             MISSILE_INTERCEPTOR, NEW_INTERCEPTOR)
from matplotlib import colors
from matplotlib.lines import Line2D

_POSITION_X = "PositionX"
_POSITION_Z = "PositionZ"

# High-contrast, color-vision-friendly series colors. All response curves use
# the same round marker and solid line so series differ only by color.
DISTINCT_SERIES_COLORS = (
    "#0072B2",  # blue
    "#D55E00",  # vermillion
    "#009E73",  # green
    "#CC79A7",  # reddish purple
    "#E69F00",  # orange
    "#56B4E9",  # sky blue
    "#000000",  # black
    "#F0E442",  # yellow
)


def metric_subtitle(
    metric: str,
    summary_note: str = "",
) -> str:
    """Returns a concise metric definition plus a plot-specific summary note."""
    is_delta = metric.endswith("_delta")
    base_metric = metric[:-len("_delta")] if is_delta else metric
    definition = METRIC_DESCRIPTIONS.get(
        base_metric,
        base_metric.replace("_", " ").capitalize(),
    )
    if is_delta:
        definition = f"Seed-matched condition minus baseline. {definition}"
    return f"{definition}\n{summary_note}" if summary_note else definition


def set_axis_title(
    ax: plt.Axes,
    title: str,
    subtitle: str | None = None,
    title_wrap_width: int = 78,
    wrap_width: int = 105,
) -> None:
    """Adds a title and a smaller, wrapped explanatory subtitle."""
    wrapped_title = textwrap.fill(title, width=title_wrap_width)
    if not subtitle:
        ax.set_title(wrapped_title)
        return
    wrapped_subtitle = "\n".join(
        textwrap.fill(line, width=wrap_width) for line in subtitle.splitlines())
    subtitle_lines = len(wrapped_subtitle.splitlines())
    ax.set_title(wrapped_title, pad=22 + 10 * subtitle_lines)
    ax.text(
        0.5,
        1.015,
        wrapped_subtitle,
        transform=ax.transAxes,
        ha="center",
        va="bottom",
        fontsize=8.5,
        color="0.3",
    )


def _finish_figure(fig: plt.Figure, output_path: Path | None) -> None:
    """Displays a figure or saves it to a requested PNG path."""
    if output_path is None:
        plt.show()
    else:
        output_path.parent.mkdir(parents=True, exist_ok=True)
        fig.savefig(output_path, dpi=200, bbox_inches="tight")
    plt.close(fig)


def _apply_latency_scale(ax: plt.Axes, xvalues: np.ndarray) -> None:
    positive_values = xvalues[np.isfinite(xvalues) & (xvalues > 0)]
    if positive_values.size:
        ax.set_xscale("symlog", linthresh=float(np.min(positive_values)) / 2)


def plot_metric_curves(
    summary: pd.DataFrame,
    x_column: str,
    metric: str,
    series_column: str | None = None,
    series_colors: dict[object, str] | None = None,
    x_label: str = "One-Way Latency [s]",
    y_label: str | None = None,
    title: str | None = None,
    subtitle: str | None = None,
    reference_y: float | None = None,
    legend_title: str | None = None,
    output_path: Path | None = None,
) -> None:
    """Plots condition means and bootstrap confidence ribbons."""
    mean_column = f"{metric}_mean"
    low_column = f"{metric}_ci_low"
    high_column = f"{metric}_ci_high"
    required_columns = {x_column, mean_column, low_column, high_column}
    if not required_columns.issubset(summary.columns) or summary.empty:
        return

    fig, ax = plt.subplots(figsize=(10, 6))
    if series_column is None:
        series = [(None, summary)]
    else:
        series = list(summary.groupby(series_column, dropna=False, sort=True))
    all_xvalues: list[np.ndarray] = []
    for series_index, (label, frame) in enumerate(series):
        frame = frame.sort_values(x_column)
        xvalues = pd.to_numeric(frame[x_column],
                                errors="coerce").to_numpy(dtype=float)
        means = pd.to_numeric(frame[mean_column],
                              errors="coerce").to_numpy(dtype=float)
        lows = pd.to_numeric(frame[low_column],
                             errors="coerce").to_numpy(dtype=float)
        highs = pd.to_numeric(frame[high_column],
                              errors="coerce").to_numpy(dtype=float)
        finite = np.isfinite(xvalues) & np.isfinite(means)
        if not finite.any():
            continue
        xvalues = xvalues[finite]
        means = means[finite]
        lows = lows[finite]
        highs = highs[finite]
        all_xvalues.append(xvalues)
        line_label = str(label) if label is not None else None
        color = (series_colors.get(label) if series_colors is not None else
                 DISTINCT_SERIES_COLORS[series_index %
                                        len(DISTINCT_SERIES_COLORS)])
        line, = ax.plot(
            xvalues,
            means,
            marker="o",
            markersize=5,
            linestyle="-",
            linewidth=1.8,
            label=line_label,
            color=color,
        )
        ax.fill_between(xvalues,
                        lows,
                        highs,
                        alpha=0.18,
                        color=line.get_color())

    if all_xvalues:
        _apply_latency_scale(ax, np.concatenate(all_xvalues))
    if reference_y is not None:
        ax.axhline(reference_y, color="gray", linewidth=1, alpha=0.8)
    ax.set_xlabel(x_label)
    ax.set_ylabel(y_label or
                  METRIC_LABELS.get(metric,
                                    metric.replace("_", " ").title()))
    set_axis_title(
        ax,
        title or METRIC_LABELS.get(metric, metric),
        subtitle if subtitle is not None else metric_subtitle(metric),
    )
    ax.grid(alpha=0.3)
    if series_column is not None:
        ax.legend(
            title=(legend_title or series_column.replace("_", " ").title()),
            fontsize=8,
        )
    fig.tight_layout()
    _finish_figure(fig, output_path)


def plot_condition_heatmap(
    summary: pd.DataFrame,
    x_column: str,
    y_column: str,
    value_column: str,
    x_label: str,
    y_label: str,
    title: str,
    subtitle: str | None = None,
    output_path: Path | None = None,
) -> None:
    """Plots a condition mean as a categorical two-dimensional heatmap."""
    required_columns = {x_column, y_column, value_column}
    if not required_columns.issubset(summary.columns) or summary.empty:
        return
    pivot = summary.pivot(index=y_column, columns=x_column, values=value_column)
    if pivot.empty:
        return
    pivot = pivot.sort_index(axis=0).sort_index(axis=1)
    values = pivot.to_numpy(dtype=float)

    width = max(10, 0.45 * len(pivot.columns))
    height = max(4, 0.55 * len(pivot.index))
    fig, ax = plt.subplots(figsize=(width, height))
    image = ax.imshow(values, aspect="auto", origin="lower", cmap="viridis")
    ax.set_xticks(np.arange(len(pivot.columns)))
    ax.set_xticklabels([_format_tick(value) for value in pivot.columns],
                       rotation=45,
                       ha="right")
    ax.set_yticks(np.arange(len(pivot.index)))
    ax.set_yticklabels([_format_tick(value) for value in pivot.index])
    ax.set_xlabel(x_label)
    ax.set_ylabel(y_label)
    metric = value_column
    for suffix in ("_delta_mean", "_mean"):
        if metric.endswith(suffix):
            metric = metric[:-len(suffix)] + ("_delta" if suffix
                                              == "_delta_mean" else "")
            break
    set_axis_title(
        ax,
        title,
        subtitle if subtitle is not None else metric_subtitle(
            metric, "Cells show condition means across runs."),
    )
    fig.colorbar(image, ax=ax, label=value_column.replace("_", " ").title())
    fig.tight_layout()
    _finish_figure(fig, output_path)


def _format_tick(value: object) -> str:
    if isinstance(value, (float, np.floating)):
        return f"{value:g}"
    return str(value)


def _collect_latency_spatial_points(
    run_data: pd.DataFrame,
    latency_column: str,
) -> tuple[np.ndarray, np.ndarray]:
    """Loads intercept and carrier-release points with per-run latency.

    A carrier release is represented in the existing logs by a
    NEW_INTERCEPTOR event for a MissileInterceptor. Its position is the point
    where the carrier created/released that missile interceptor.
    """
    required_run_columns = {"event_log_path", latency_column}
    missing = required_run_columns.difference(run_data.columns)
    if missing:
        raise ValueError(f"Missing spatial-plot columns: {sorted(missing)}")

    intercept_arrays: list[np.ndarray] = []
    launch_arrays: list[np.ndarray] = []
    use_columns = [EVENT, AGENT_TYPE, _POSITION_X, _POSITION_Z]
    for event_path, latency_value in run_data[[
            "event_log_path", latency_column
    ]].itertuples(index=False, name=None):
        latency = pd.to_numeric(pd.Series([latency_value]),
                                errors="coerce").iloc[0]
        if not np.isfinite(latency):
            continue
        events = pd.read_csv(event_path, usecols=use_columns)
        events[EVENT] = events[EVENT].astype(str).str.upper().str.strip()
        positions = events[[_POSITION_X, _POSITION_Z]].apply(pd.to_numeric,
                                                             errors="coerce")

        intercept_mask = ((events[EVENT] == INTERCEPTOR_HIT) &
                          (events[AGENT_TYPE] == MISSILE_INTERCEPTOR))
        intercept_positions = positions.loc[intercept_mask].dropna().to_numpy(
            dtype=float)
        if intercept_positions.size:
            intercept_arrays.append(
                np.column_stack((
                    intercept_positions,
                    np.full(intercept_positions.shape[0], float(latency)),
                )))

        launch_mask = ((events[EVENT] == NEW_INTERCEPTOR) &
                       (events[AGENT_TYPE] == MISSILE_INTERCEPTOR))
        launch_positions = positions.loc[launch_mask].dropna().to_numpy(
            dtype=float)
        if launch_positions.size:
            launch_arrays.append(
                np.column_stack((
                    launch_positions,
                    np.full(launch_positions.shape[0], float(latency)),
                )))

    empty = np.empty((0, 3), dtype=float)
    intercepts = (np.concatenate(intercept_arrays)
                  if intercept_arrays else empty.copy())
    launches = (np.concatenate(launch_arrays)
                if launch_arrays else empty.copy())
    return intercepts, launches


def _collect_category_spatial_points(
    run_data: pd.DataFrame,
    category_column: str,
) -> tuple[dict[object, np.ndarray], dict[object, np.ndarray]]:
    """Loads hit and launch positions grouped by a run-level category."""
    required_run_columns = {"event_log_path", category_column}
    missing = required_run_columns.difference(run_data.columns)
    if missing:
        raise ValueError(f"Missing spatial-plot columns: {sorted(missing)}")

    intercept_arrays: dict[object, list[np.ndarray]] = {}
    launch_arrays: dict[object, list[np.ndarray]] = {}
    use_columns = [EVENT, AGENT_TYPE, _POSITION_X, _POSITION_Z]
    for event_path, category in run_data[["event_log_path", category_column
                                         ]].itertuples(index=False, name=None):
        if pd.isna(category):
            continue
        events = pd.read_csv(event_path, usecols=use_columns)
        events[EVENT] = events[EVENT].astype(str).str.upper().str.strip()
        positions = events[[_POSITION_X, _POSITION_Z]].apply(pd.to_numeric,
                                                             errors="coerce")

        intercept_mask = ((events[EVENT] == INTERCEPTOR_HIT) &
                          (events[AGENT_TYPE] == MISSILE_INTERCEPTOR))
        intercept_positions = positions.loc[intercept_mask].dropna().to_numpy(
            dtype=float)
        if intercept_positions.size:
            intercept_arrays.setdefault(category,
                                        []).append(intercept_positions)

        launch_mask = ((events[EVENT] == NEW_INTERCEPTOR) &
                       (events[AGENT_TYPE] == MISSILE_INTERCEPTOR))
        launch_positions = positions.loc[launch_mask].dropna().to_numpy(
            dtype=float)
        if launch_positions.size:
            launch_arrays.setdefault(category, []).append(launch_positions)

    intercepts = {
        category: np.concatenate(arrays)
        for category, arrays in intercept_arrays.items()
    }
    launches = {
        category: np.concatenate(arrays)
        for category, arrays in launch_arrays.items()
    }
    return intercepts, launches


def _latency_color_normalizer(latency_values: np.ndarray) -> colors.Normalize:
    """Uses a symlog color scale when latency spans several decades."""
    finite = latency_values[np.isfinite(latency_values)]
    if finite.size == 0:
        return colors.Normalize(vmin=0.0, vmax=1.0)
    minimum = float(np.min(finite))
    maximum = float(np.max(finite))
    if minimum == maximum:
        padding = max(abs(minimum) * 0.05, 0.5)
        return colors.Normalize(vmin=minimum - padding, vmax=maximum + padding)
    positive = finite[finite > 0]
    if positive.size and maximum / float(np.min(positive)) >= 100:
        return colors.SymLogNorm(
            linthresh=float(np.min(positive)) / 2,
            vmin=min(0.0, minimum),
            vmax=maximum,
        )
    return colors.Normalize(vmin=minimum, vmax=maximum)


def _format_position_axes(
    ax: plt.Axes,
    title: str,
    subtitle: str,
) -> None:
    ax.set_aspect("equal", adjustable="box")
    ax.set_xlabel("X Position [m]")
    ax.set_ylabel("Z Position [m]")
    set_axis_title(ax, title, subtitle)
    ax.grid(alpha=0.2)


def _add_latency_colorbar(
    fig: plt.Figure,
    ax: plt.Axes,
    norm: colors.Normalize,
    cmap: str,
    latency_label: str,
) -> None:
    mappable = plt.cm.ScalarMappable(norm=norm, cmap=cmap)
    mappable.set_array([])
    fig.colorbar(mappable, ax=ax, label=latency_label)


def plot_latency_position_maps(
    run_data: pd.DataFrame,
    latency_column: str,
    latency_label: str,
    experiment_label: str,
    output_dir: Path | None = None,
    filename_prefix: str = "positions",
) -> None:
    """Creates three latency-colored spatial views."""
    intercepts, launches = _collect_latency_spatial_points(
        run_data, latency_column)
    if intercepts.size == 0 and launches.size == 0:
        logging.warning("No spatial events found for %s.", experiment_label)
        return

    latency_arrays = [
        points[:, 2] for points in (intercepts, launches) if points.size
    ]
    norm = _latency_color_normalizer(np.concatenate(latency_arrays))
    cmap = "viridis"

    if intercepts.size:
        fig, ax = plt.subplots(figsize=(11, 8))
        ax.scatter(
            intercepts[:, 0],
            intercepts[:, 1],
            c=intercepts[:, 2],
            cmap=cmap,
            norm=norm,
            marker="o",
            s=8,
            alpha=0.35,
            edgecolors="none",
            rasterized=True,
        )
        _format_position_axes(
            ax,
            f"{experiment_label}: Missile Interceptor Hit Positions",
            ("Each circle is an INTERCEPTOR_HIT position in the X-Z plane; "
             "color encodes the tested latency."),
        )
        _add_latency_colorbar(fig, ax, norm, cmap, latency_label)
        fig.tight_layout()
        output_path = (output_dir / f"{filename_prefix}_hit_positions.png"
                       if output_dir is not None else None)
        _finish_figure(fig, output_path)

    if launches.size:
        fig, ax = plt.subplots(figsize=(11, 8))
        ax.scatter(
            launches[:, 0],
            launches[:, 1],
            c=launches[:, 2],
            cmap=cmap,
            norm=norm,
            marker="^",
            s=10,
            alpha=0.3,
            edgecolors="none",
            rasterized=True,
        )
        _format_position_axes(
            ax,
            f"{experiment_label}: Carrier Release Positions "
            "(Missile Interceptor Launches)",
            ("Each triangle is a missile-interceptor NEW_INTERCEPTOR position "
             "in the X-Z plane; color encodes latency."),
        )
        _add_latency_colorbar(fig, ax, norm, cmap, latency_label)
        fig.tight_layout()
        output_path = (output_dir / f"{filename_prefix}_launch_positions.png"
                       if output_dir is not None else None)
        _finish_figure(fig, output_path)

    fig, ax = plt.subplots(figsize=(11, 8))
    if intercepts.size:
        ax.scatter(
            intercepts[:, 0],
            intercepts[:, 1],
            c=intercepts[:, 2],
            cmap=cmap,
            norm=norm,
            marker="o",
            s=8,
            alpha=0.3,
            edgecolors="none",
            rasterized=True,
        )
    if launches.size:
        ax.scatter(
            launches[:, 0],
            launches[:, 1],
            c=launches[:, 2],
            cmap=cmap,
            norm=norm,
            marker="^",
            s=10,
            alpha=0.25,
            edgecolors="none",
            rasterized=True,
        )
    _format_position_axes(
        ax,
        f"{experiment_label}: Missile Interceptor Launches and Hits",
        ("Triangles mark launches and circles mark hits in the X-Z plane; "
         "color encodes the tested latency."),
    )
    ax.legend(handles=[
        Line2D([], [],
               color="black",
               marker="o",
               linestyle="None",
               markersize=6,
               label="Missile Interceptor Hit"),
        Line2D([], [],
               color="black",
               marker="^",
               linestyle="None",
               markersize=7,
               label="Carrier Release / Missile Interceptor Launch"),
    ])
    _add_latency_colorbar(fig, ax, norm, cmap, latency_label)
    fig.tight_layout()
    output_path = (output_dir / f"{filename_prefix}_launches_and_hits.png"
                   if output_dir is not None else None)
    _finish_figure(fig, output_path)


def plot_category_position_maps(
    run_data: pd.DataFrame,
    category_column: str,
    category_label: str,
    experiment_label: str,
    category_colors: dict[object, str] | None = None,
    output_dir: Path | None = None,
    filename_prefix: str = "positions",
) -> None:
    """Creates three spatial views colored by a categorical condition."""
    intercepts, launches = _collect_category_spatial_points(
        run_data, category_column)
    available_categories = set(intercepts).union(launches)
    if not available_categories:
        logging.warning("No spatial events found for %s.", experiment_label)
        return

    preferred_order = (list(category_colors)
                       if category_colors is not None else [])
    categories = [
        category for category in preferred_order
        if category in available_categories
    ]
    categories.extend(
        sorted(available_categories.difference(categories), key=str))
    colors_by_category = {
        category: (category_colors[category] if category_colors is not None and
                   category in category_colors else
                   DISTINCT_SERIES_COLORS[index % len(DISTINCT_SERIES_COLORS)]
                  ) for index, category in enumerate(categories)
    }

    def plot_points(
        ax: plt.Axes,
        grouped_points: dict[object, np.ndarray],
        marker: str,
        size: float,
        alpha: float,
    ) -> None:
        for category in categories:
            points = grouped_points.get(category)
            if points is None or not points.size:
                continue
            ax.scatter(
                points[:, 0],
                points[:, 1],
                color=colors_by_category[category],
                marker=marker,
                s=size,
                alpha=alpha,
                edgecolors="none",
                rasterized=True,
            )

    def category_handles(marker: str = "o") -> list[Line2D]:
        return [
            Line2D(
                [],
                [],
                color=colors_by_category[category],
                marker=marker,
                linestyle="None",
                markersize=7,
                label=str(category),
            ) for category in categories
        ]

    if intercepts:
        fig, ax = plt.subplots(figsize=(11, 8))
        plot_points(ax, intercepts, marker="o", size=8, alpha=0.35)
        _format_position_axes(
            ax,
            f"{experiment_label}: Missile Interceptor Hit Positions by "
            f"{category_label}",
            ("Each circle is an INTERCEPTOR_HIT position in the X-Z plane; "
             f"color identifies the {category_label.lower()}."),
        )
        ax.legend(handles=category_handles(), title=category_label)
        fig.tight_layout()
        output_path = (output_dir / f"{filename_prefix}_hit_positions.png"
                       if output_dir is not None else None)
        _finish_figure(fig, output_path)

    if launches:
        fig, ax = plt.subplots(figsize=(11, 8))
        plot_points(ax, launches, marker="^", size=10, alpha=0.3)
        _format_position_axes(
            ax,
            f"{experiment_label}: Carrier Release Positions by "
            f"{category_label}",
            ("Each triangle is a missile-interceptor NEW_INTERCEPTOR position "
             f"in the X-Z plane; color identifies the "
             f"{category_label.lower()}."),
        )
        ax.legend(handles=category_handles(marker="^"), title=category_label)
        fig.tight_layout()
        output_path = (output_dir / f"{filename_prefix}_launch_positions.png"
                       if output_dir is not None else None)
        _finish_figure(fig, output_path)

    fig, ax = plt.subplots(figsize=(11, 8))
    plot_points(ax, intercepts, marker="o", size=8, alpha=0.3)
    plot_points(ax, launches, marker="^", size=10, alpha=0.25)
    _format_position_axes(
        ax,
        f"{experiment_label}: Missile Interceptor Launches and Hits by "
        f"{category_label}",
        ("Triangles mark launches and circles mark hits in the X-Z plane; "
         f"color identifies the {category_label.lower()}."),
    )
    category_legend = ax.legend(
        handles=[
            Line2D([], [],
                   color=colors_by_category[category],
                   linewidth=3,
                   label=str(category)) for category in categories
        ],
        title=category_label,
        loc="upper right",
    )
    ax.add_artist(category_legend)
    ax.legend(
        handles=[
            Line2D([], [],
                   color="black",
                   marker="o",
                   linestyle="None",
                   markersize=6,
                   label="Missile Interceptor Hit"),
            Line2D([], [],
                   color="black",
                   marker="^",
                   linestyle="None",
                   markersize=7,
                   label="Carrier Release / Missile Interceptor Launch"),
        ],
        title="Event Type",
        loc="lower right",
    )
    fig.tight_layout()
    output_path = (output_dir / f"{filename_prefix}_launches_and_hits.png"
                   if output_dir is not None else None)
    _finish_figure(fig, output_path)
