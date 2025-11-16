"""Visualizes the telemetry data and events."""

import glob
import os
import platform

import matplotlib.pyplot as plt
import mpl_config
import numpy as np
import pandas as pd
from absl import app, flags, logging

FLAGS = flags.FLAGS

# Telmetry file prefix.
TELEMETRY_FILE_PREFIX = "sim_telemetry_"

# Event log prefix.
EVENT_LOG_FILE_PREFIX = "sim_events_"


def _get_logs_directory() -> str:
    """Returns the path to the logs directory."""
    system = platform.system()
    if system == "Windows":
        return os.path.expandvars(
            r"%USERPROFILE%\AppData\LocalLow\BAMLAB\micromissiles\Logs")
    elif system == "Darwin":
        return os.path.expanduser(
            "~/Library/Application Support/BAMLAB/micromissiles/Logs")
    elif system == "Linux":
        return os.path.expanduser("~/.config/unity3d/Logs")
    else:
        raise NotImplementedError(f"Unsupported platform: {system}.")


def _find_latest_file(directory: str, file_pattern: str) -> str:
    """Returns the latest file in the directory that matches the file pattern.

    If no files match the given pattern, returns None.

    Args:
        directory: Directory to look through.
        file_pattern: File pattern to match.
    """
    list_of_files = glob.glob(
        os.path.join(directory, "**", file_pattern),
        recursive=True,
    )
    if not list_of_files:
        logging.warning(f"No files found matching the pattern {file_pattern} "
                        f"in the directory: {directory}.")
        return None
    latest_file = max(list_of_files, key=os.path.getctime)
    logging.info(f"Using latest file found: {latest_file}.")
    return latest_file


def _find_latest_telemetry_file() -> str:
    """Returns the latest telemtry file."""
    logs_dir = _get_logs_directory()
    latest_log_dir = max(glob.glob(os.path.join(logs_dir, "*")),
                         key=os.path.getctime)
    return _find_latest_file(latest_log_dir, f"{TELEMETRY_FILE_PREFIX}*.csv")


def _find_latest_event_log() -> str:
    """Returns the latest event log."""
    latest_telemetry_file = _find_latest_telemetry_file()
    if latest_telemetry_file:
        return latest_telemetry_file.replace(TELEMETRY_FILE_PREFIX,
                                             EVENT_LOG_FILE_PREFIX)
    return None


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
    hits = event_df[event_df["Event"] == "INTERCEPTOR_HIT"]
    misses = event_df[event_df["Event"] == "INTERCEPTOR_MISS"]
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


def plot_telemetry(telemetry_df: str, event_df: str) -> None:
    """Plots the trajectories in the telemetry data and the events.

    Args:
        telemetry_df: Dataframe containing the telemetry data.
        event_df: Dataframe containing the events.
    """
    fig, ax = plt.subplots(figsize=(16, 8), subplot_kw={"projection": "3d"})

    # Define colors for different agent types.
    colors = {
        "M": "blue",
        "T": "red",
    }

    # Plot the agent trajectories.
    for _, agent_data in telemetry_df.groupby("AgentID"):
        agent_type = agent_data["AgentType"].iloc[0]
        color = colors.get(agent_type, "black")

        ax.plot(
            agent_data["AgentX"],
            agent_data["AgentZ"],
            agent_data["AgentY"],
            color=color,
            alpha=0.5,
            linewidth=0.5,
        )

    # Define event markers for different events.
    event_markers = {
        "INTERCEPTOR_HIT": ("o", "lime", "Hit"),
        "INTERCEPTOR_MISS": ("X", "red", "Miss"),
        "NEW_THREAT": ("^", "darkorange", "New Threat"),
        "NEW_INTERCEPTOR": ("s", "blue", "New Interceptor"),
    }

    # Plot the events.
    for event_type, (marker, color, label) in event_markers.items():
        event_data = event_df[event_df["Event"] == event_type]
        if not event_data.empty:
            ax.scatter(
                event_data["PositionX"],
                event_data["PositionZ"],
                event_data["PositionY"],
                s=60,
                c=color,
                marker=marker,
                depthshade=True,
                edgecolors="black",
                zorder=5,
                label=label,
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
    fig.tight_layout()
    plt.show()


def main(argv):
    assert len(argv) == 1, argv

    telemetry_file_path = FLAGS.telemetry_file
    event_file_path = FLAGS.event_file
    if not telemetry_file_path or not event_file_path:
        telemetry_file_path = _find_latest_telemetry_file()
        event_file_path = _find_latest_event_log()
    if not telemetry_file_path or not event_file_path:
        raise ValueError(
            "Both a telemetry file and an event log must be provided.")

    event_df = pd.read_csv(event_file_path)
    # Sanitize the event column to ensure consistency.
    event_df["Event"] = event_df["Event"].str.upper().str.strip()
    telemetry_df = pd.read_csv(telemetry_file_path)

    log_event_summary(event_df)
    plot_telemetry(telemetry_df, event_df)


if __name__ == "__main__":
    flags.DEFINE_string("telemetry_file", None,
                        "Path to the telemetry CSV file.")
    flags.DEFINE_string("event_file", None, "Path to the event CSV file.")

    app.run(main)
