import argparse
import glob
import os
import platform

import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
from mpl_toolkits.mplot3d import Axes3D


def get_logs_directory():
    if platform.system() == "Windows":
        return os.path.expandvars(
            r"%USERPROFILE%\AppData\LocalLow\BAMLAB\micromissiles\Telemetry\Logs"
        )
    elif platform.system() == "Darwin":
        return os.path.expanduser(
            "~/Library/Application Support/BAMLAB/micromissiles/Telemetry/Logs"
        )
    else:
        raise NotImplementedError(
            f"Unsupported platform: {platform.system()}.")


def find_latest_file(directory, file_pattern):
    list_of_files = glob.glob(os.path.join(directory, file_pattern))
    if not list_of_files:
        print(f"No files matching '{file_pattern}' found in {directory}.")
        return None
    latest_file = max(list_of_files, key=os.path.getctime)
    print(f"Using latest file: {latest_file}.")
    return latest_file


def find_latest_telemetry_file():
    logs_dir = get_logs_directory()
    latest_log_dir = max(glob.glob(os.path.join(logs_dir, "*")),
                         key=os.path.getctime)
    return find_latest_file(latest_log_dir, "sim_telemetry_*.csv")


def find_latest_event_log():
    latest_telemetry_file = find_latest_telemetry_file()
    if latest_telemetry_file:
        return latest_telemetry_file.replace("sim_telemetry_", "sim_events_")
    else:
        return None


def plot_telemetry(telemetry_file_path, event_file_path):
    # Read the telemetry CSV file.
    df = pd.read_csv(telemetry_file_path)

    # Read the event CSV file.
    event_df = pd.read_csv(event_file_path)

    # Sanitize the "Event" column to ensure consistency.
    event_df["Event"] = event_df["Event"].str.upper().str.strip()

    # Debugging: Print unique event types to verify correct parsing.
    unique_events = event_df["Event"].unique()
    print(f"Unique Events Found: {unique_events}")

    # Create a 3D plot.
    fig = plt.figure(figsize=(14, 10))
    ax = fig.add_subplot(111, projection="3d")

    # Define colors for different agent types.
    colors = {"T": "red", "M": "blue"}

    # Group data by AgentID.
    agent_types = set()
    for agent_id, agent_data in df.groupby("AgentID"):
        agent_type = agent_data["AgentType"].iloc[0]
        color = colors.get(agent_type, "black")
        downsampled = agent_data.iloc[::10]

        ax.plot(downsampled["AgentX"],
                downsampled["AgentZ"],
                downsampled["AgentY"],
                color=color,
                alpha=0.5,
                linewidth=0.5,
                label=f"Agent Type: {agent_type}")
        agent_types.add(agent_type)

    # Define event markers with higher zorder for visibility.
    event_markers = {
        "HIT": ("o", "green", "Hit"),
        "MISS": ("x", "red", "Miss"),
        "NEW_THREAT": ("^", "orange", "New Threat"),
        "NEW_INTERCEPTOR": ("s", "blue", "New Interceptor")
    }

    # Plot events.
    for event_type, (marker, color, label) in event_markers.items():
        event_data = event_df[event_df["Event"] == event_type]
        if not event_data.empty:
            ax.scatter(
                event_data["PositionX"],
                event_data["PositionZ"],
                event_data["PositionY"],
                c=color,
                marker=marker,
                s=100,  # Increased marker size for better visibility.
                label=label,
                edgecolors="k",  # Adding black edges for contrast.
                depthshade=True,
                zorder=5  # Ensure markers are on top.
            )

    # Set labels.
    ax.set_xlabel("X (m)", fontsize=12)
    ax.set_ylabel("Z (m)", fontsize=12)
    ax.set_zlabel("Y (m)", fontsize=12)

    # Set view angle for better visualization.
    ax.view_init(elev=20, azim=45)

    # Add a ground plane for reference.
    x_min, x_max = ax.get_xlim()
    z_min, z_max = ax.get_ylim()
    xx, zz = np.meshgrid(np.linspace(x_min, x_max, 2),
                         np.linspace(z_min, z_max, 2))
    yy = np.zeros_like(xx)
    ax.plot_surface(xx, zz, yy, alpha=0.2, color="green")

    plt.title("Agents Trajectories and Events (X: Right, Z: Forward, Y: Up)",
              fontsize=14)

    # Optimize legend to prevent overcrowding.
    handles, labels = ax.get_legend_handles_labels()
    # Remove duplicate labels.
    unique = dict(zip(labels, handles))
    ax.legend(unique.values(),
              unique.keys(),
              loc="upper left",
              bbox_to_anchor=(1, 1),
              fontsize=10)

    plt.tight_layout()
    plt.show()


def print_summary(telemetry_file_path, event_file_path):
    # Read the telemetry CSV file.
    df = pd.read_csv(telemetry_file_path)

    # Read the event CSV file.
    event_df = pd.read_csv(event_file_path)

    # Sanitize the "Event" column to ensure consistency.
    event_df["Event"] = event_df["Event"].str.upper().str.strip()

    # Print total number of events.
    total_events = len(event_df)
    print(f"Total number of events: {total_events}")

    # Print counts of each event type.
    event_counts = event_df["Event"].value_counts()
    print("\nEvent Counts:")
    for event_type, count in event_counts.items():
        print(f"  {event_type}: {count}")

    # Calculate the time duration of the events.
    if "Time" in event_df.columns:
        start_time = event_df["Time"].min()
        end_time = event_df["Time"].max()
        duration = end_time - start_time
        print(f"\nTotal duration of events: {duration:.2f} seconds (from "
              f"{start_time:.2f} to {end_time:.2f})")
    else:
        print("\n'Time' column not found in event data.")

    if "Time" in event_df.columns:
        hits = event_df[event_df["Event"] == "HIT"]
        misses = event_df[event_df["Event"] == "MISS"]

        if not hits.empty:
            first_hit_time = hits["Time"].min()
            last_hit_time = hits["Time"].max()
            print(f"\nFirst hit at {first_hit_time:.2f} seconds, last hit at "
                  f"{last_hit_time:.2f} seconds")
        else:
            print("\nNo hits recorded.")

        if not misses.empty:
            first_miss_time = misses["Time"].min()
            last_miss_time = misses["Time"].max()
            print(f"First miss at {first_miss_time:.2f} seconds, last miss at "
                  f"{last_miss_time:.2f} seconds")
        else:
            print("No misses recorded.")
    else:
        print("\n'Time' column not found in event data.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="Visualize telemetry data and events.")
    parser.add_argument("telemetry_file",
                        nargs="?",
                        default=None,
                        help="Path to telemetry CSV file.")
    parser.add_argument("event_file",
                        nargs="?",
                        default=None,
                        help="Path to event CSV file.")
    args = parser.parse_args()

    if args.telemetry_file and args.event_file:
        telemetry_file_path = args.telemetry_file
        event_file_path = args.event_file
    else:
        telemetry_file_path = find_latest_telemetry_file()
        event_file_path = find_latest_event_log()
        if telemetry_file_path is None or event_file_path is None:
            exit(1)
    print_summary(telemetry_file_path, event_file_path)
    plot_telemetry(telemetry_file_path, event_file_path)
