"""Implements various utility functions."""

import glob
import os

from absl import logging
from constants import EVENT_LOG_FILE_PREFIX, TELEMETRY_FILE_PREFIX


def find_latest_file(dir: str, file_pattern: str) -> str:
    """Returns the latest file in the directory that matches the file pattern.

    If no files match the given pattern, returns None.

    Args:
        dir: Directory to look through.
        file_pattern: File pattern to match.
    """
    list_of_files = glob.glob(
        os.path.join(dir, "**", file_pattern),
        recursive=True,
    )
    if not list_of_files:
        logging.warning(f"No files found matching the pattern {file_pattern} "
                        f"in the directory: {dir}.")
        return None
    latest_file = max(list_of_files, key=os.path.getctime)
    logging.info(f"Using latest file found: {latest_file}.")
    return latest_file


def find_latest_telemetry_file(log_dir: str) -> str:
    """Returns the latest telemetry file.

    Args:
        log_dir: Log directory.
    """
    latest_log_dir = max(glob.glob(os.path.join(log_dir, "*")),
                         key=os.path.getctime)
    return find_latest_file(latest_log_dir, f"{TELEMETRY_FILE_PREFIX}*.csv")


def find_latest_event_log(log_dir: str) -> str:
    """Returns the latest event log.

    Args:
        log_dir: Log directory.
    """
    latest_telemetry_file = find_latest_telemetry_file(log_dir)
    if latest_telemetry_file:
        return latest_telemetry_file.replace(TELEMETRY_FILE_PREFIX,
                                             EVENT_LOG_FILE_PREFIX)
    return None
