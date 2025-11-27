"""Utility functions for Unity, such as finding Unity installations and
returning the persistent data path.
"""

import os
import platform
from pathlib import Path
from typing import Set

from absl import logging


def _get_unity_roots() -> list[Path]:
    """Returns a list of path roots in which to search for Unity installations."""
    roots: list[Path] = []
    system = platform.system()
    if system == "Windows":
        unity_hub_path = os.environ.get("UNITY_HUB_PATH")
        if unity_hub_path:
            roots.append(Path(unity_hub_path) / "Editor")
        for env_variable in ("ProgramFiles", "ProgramFiles(x86)"):
            base = os.environ.get(env_variable)
            if base:
                roots.append(Path(base) / "Unity/Hub/Editor")
                roots.append(Path(base) / "Unity Hub/Editor")
                roots.append(Path(base) / "Unity/Editor")
        local_app_data = os.environ.get("LOCALAPPDATA")
        if local_app_data:
            roots.append(Path(local_app_data) / "UnityHub/Editor")
    elif system == "Darwin":
        roots.append(Path("/Applications/Unity/Hub/Editor"))
        roots.append(Path("/Applications/Unity"))
        roots.append(Path("~/Applications/Unity/Hub/Editor"))
        roots.append(Path("~/Unity/Hub/Editor"))
    elif system == "Linux":
        roots.append(Path("/opt/Unity"))
        roots.append(Path("/opt/UnityHub/Editor"))
        roots.append(Path("/opt/unityhub/Editor"))
        roots.append(Path("~/.local/share/UnityHub/Editor"))
        roots.append(Path("~/Unity/Hub/Editor"))
    else:
        raise NotImplementedError(f"Unsupported platform: {system}.")
    return [path.expanduser() for path in roots]


def _resolve_unity_executable(directory: Path) -> Path | None:
    """Resolves the path to a Unity executable in the given directory.

    Args:
        directory: Directory in which to search for a Unity executable.

    Returns:
        The resolved path of the Unity executable.
    """
    system = platform.system()
    if system == "Windows":
        candidate = directory / "Editor/Unity.exe"
        if candidate.exists():
            return candidate
    elif system == "Darwin":
        candidate = directory / "Unity.app/Contents/MacOS/Unity"
        if candidate.exists():
            return candidate
        candidate = directory / "Unity/Unity.app/Contents/MacOS/Unity"
        if candidate.exists():
            return candidate
        return None
    elif system == "Linux":
        candidate = directory / "Editor/Unity"
        if candidate.exists():
            return candidate
        candidate = directory / "Unity/Editor/Unity"
        if candidate.exists():
            return candidate
    else:
        raise NotImplementedError(f"Unsupported platform: {system}.")
    return None


def _get_default_unity_paths() -> list[Path]:
    """Returns a list of the default Unity executable paths."""
    paths: list[Path] = []
    system = platform.system()
    if system == "Windows":
        paths.extend([
            Path("C:/Program Files/Unity/Editor/Unity.exe"),
            Path("C:/Program Files (x86)/Unity/Editor/Unity.exe"),
        ])
    elif system == "Darwin":
        paths.append(Path("/Applications/Unity/Unity.app/Contents/MacOS/Unity"))
    elif system == "Linux":
        paths.append(Path("/opt/Unity/Editor/Unity"))
    else:
        raise NotImplementedError(f"Unsupported platform: {system}")
    return paths


def _find_unity_executables() -> list[Path]:
    """Returns a list of paths to the Unity executables."""
    installs: Set[Path] = set()
    for root in _get_unity_roots():
        if not root.exists():
            continue
        for entry in sorted(root.iterdir()):
            if not entry.is_dir():
                continue
            executable = _resolve_unity_executable(entry)
            if executable and executable.exists():
                installs.add(executable.resolve())

    for executable in _get_default_unity_paths():
        if executable.exists():
            installs.add(executable.resolve())
    return sorted(list(installs), key=str)


def find_unity_path(unity_path: str) -> Path:
    """Returns the path to a Unity executable."""
    if unity_path:
        unity_path = Path(unity_path).expanduser()
        if not unity_path.exists():
            raise ValueError(f"Unity executable not found: {unity_path}.")
        return unity_path

    executables = _find_unity_executables()
    if not executables:
        raise ValueError("No Unity installations detected. "
                         "Use --unity-path or set $UNITY_PATH.")
    if len(executables) > 1:
        logging.warning("Found multiple Unity installations.")
    logging.info("Found Unity installation at %s.", executables[0])
    return executables[0]


def get_persistent_data_directory() -> str:
    """Returns the path to Unity's persistent data directory."""
    system = platform.system()
    if system == "Windows":
        return os.path.expandvars(
            r"%USERPROFILE%\AppData\LocalLow\BAMLAB\micromissiles\Logs")
    elif system == "Darwin":
        return os.path.expanduser(
            "~/Library/Application Support/BAMLAB/micromissiles/Logs")
    elif system == "Linux":
        return os.path.expanduser("~/.config/unity3d/BAMLAB/micromissiles/Logs")
    else:
        raise NotImplementedError(f"Unsupported platform: {system}.")
