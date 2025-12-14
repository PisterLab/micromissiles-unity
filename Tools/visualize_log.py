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
from constants import AgentType, EventType

FLAGS = flags.FLAGS


@dataclass(frozen=True)
class EventMarker:
    """Event marker."""
    marker: str
    color: str
    label: str


# Map from agent type to visualization color.
AGENT_TYPE_TO_COLOR = {
    AgentType.INTERCEPTOR: "blue",
    AgentType.THREAT: "red",
}

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
    event_counts = event_df["Event"].value_counts()
    logging.info("Event counts:")
    for event_type, count in event_counts.items():
        logging.info("  %s: %d", event_type, count)

    # Calculate the time duration of the events.
    start_time = event_df["Time"].min()
    end_time = event_df["Time"].max()
    duration = end_time - start_time
    logging.info("Total duration of events: %.2f seconds (from %.2f to %.2f).",
                 duration, start_time, end_time)

    # Determine the times of the hits and misses.
    hits = event_df[event_df["Event"] == EventType.INTERCEPTOR_HIT]
    misses = event_df[event_df["Event"] == EventType.INTERCEPTOR_MISS]
    logging.info("Number of hits recorded: %d.", len(hits))
    if not hits.empty:
        first_hit_time = hits["Time"].min()
        last_hit_time = hits["Time"].max()
        logging.info("  First hit at %.2f, last hit at %.2f.", first_hit_time,
                     last_hit_time)
    logging.info("Number of misses recorded: %d.", len(misses))
    if not misses.empty:
        first_miss_time = misses["Time"].min()
        last_miss_time = misses["Time"].max()
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
    for _, agent_data in telemetry_df.groupby("AgentID"):
        agent_type = agent_data["AgentType"].iloc[0]
        color = AGENT_TYPE_TO_COLOR.get(agent_type, "black")

        ax.plot(
            agent_data["AgentX"],
            agent_data["AgentZ"],
            agent_data["AgentY"],
            color=color,
            alpha=0.5,
            linewidth=0.5,
        )

    # Plot the events.
    for event_type, event_marker in EVENT_TYPE_TO_MARKER.items():
        event_data = event_df[event_df["Event"] == event_type]
        if not event_data.empty:
            ax.scatter(
                event_data["PositionX"],
                event_data["PositionZ"],
                event_data["PositionY"],
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
    fig.tight_layout()
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
        raise ValueError("Missing required telemetry or event log file.")

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
