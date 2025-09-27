#!/usr/bin/env python3
import argparse
import csv
import os
import platform
from collections import defaultdict
from pathlib import Path


COMPANY_NAME = "BAMLAB"
PRODUCT_NAME = "micromissiles"


def get_logs_directory() -> Path:
    system = platform.system()
    if system == "Windows":
        base = Path(os.path.expandvars(r"%USERPROFILE%\AppData\LocalLow"))
        return base / COMPANY_NAME / PRODUCT_NAME / "Telemetry" / "Logs"
    if system == "Darwin":
        base = Path.home() / "Library" / "Application Support"
        return base / COMPANY_NAME / PRODUCT_NAME / "Telemetry" / "Logs"
    if system == "Linux":
        base = Path.home() / ".config" / "unity3d"
        return base / COMPANY_NAME / PRODUCT_NAME / "Telemetry" / "Logs"
    raise NotImplementedError(f"Unsupported platform: {system}")


def count_events(csv_path: Path):
    hits = 0
    misses = 0
    with csv_path.open("r", newline="") as f:
        reader = csv.DictReader(f)
        for row in reader:
            evt = (row.get("Event") or "").strip().upper()
            if evt in {"INTERCEPTOR_HIT", "HIT"}:
                hits += 1
            elif evt in {"INTERCEPTOR_MISS", "MISS"}:
                misses += 1
    return hits, misses


def aggregate_all(logs_root: Path):
    batch_stats = defaultdict(lambda: {"runs": 0, "hits": 0, "misses": 0})
    for csv_path in logs_root.rglob("sim_events_*.csv"):
        run_dir = csv_path.parent
        meta_path = run_dir / "run_meta.json"
        if not meta_path.exists():
            continue
        parent = run_dir.parent
        batch_id = parent.name if parent != logs_root else "standalone"
        hits, misses = count_events(csv_path)
        stats = batch_stats[batch_id]
        stats["runs"] += 1
        stats["hits"] += hits
        stats["misses"] += misses
    return batch_stats


def main():
    parser = argparse.ArgumentParser(description="Aggregate interceptor hit/miss counts")
    parser.add_argument("path", nargs="?", help="Logs directory (defaults to persistent Telemetry/Logs)")
    args = parser.parse_args()

    base = Path(args.path) if args.path else get_logs_directory()
    if not base.exists():
        print(f"Logs directory not found: {base}")
        return 1

    stats = aggregate_all(base)
    if not stats:
        print(f"No sim_events_*.csv files found under {base}")
        return 0

    overall_runs = overall_hits = overall_misses = 0
    print(f"Aggregating runs under {base}\n")
    for batch_id, data in sorted(stats.items()):
        runs = data["runs"]
        hits = data["hits"]
        misses = data["misses"]
        total = hits + misses
        rate = (hits / total) if total else 0.0
        print(f"Batch: {batch_id}")
        print(f"  Runs analyzed : {runs}")
        print(f"  Interceptor hits : {hits}")
        print(f"  Interceptor misses : {misses}")
        print(f"  Hit rate : {rate:.2%}\n")
        overall_runs += runs
        overall_hits += hits
        overall_misses += misses

    overall_total = overall_hits + overall_misses
    print("Overall Summary")
    print(f"  Runs analyzed : {overall_runs}")
    print(f"  Interceptor hits : {overall_hits}")
    print(f"  Interceptor misses : {overall_misses}")
    if overall_total:
        print(f"  Hit rate : {overall_hits / overall_total:.2%}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
