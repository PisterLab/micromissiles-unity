"""Visualizes the telemetry data and events."""

from dataclasses import dataclass
from pathlib import Path

import matplotlib.pyplot as plt
import mpl_config
import numpy as np
import pandas as pd
import unity_utils
import utils
from absl import app, flags, logging
from constants import Column, EventType, get_agent_color

FLAGS = flags.FLAGS


@dataclass(frozen=True)
class EventMarker:
    """Event marker."""
    marker: str
    color: str
    label: str


# Map from event type to event marker.
EVENT_TYPE_TO_MARKER = {
    EventType.NEW_INTERCEPTOR: EventMarker("s", "blue", "New Interceptor"),
    EventType.NEW_THREAT: EventMarker("^", "darkorange", "New Threat"),
    EventType.INTERCEPTOR_HIT: EventMarker("o", "lime", "Hit"),
    EventType.INTERCEPTOR_MISS: EventMarker("X", "red", "Miss"),
}


def log_event_summary(event_df: pd.DataFrame) -> None:
    """Logs a summary of the events.

    Args:
        event_df: Dataframe containing the events.
    """
    # Log total number of events.
    total_events = len(event_df)
    logging.info("Total number of events: %d.", total_events)

    # Log the counts of each event type.
    event_counts = event_df[Column.EVENT].value_counts()
    logging.info("Event counts:")
    for event_type, count in event_counts.items():
        logging.info("  %s: %d", event_type, count)

    # Calculate the time duration of the events.
    start_time = event_df[Column.TIME].min()
    end_time = event_df[Column.TIME].max()
    duration = end_time - start_time
    logging.info("Total duration of events: %.2f seconds (from %.2f to %.2f).",
                 duration, start_time, end_time)

    # Determine the times of the hits and misses.
    interceptor_hits = (
        event_df[event_df[Column.EVENT] == EventType.INTERCEPTOR_HIT])
    interceptor_misses = (
        event_df[event_df[Column.EVENT] == EventType.INTERCEPTOR_MISS])
    logging.info("Number of interceptor hits recorded: %d.",
                 len(interceptor_hits))
    if not interceptor_hits.empty:
        first_hit_time = interceptor_hits[Column.TIME].min()
        last_hit_time = interceptor_hits[Column.TIME].max()
        logging.info("  First hit at %.2f, last hit at %.2f.", first_hit_time,
                     last_hit_time)
    logging.info("Number of interceptor misses recorded: %d.",
                 len(interceptor_misses))
    if not interceptor_misses.empty:
        first_miss_time = interceptor_misses[Column.TIME].min()
        last_miss_time = interceptor_misses[Column.TIME].max()
        logging.info("  First miss at %.2f, last miss at %.2f.",
                     first_miss_time, last_miss_time)


def plot_telemetry(telemetry_df: pd.DataFrame, event_df: pd.DataFrame) -> None:
    """Plots the trajectories in the telemetry data and the events.

    Args:
        telemetry_df: Dataframe containing the telemetry data.
        event_df: Dataframe containing the events.
    """
    fig, ax = plt.subplots(
        figsize=(16, 8),
        subplot_kw={"projection": "3d"},
    )

    # Plot the agent trajectories.
    for _, agent_data in telemetry_df.groupby(Column.AGENT_ID):
        agent_type = agent_data[Column.AGENT_TYPE].iloc[0]
        color = get_agent_color(agent_type)

        ax.plot(
            agent_data[Column.POSITION_X],
            agent_data[Column.POSITION_Z],
            agent_data[Column.POSITION_Y],
            color=color,
            alpha=0.5,
            linewidth=0.5,
        )

    # Plot the events.
    for event_type, event_marker in EVENT_TYPE_TO_MARKER.items():
        event_data = event_df[event_df[Column.EVENT] == event_type]
        if not event_data.empty:
            ax.scatter(
                event_data[Column.POSITION_X],
                event_data[Column.POSITION_Z],
                event_data[Column.POSITION_Y],
                s=60,
                c=event_marker.color,
                marker=event_marker.marker,
                edgecolors="black",
                label=event_marker.label,
            )

    # Add a ground plane for reference.
    x_min, x_max = ax.get_xlim()
    z_min, z_max = ax.get_ylim()
    xx, zz = np.meshgrid([x_min, x_max], [z_min, z_max])
    yy = np.zeros_like(xx)
    ax.plot_surface(xx, zz, yy, alpha=0.2, color="chocolate")

    ax.set_xlabel("$x$ [m]")
    ax.set_ylabel("$z$ [m]")
    ax.set_zlabel("$y$ [m]")

    ax.set_aspect("equal")
    ax.view_init(elev=20, azim=-45)
    ax.legend(loc="lower center")
    plt.show()


def main(argv):
    assert len(argv) == 1, argv

    if FLAGS.telemetry_file and FLAGS.event_log:
        telemetry_file_path = Path(FLAGS.telemetry_file)
        event_log_path = Path(FLAGS.event_log)
    else:
        telemetry_file_path = utils.find_latest_telemetry_file(
            FLAGS.log_search_dir)
        event_log_path = utils.find_latest_event_log(FLAGS.log_search_dir)
    if not telemetry_file_path or not event_log_path:
        raise ValueError(
            "Both a telemetry file and an event log must be provided.")

    telemetry_df = utils.read_telemetry_file(telemetry_file_path)
    event_df = utils.read_event_log(event_log_path)

    log_event_summary(event_df)
    plot_telemetry(telemetry_df, event_df)


if __name__ == "__main__":
    flags.DEFINE_string("telemetry_file", None,
                        "Path to the telemetry CSV file.")
    flags.DEFINE_string("event_log", None, "Path to the event CSV log.")
    flags.DEFINE_string("log_search_dir",
                        unity_utils.get_persistent_data_directory(),
                        "Log directory in which to search for logs.")

    app.run(main)
