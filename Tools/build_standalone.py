#!/usr/bin/env python3
"""Simplified local build helper for micromissiles-unity.

This script prompts for a Unity editor installation and builds the standalone
player for the current operating system. Build output is written to
Build/<timestamp>.
"""

from __future__ import annotations

import argparse
import datetime
import os
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable, List, Sequence

try:
  import msvcrt  # type: ignore
except ImportError:  # pragma: no cover - not available on non-Windows
  msvcrt = None
try:
  import termios
  import tty
except ImportError:  # pragma: no cover - not available on Windows
  termios = None
  tty = None


REPO_ROOT = Path(__file__).resolve().parents[1]
BUILD_ROOT = REPO_ROOT / "Build"
DEFAULT_BUILD_NAME = "micromissiles"


@dataclass(frozen=True)
class PlatformTarget:
  unity_name: str
  build_flag: str
  extension: str


PLATFORM_TARGETS = {
    "windows": PlatformTarget("StandaloneWindows64", "-buildWindows64Player", ".exe"),
    "mac": PlatformTarget("StandaloneOSX", "-buildOSXUniversalPlayer", ".app"),
    "linux": PlatformTarget("StandaloneLinux64", "-buildLinux64Player", ".x86_64"),
}


@dataclass(frozen=True)
class UnityInstall:
  version: str
  path: Path


def parse_args(argv: Iterable[str]) -> argparse.Namespace:
  parser = argparse.ArgumentParser(
      description="Build the standalone player for the current platform."
  )
  parser.add_argument(
      "--unity-path",
      dest="unity_path",
      help="Path to the Unity editor executable. Falls back to UNITY_PATH env var or interactive selection.",
  )
  return parser.parse_args(list(argv))


def run(cmd: List[str], cwd: Path | None = None) -> None:
  print(f"[build] Running: {' '.join(cmd)}", flush=True)
  result = subprocess.run(cmd, cwd=str(cwd) if cwd else None)
  if result.returncode != 0:
    raise SystemExit(result.returncode)


def discover_unity_installations() -> List[UnityInstall]:
  installs: dict[Path, UnityInstall] = {}
  for root in unity_search_roots():
    if not root.exists():
      continue
    for entry in sorted(root.iterdir()):
      if not entry.is_dir():
        continue
      version = entry.name
      executable = resolve_unity_executable(entry)
      if executable and executable.exists():
        installs.setdefault(executable.resolve(), UnityInstall(version, executable.resolve()))

  for hint in unity_executable_hints():
    if hint.exists():
      version = hint.parent.parent.name if hint.parent and hint.parent.parent else hint.name
      installs.setdefault(hint.resolve(), UnityInstall(version, hint.resolve()))

  return sorted(installs.values(), key=lambda inst: inst.version, reverse=True)


def unity_search_roots() -> List[Path]:
  roots: List[Path] = []
  home = Path.home()
  system = sys.platform

  def add(path: Path | None) -> None:
    if path:
      roots.append(path.expanduser())

  if system.startswith("win"):
    env = os.environ
    add(Path(env.get("UNITY_HUB_PATH", "")) / "Editor")
    for var in ("ProgramFiles", "ProgramFiles(x86)"):
      base = env.get(var)
      if base:
        add(Path(base) / "Unity" / "Hub" / "Editor")
        add(Path(base) / "Unity Hub" / "Editor")
        add(Path(base) / "Unity" / "Editor")
    add(Path(env.get("LOCALAPPDATA", "")) / "UnityHub" / "Editor")
    add(Path("C:/Program Files/Unity/Hub/Editor"))
    add(Path("C:/Program Files/Unity/Editor"))
  elif system == "darwin":
    add(Path("/Applications/Unity/Hub/Editor"))
    add(Path("/Applications/Unity"))
    add(home / "Applications/Unity/Hub/Editor")
    add(home / "Unity/Hub/Editor")
  else:
    add(Path("/opt/Unity"))
    add(Path("/opt/UnityHub/Editor"))
    add(Path("/opt/unityhub/Editor"))
    add(home / ".local/share/UnityHub/Editor")
    add(home / "Unity/Hub/Editor")

  return roots


def unity_executable_hints() -> List[Path]:
  hints: List[Path] = []
  system = sys.platform
  if system.startswith("win"):
    hints.extend([
        Path("C:/Program Files/Unity/Editor/Unity.exe"),
        Path("C:/Program Files (x86)/Unity/Editor/Unity.exe"),
    ])
  elif system == "darwin":
    hints.append(Path("/Applications/Unity/Unity.app/Contents/MacOS/Unity"))
  else:
    hints.append(Path("/opt/Unity/Editor/Unity"))
  return hints


def resolve_unity_executable(version_dir: Path) -> Path | None:
  system = sys.platform
  if system.startswith("win"):
    candidate = version_dir / "Editor" / "Unity.exe"
    if candidate.exists():
      return candidate
    return None
  if system == "darwin":
    candidate = version_dir / "Unity.app" / "Contents" / "MacOS" / "Unity"
    if candidate.exists():
      return candidate
    candidate = version_dir / "Unity" / "Unity.app" / "Contents" / "MacOS" / "Unity"
    if candidate.exists():
      return candidate
    return None
  candidate = version_dir / "Editor" / "Unity"
  if candidate.exists():
    return candidate
  candidate = version_dir / "Unity" / "Editor" / "Unity"
  if candidate.exists():
    return candidate
  return None


def interactive_choice(options: Sequence[str], prompt: str = "Select an option:") -> int:
  if not options:
    raise ValueError("No options provided for selection.")

  print(prompt)
  index = 0

  if os.name == "nt":  # pragma: no cover - Windows specific
    os.system("")

  def render() -> None:
    for idx, option in enumerate(options):
      selector = "➜" if idx == index else " "
      line = f"{selector} {option}"
      if idx == index:
        line = f"\033[36m{line}\033[0m"
      print(line)
    print(f"\033[{len(options)}F", end="", flush=True)

  render()
  try:
    while True:
      key = read_key()
      if key == "up":
        index = (index - 1) % len(options)
      elif key == "down":
        index = (index + 1) % len(options)
      elif key == "enter":
        break
      elif key == "abort":
        raise KeyboardInterrupt
      render()
  finally:
    print(f"\033[{len(options)}E", end="\r", flush=True)

  print(f"[build] Chosen: {options[index]}")
  return index


def read_key() -> str:
  if os.name == "nt" and msvcrt:  # pragma: no cover - Windows specific
    while True:
      ch = msvcrt.getwch()
      if ch in ("\r", "\n"):
        return "enter"
      if ch in ("q", "Q"):
        return "abort"
      if ch in ("\x00", "\xe0"):
        ch2 = msvcrt.getwch()
        if ch2 == "H":
          return "up"
        if ch2 == "P":
          return "down"
      if ch in ("k", "K"):
        return "up"
      if ch in ("j", "J"):
        return "down"
  elif termios and tty:
    fd = sys.stdin.fileno()
    old_settings = termios.tcgetattr(fd)
    try:
      tty.setraw(fd)
      ch = sys.stdin.read(1)
      if ch in ("\r", "\n"):
        return "enter"
      if ch == "\x03":
        return "abort"
      if ch == "\x1b":
        seq = sys.stdin.read(2)
        if seq == "[A":
          return "up"
        if seq == "[B":
          return "down"
      if ch in ("k", "K"):
        return "up"
      if ch in ("j", "J"):
        return "down"
    finally:
      termios.tcsetattr(fd, termios.TCSADRAIN, old_settings)
  else:  # pragma: no cover - environments without raw input support
    raise SystemExit(
        "Interactive selection is not supported in this environment. Use --unity-path instead."
    )
  return "unknown"


def determine_unity_path(arg_value: str | None) -> Path:
  candidate = arg_value or os.environ.get("UNITY_PATH")
  if candidate:
    unity_path = Path(candidate).expanduser()
    if unity_path.exists():
      return unity_path
    raise SystemExit(f"Unity executable not found: {unity_path}")

  installs = discover_unity_installations()
  if not installs:
    raise SystemExit(
        "Unity path not supplied and no installations detected. Use --unity-path or set UNITY_PATH."
    )

  if len(installs) == 1:
    chosen = installs[0]
    print(f"[build] Auto-selected Unity {chosen.version} at {chosen.path}")
    return chosen.path

  if not sys.stdin.isatty() or not sys.stdout.isatty():
    chosen = installs[0]
    print(
        "[build] Multiple Unity versions detected but no interactive terminal available; "
        f"defaulting to {chosen.version} ({chosen.path})."
    )
    return chosen.path

  selection = interactive_choice(
      [f"{install.version} — {install.path}" for install in installs],
      prompt="Select a Unity installation:"
  )
  chosen = installs[selection]
  print(f"[build] Selected Unity {chosen.version} at {chosen.path}")
  return chosen.path


def detect_platform_target() -> PlatformTarget:
  platform = sys.platform
  if platform.startswith("win"):
    return PLATFORM_TARGETS["windows"]
  if platform == "darwin":
    return PLATFORM_TARGETS["mac"]
  if platform.startswith("linux"):
    return PLATFORM_TARGETS["linux"]
  raise SystemExit(f"Unsupported platform: {platform}")


def prepare_output_directory() -> Path:
  timestamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
  destination = BUILD_ROOT / timestamp
  try:
    destination.mkdir(parents=True, exist_ok=False)
  except FileExistsError as exc:  # pragma: no cover - repeated runs in same second
    raise SystemExit(f"Output directory already exists: {destination}") from exc
  return destination


def build_player(unity_path: Path, build_name: str) -> Path:
  target = detect_platform_target()
  output_dir = prepare_output_directory()
  player_path = output_dir / f"{build_name}{target.extension}"
  log_path = output_dir / "unity.log"

  print(f"[build] Building {target.unity_name} to {player_path}")

  cmd = [
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
  run(cmd)

  print(f"[build] Player generated at {player_path}")
  print(f"[build] Unity log stored at {log_path}")
  return output_dir


def main(argv: Iterable[str]) -> None:
  args = parse_args(argv)
  unity_path = determine_unity_path(args.unity_path)
  build_player(unity_path, DEFAULT_BUILD_NAME)


if __name__ == "__main__":
  main(sys.argv[1:])
