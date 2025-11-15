"""Builds a standalone player for the current operating system.

The build output is written to the <build root>/<timestamp> directory.
"""

import datetime
import os
import platform
import subprocess
from dataclasses import dataclass
from pathlib import Path

import unity
from absl import app, flags, logging

FLAGS = flags.FLAGS

# Path to the repository root.
REPO_ROOT = Path(__file__).resolve().parents[1]

# Path to the default build root directory.
DEFAULT_BUILD_ROOT = REPO_ROOT / "Build"

# Default name of the standalone build.
DEFAULT_BUILD_NAME = "micromissiles"


@dataclass(frozen=True)
class PlatformTarget:
    """Platform target."""
    unity_name: str
    build_flag: str
    extension: str


# Platform targets.
PLATFORM_TARGETS = {
    "Windows":
        PlatformTarget("StandaloneWindows64", "-buildWindows64Player", ".exe"),
    "Darwin":
        PlatformTarget("StandaloneOSX", "-buildOSXUniversalPlayer", ".app"),
    "Linux":
        PlatformTarget("StandaloneLinux64", "-buildLinux64Player", ".x86_64"),
}


def build_player(unity_path: Path, build_name: str, build_root: str) -> None:
    """Builds the standalone player.

    Args:
        unity_path: Path to the Unity installation.
        build_name: Unity build name.
        build_root: Unity build root directory.
    """
    system = platform.system()
    if system not in PLATFORM_TARGETS:
        raise NotImplementedError(f"Unsupported platform: {system}.")

    target = PLATFORM_TARGETS[system]
    timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
    build_dir = Path(build_root) / timestamp
    build_dir.mkdir(parents=True, exist_ok=True)
    player_path = build_dir / f"{build_name}{target.extension}"
    log_path = build_dir / "unity.log"
    logging.info("Building %s to %s.", target.unity_name, player_path)

    # Build the standalone player.
    command = [
        str(unity_path),
        "-batchmode",
        "-nographics",
        "-quit",
        "-projectPath",
        str(REPO_ROOT),
        target.build_flag,
        str(player_path),
        "-logFile",
        str(log_path),
        "-buildTarget",
        target.unity_name,
    ]
    logging.info("Running command: %s", " ".join(command))
    subprocess.run(command, check=True)
    logging.info("Standalone player: %s.", player_path)
    logging.info("Unity log: %s.", log_path)


def main(argv):
    assert len(argv) == 1, argv

    unity_path = unity.find_unity_path(FLAGS.unity_path)
    build_player(unity_path, FLAGS.build_name, FLAGS.build_root)


if __name__ == "__main__":
    flags.DEFINE_string("unity_path", os.environ.get("UNITY_PATH"),
                        "Path to the Unity installation.")
    flags.DEFINE_string("build_name", DEFAULT_BUILD_NAME, "Build name.")
    flags.DEFINE_string("build_root", str(DEFAULT_BUILD_ROOT),
                        "Build root directory.")

    app.run(main)
