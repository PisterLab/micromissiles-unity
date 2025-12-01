"""Implements various utility functions."""

from pathlib import Path

import pandas as pd
from absl import logging
from constants import EVENT_LOG_FILE_PREFIX, TELEMETRY_FILE_PREFIX


def find_all_files(dir: str, file_pattern: str) -> list[Path]:
    """Returns all files in the directory and its subdirectories that match the
    file pattern.

    If no files match the given pattern, returns an empty list.

    Args:
        dir: Directory to look through.
        file_pattern: File pattern to match.
    """
    files = list(Path(dir).rglob(file_pattern))
    if not files:
        logging.warning(f"No files found matching the pattern {file_pattern} "
                        f"in the directory: {dir}.")
    return files


def find_all_telemetry_files(log_dir: str) -> list[Path]:
    """Returns all telemetry files in the directory and its subdirectories.

    Args:
        log_dir: Log directory.
    """
    return find_all_files(log_dir, f"{TELEMETRY_FILE_PREFIX}_*.csv")


def find_all_event_logs(log_dir: str) -> list[Path]:
    """Returns all event logs in the directory and its subdirectories.

    Args:
        log_dir: Log directory.
    """
    return find_all_files(log_dir, f"{EVENT_LOG_FILE_PREFIX}_*.csv")


def find_latest_file(dir: str, file_pattern: str) -> Path | None:
    """Returns the latest file in the directory and its subdirectories that
    matches the file pattern.

    If no files match the given pattern, returns None.

    Args:
        dir: Directory to look through.
        file_pattern: File pattern to match.
    """
    files = find_all_files(dir, file_pattern)
    if not files:
        return None
    latest_file = max(files, key=lambda path: path.stat().st_ctime)
    logging.info(f"Using latest file found: {latest_file}.")
    return latest_file


def find_latest_telemetry_file(log_dir: str) -> Path | None:
    """Returns the latest telemetry file.

    Args:
        log_dir: Log directory.
    """
    return find_latest_file(log_dir, f"{TELEMETRY_FILE_PREFIX}_*.csv")


def find_latest_event_log(log_dir: str) -> Path | None:
    """Returns the latest event log.

    Args:
        log_dir: Log directory.
    """
    return find_latest_file(log_dir, f"{EVENT_LOG_FILE_PREFIX}_*.csv")


def find_all_subdirectories(dir: str, subdir_pattern: str) -> list[Path]:
    """Returns all subdirectories within the directory and its subdirectories
    that match the file pattern.

    If no subdirectories match the given pattern, returns an empty list.

    Args:
        dir: Directory to look through.
        subdir_pattern: Subdirectory pattern to match.
    """
    paths = Path(dir).rglob(subdir_pattern)
    subdirs = [path for path in paths if path.is_dir()]
    if not subdirs:
        logging.warning(
            f"No subdirectories found matching the pattern {subdir_pattern} "
            f"in the directory: {dir}.")
    return subdirs


def read_telemetry_file(path: str | Path) -> pd.DataFrame:
    """Reads the telemetry file into a dataframe.

    Args:
        path: Path to the telemetry file.

    Returns:
        A dataframe containing the telemetry data.
    """
    return pd.read_csv(path)


def read_event_log(path: str | Path) -> pd.DataFrame:
    """Reads the event log file into a dataframe.

    Args:
        path: Path to the event log.

    Returns:
        A dataframe containing the events.
    """
    df = pd.read_csv(path)
    # Sanitize the event column to ensure consistency.
    df["Event"] = df["Event"].str.upper().str.strip()
    return df
