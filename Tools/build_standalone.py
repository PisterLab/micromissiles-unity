#!/usr/bin/env python3
"""Local build helper mirroring the CI Unity build workflow.

This script bundles the high-level steps from .github/workflows/build.yaml:
1. Optionally builds the native plugins with Bazel.
2. Ensures the generated plugins are expanded into Assets/Plugins.
3. Invokes the Unity editor in batch mode for one or more target platforms.
4. Copies the Tools/ directory next to the generated player and packages the
   build output matching the GitHub Actions artefacts (zip for Windows, tar.gz
   elsewhere).

Example:
  python Tools/build_standalone.py --unity-path "C:/Program Files/Unity/Hub/Editor/2022.3.15f1/Editor/Unity.exe" \
      --target StandaloneWindows64 --target StandaloneLinux64

Unity CLI flags rely on the standard -build{Platform}Player switches, so no
project-specific executeMethod is required.
"""

from __future__ import annotations

import argparse
import os
import shutil
import subprocess
import sys
import tarfile
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
TOOLS_DIR = REPO_ROOT / "Tools"
PLUGINS_DIR = REPO_ROOT / "Assets" / "Plugins"


@dataclass(frozen=True)
class TargetSpec:
  build_flag: str
  extension: str  # File or bundle suffix added to the build name.
  archive_format: str  # Accepted by shutil.make_archive.


TARGETS = {
    "StandaloneWindows64": TargetSpec("-buildWindows64Player", ".exe", "zip"),
    "StandaloneLinux64": TargetSpec("-buildLinux64Player", ".x86_64", "gztar"),
    "StandaloneOSX": TargetSpec("-buildOSXUniversalPlayer", ".app", "gztar"),
}


def parse_args(argv: Iterable[str]) -> argparse.Namespace:
  parser = argparse.ArgumentParser(description="Build micromissiles-unity players via Unity CLI")
  parser.add_argument(
      "--unity-path",
      dest="unity_path",
      help=(
          "Path to the Unity editor executable. Falls back to UNITY_PATH env var or an "
          "auto-detected installation."
      ),
  )
  parser.add_argument(
      "--target",
      dest="targets",
      action="append",
      choices=sorted(TARGETS.keys()),
      help="Target platform to build. May be provided multiple times. Defaults to all platforms.",
  )
  parser.add_argument(
      "--output",
      dest="output_dir",
      default=str(REPO_ROOT / "build"),
      help="Directory where build outputs are written (default: %(default)s)",
  )
  parser.add_argument(
      "--build-name",
      dest="build_name",
      default="micromissiles",
      help="Base name for the produced player (default: %(default)s)",
  )
  parser.add_argument(
      "--skip-plugins",
      dest="skip_plugins",
      action="store_true",
      help="Skip Bazel plugin build step and reuse existing Assets/Plugins payload.",
  )
  parser.add_argument(
      "--skip-bazel",
      dest="skip_plugins",
      action="store_true",
      help="Alias for --skip-plugins.",
  )
  parser.add_argument(
      "--skip-unity-build",
      dest="skip_unity_build",
      action="store_true",
      help="Reuse an existing Unity build and only perform tooling copy + packaging.",
  )
  parser.add_argument(
      "--bazel-args",
      dest="bazel_args",
      nargs=argparse.REMAINDER,
      help="Extra arguments forwarded to `bazel build`."
  )
  parser.add_argument(
      "--log-dir",
      dest="log_dir",
      default=str(REPO_ROOT / "Logs"),
      help="Directory to store Unity batchmode logs (default: %(default)s)",
  )
  return parser.parse_args(list(argv))


def run(cmd: List[str], cwd: Path | None = None) -> None:
  print(f"[build] Running: {' '.join(cmd)}", flush=True)
  result = subprocess.run(cmd, cwd=str(cwd) if cwd else None)
  if result.returncode != 0:
    raise SystemExit(result.returncode)


def ensure_plugins(skip_plugins: bool, bazel_args: List[str] | None) -> None:
  if skip_plugins:
    print("[build] Skipping Bazel plugin build (per flag).")
    return

  cmd = ["bazel", "build", "-c", "opt", "//:plugins"]
  if bazel_args:
    cmd.extend(bazel_args)
  run(cmd, cwd=REPO_ROOT / "plugins")

  archive = REPO_ROOT / "plugins" / "bazel-bin" / "plugins.tar.gz"
  if not archive.exists():
    raise SystemExit(f"Expected plugins archive not found: {archive}")

  PLUGINS_DIR.mkdir(parents=True, exist_ok=True)
  with tarfile.open(archive, mode="r:gz") as tf:
    print(f"[build] Extracting {archive} -> {PLUGINS_DIR}")
    tf.extractall(path=PLUGINS_DIR)


@dataclass(frozen=True)
class UnityInstall:
  version: str
  path: Path


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

  # Also consider direct executable hints (e.g., /Applications/Unity/Unity.app)
  for hint in unity_executable_hints():
    if hint.exists():
      version = hint.parent.parent.name if hint.parent and hint.parent.parent else hint.name
      installs.setdefault(hint.resolve(), UnityInstall(version, hint.resolve()))

  # Sort versions descending to favour newest installs first.
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
  else:  # Linux and other POSIX platforms
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

  # Enable ANSI on Windows terminals when possible.
  if os.name == "nt":  # pragma: no cover - Windows specific
    os.system("")

  def render() -> None:
    for idx, option in enumerate(options):
      selector = "➜" if idx == index else " "
      line = f"{selector} {option}"
      if idx == index:
        line = f"\033[36m{line}\033[0m"
      print(line)
    # Move cursor up to redraw on next iteration
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
    # Move cursor to bottom of list before exiting render loop
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
        "Unity path not supplied and no installations detected. Use --unity-path or set "
        "UNITY_PATH."
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


def build_target(unity_path: Path | None, spec_key: str, build_name: str, output_root: Path,
                 log_dir: Path, skip_unity_build: bool) -> Path:
  spec = TARGETS[spec_key]
  target_dir = output_root / spec_key
  target_dir.mkdir(parents=True, exist_ok=True)

  player_path = target_dir / f"{build_name}{spec.extension}"
  log_dir.mkdir(parents=True, exist_ok=True)
  log_path = log_dir / f"unity-{spec_key}.log"

  if skip_unity_build:
    if not player_path.exists():
      raise SystemExit(
          f"Expected existing build at {player_path}, but it was not found."
      )
    print(f"[build] Reusing Unity build at {player_path}")
  else:
    if target_dir.exists():
      print(f"[build] Removing existing build directory: {target_dir}")
      shutil.rmtree(target_dir)
      target_dir.mkdir(parents=True, exist_ok=True)
    if unity_path is None:
      raise SystemExit("Unity path is required when not skipping Unity build.")

    cmd = [
        str(unity_path),
        "-batchmode",
        "-nographics",
        "-quit",
        "-projectPath",
        str(REPO_ROOT),
        spec.build_flag,
        str(player_path),
        "-logFile",
        str(log_path),
        "-buildTarget",
        spec_key,
    ]
    run(cmd)

  # Copy Tools directory alongside the build output for parity with CI artefacts.
  tools_dst = target_dir / "Tools"
  if tools_dst.exists():
    shutil.rmtree(tools_dst)
  print(f"[build] Copying Tools/ -> {tools_dst}")
  shutil.copytree(TOOLS_DIR, tools_dst)

  archive_base = output_root / f"build-{spec_key}"
  archive_path = make_archive(archive_base, spec.archive_format, target_dir)
  print(f"[build] Packaged artefact: {archive_path}")
  return archive_path


def make_archive(base: Path, fmt: str, source: Path) -> Path:
  archive_path = base.with_suffix(".zip" if fmt == "zip" else ".tar.gz")
  if archive_path.exists():
    archive_path.unlink()
  created = shutil.make_archive(base_name=str(base), format=fmt, root_dir=str(source))
  return Path(created)


def main(argv: Iterable[str]) -> None:
  args = parse_args(argv)
  skip_unity_build = args.skip_unity_build
  unity_path = None if skip_unity_build else determine_unity_path(args.unity_path)
  targets = args.targets or list(TARGETS.keys())

  ensure_plugins(args.skip_plugins, args.bazel_args)

  output_root = Path(args.output_dir).resolve()
  archive_paths = []
  for target in targets:
    archive_paths.append(
        build_target(unity_path, target, args.build_name, output_root, Path(args.log_dir),
                     skip_unity_build)
    )

  print("[build] Completed targets:")
  for path in archive_paths:
    print(f"  - {path}")


if __name__ == "__main__":
  main(sys.argv[1:])
