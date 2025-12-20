"""Processes the logs of a simulation run."""

import matplotlib
import matplotlib.pyplot as plt
import mpl_config
import multi_metric
import numpy as np
import pandas as pd
import scalar_metric
import unity_utils
import utils
from absl import app, flags, logging
from aggregator import Aggregator
from distribution import Distribution

FLAGS = flags.FLAGS

# Pixel size in meters.
PIXEL_SIZE = 20

# Scalar metrics.
SCALAR_METRICS = [
    scalar_metric.NumMissileInterceptors(),
    scalar_metric.NumMissileInterceptorHits(),
    scalar_metric.NumMissileInterceptorMisses(),
    scalar_metric.MissileInterceptorHitRate(),
    scalar_metric.MissileInterceptorEfficiency(),
    scalar_metric.MinInterceptDistance(),
]

# Multi-metrics.
MULTI_METRICS = [
    multi_metric.InterceptPosition2D(),
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


def plot_heatmap_and_scatter(
    event_dfs: list[pd.DataFrame],
    metric: multi_metric.MultiMetric,
    ax: matplotlib.axes.Axes,
    color: str,
) -> None:
    """Plots a heatmap and a scatter plot of the multi-metric aggregated over
    all simulation runs.

    Args:
        event_dfs: List of dataframes containing the events.
        metric: Multi-metric outputting 2D coordinates.
    """
    aggregator = Aggregator(event_dfs, metric)
    values = np.array(aggregator.values)
    if values.size == 0:
        logging.warning("No metric values for metric: %s.", metric.name)
        return

    xvalues = values[:, 0]
    yvalues = values[:, 1]

    # Plot a scatter plot of the metric values.
    ax.scatter(xvalues, yvalues, s=PIXEL_SIZE, c=color, alpha=0.2, linewidths=0)


def main(argv):
    assert len(argv) == 1, argv

    run_log_dir = FLAGS.run_log_dir
    for metric in MULTI_METRICS:
        fig, axes = plt.subplots(1,
                                 len(run_log_dir),
                                 figsize=(12, 4),
                                 sharex=True,
                                 sharey=True)
        xmin = -2500
        xmax = 2500
        ymin = 2000
        ymax = 12000
        colors = ["C0", "C3", "C3", "C1", "C1", "C1"]
        for i, dir in enumerate(run_log_dir):
            ax = axes[i]
            event_log_paths = utils.find_all_event_logs(dir)
            event_dfs = [utils.read_event_log(path) for path in event_log_paths]
            plot_heatmap_and_scatter(event_dfs, metric, ax, colors[i])
            ax.set_xlim(xmin, xmax)
            ax.set_ylim(ymin, ymax)
            ax.set_aspect("equal")
        plt.show()


if __name__ == "__main__":
    flags.DEFINE_multi_string(
        "run_log_dir", [
            "/Users/titan/Documents/micromissiles-assets/logs/batch_5_swarms_100_ucav_20251218_213319_baseline",
            "/Users/titan/Documents/micromissiles-assets/logs/batch_5_swarms_100_ucav_20251218_213913_kp_0_8",
            "/Users/titan/Documents/micromissiles-assets/logs/batch_5_swarms_100_ucav_20251218_214053_kp_0_5",
            "/Users/titan/Documents/micromissiles-assets/logs/batch_5_swarms_100_ucav_20251218_214542_no_interceptor_reassignment",
            "/Users/titan/Documents/micromissiles-assets/logs/batch_5_swarms_100_ucav_20251218_214724_kp_0_8_no_interceptor_reassignment",
            "/Users/titan/Documents/micromissiles-assets/logs/batch_5_swarms_100_ucav_20251218_215034_kp_0_5_no_interceptor_reassignment",
        ],
        "Run log directory containing subdirectories for the logs of each run.")
    flags.DEFINE_string("run_log_search_dir",
                        unity_utils.get_persistent_data_directory(),
                        "Log directory in which to search for logs.")

    app.run(main)
