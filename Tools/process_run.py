"""Processes the logs of a simulation run."""

import matplotlib.pyplot as plt
import mpl_config
import numpy as np
import pandas as pd
import unity_utils
import utils
from absl import app, flags, logging
from aggregator import Aggregator
from distribution import Distribution
from multi_metric import InterceptPosition2D, MultiMetric
from scalar_metric import (HitRate, InterceptorEfficiency,
                           MinInterceptorDistance, NumHits, NumInterceptors,
                           NumMisses)

FLAGS = flags.FLAGS

# Pixel size in meters.
PIXEL_SIZE = 20

# Scalar metrics.
SCALAR_METRICS = [
    NumInterceptors(),
    NumHits(),
    NumMisses(),
    HitRate(),
    InterceptorEfficiency(),
    MinInterceptorDistance(),
]


def print_aggregated_stats(event_dfs: list[pd.DataFrame]) -> None:
    """Prints the aggregated statistics of the simulation run.

    Args:
        event_dfs: List of dataframes containing the events.
    """
    if not event_dfs:
        logging.warning("No simulation runs to aggregate stats.")
        return

    num_runs = len(event_dfs)
    logging.info(
        "Aggregating the stats for %d runs found in the log directory.",
        num_runs)

    for metric in SCALAR_METRICS:
        distribution = Distribution(event_dfs, metric)
        mean = distribution.mean()
        std = distribution.std()
        logging.info("  %s: mean: %f, std: %f.", metric.name, mean, std)


def plot_heatmap(event_dfs: list[pd.DataFrame], metric: MultiMetric) -> None:
    """Plots the heatmap of the multi-metric aggregated over all simulation runs.

    Args:
        event_dfs: List of dataframes containing the events.
        metric: Multi-metric outputting 2D coordinates.
    """
    aggregator = Aggregator(event_dfs, metric)
    values = np.array(aggregator.values)
    if values.size == 0:
        logging.warning("No metric values for metric: %s.", metric.name)
        return

    # Calculate the extent to plot.
    xvalues = values[:, 0]
    xmin = min(xvalues)
    xmax = max(xvalues)
    xrange = xmax - xmin
    yvalues = values[:, 1]
    ymin = min(yvalues)
    ymax = max(yvalues)
    yrange = ymax - ymin
    xpad = xrange * 0.05 if xrange > 0 else 1
    ypad = yrange * 0.05 if yrange > 0 else 1
    xedges = np.linspace(xmin - xpad, xmax + xpad,
                         max(int(xrange / PIXEL_SIZE) + 1, 2))
    yedges = np.linspace(ymin - ypad, ymax + ypad,
                         max(int(yrange / PIXEL_SIZE) + 1, 2))

    # Plot a 2D histogram of the metric values.
    fig, ax = plt.subplots(figsize=(16, 8))
    H, _, _ = np.histogram2d(values[:, 0], values[:, 1], bins=[xedges, yedges])
    H = H.T
    im = ax.imshow(
        H,
        cmap="hot",
        aspect="equal",
        interpolation="bilinear",
        origin="lower",
        extent=[xedges[0], xedges[-1], yedges[0], yedges[-1]],
    )
    plt.colorbar(im, ax=ax)
    ax.set_xlabel("$x$ [m]")
    ax.set_ylabel("$z$ [m]")
    ax.set_title(metric.name)
    ax.grid(alpha=0.25)

    # Plot a scatter plot of the metric values.
    fig, ax = plt.subplots(figsize=(16, 8))
    ax.scatter(xvalues, yvalues, s=PIXEL_SIZE, c="red", alpha=0.8)
    ax.set_aspect("equal")
    ax.set_xlabel("$x$ [m]")
    ax.set_ylabel("$z$ [m]")
    ax.set_xlim(xedges[0], xedges[-1])
    ax.set_ylim(yedges[0], yedges[-1])
    ax.set_title(metric.name)

    plt.show()


def main(argv):
    assert len(argv) == 1, argv

    run_log_dir = FLAGS.run_log_dir
    if not run_log_dir:
        run_log_dir = utils.find_latest_subdirectory(FLAGS.run_log_search_dir)
    event_log_paths = utils.find_all_event_logs(run_log_dir)
    event_dfs = [utils.read_event_log(path) for path in event_log_paths]
    print_aggregated_stats(event_dfs)
    plot_heatmap(event_dfs, InterceptPosition2D())


if __name__ == "__main__":
    flags.DEFINE_string(
        "run_log_dir", None,
        "Run log directory containing subdirectories for the logs of each run.")
    flags.DEFINE_string("run_log_search_dir",
                        unity_utils.get_persistent_data_directory(),
                        "Log directory in which to search for logs.")

    app.run(main)
