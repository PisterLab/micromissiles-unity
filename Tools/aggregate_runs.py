#!/usr/bin/env python3
import argparse
import csv
import os
import platform
from collections import defaultdict
from pathlib import Path
import numpy as np
import matplotlib.pyplot as plt


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


def collect_hit_positions(csv_path: Path):
    """Collect all hit event positions from an events CSV file."""
    positions = []
    with csv_path.open("r", newline="") as f:
        reader = csv.DictReader(f)
        for row in reader:
            evt = (row.get("Event") or "").strip().upper()
            if evt in {"INTERCEPTOR_HIT", "HIT"}:
                try:
                    x = float(row.get("PositionX", 0))
                    y = float(row.get("PositionY", 0))
                    z = float(row.get("PositionZ", 0))
                    positions.append((x, y, z))
                except (ValueError, TypeError):
                    continue
    return positions


def aggregate_batch_hits(batch_dir: Path):
    """Aggregate all hit positions from all runs in a batch."""
    all_positions = []
    run_count = 0
    hit_count = 0
    miss_count = 0
    
    # Find all run directories (run_XXXX_seed_XXX pattern)
    for run_dir in sorted(batch_dir.glob("run_*_seed_*")):
        if not run_dir.is_dir():
            continue
            
        # Find sim_events CSV files in this run
        event_files = list(run_dir.glob("sim_events_*.csv"))
        if not event_files:
            continue
            
        run_count += 1
        for csv_path in event_files:
            # Collect hit positions
            positions = collect_hit_positions(csv_path)
            all_positions.extend(positions)
            
            # Also count hits and misses for statistics
            hits, misses = count_events(csv_path)
            hit_count += hits
            miss_count += misses
    
    return all_positions, run_count, hit_count, miss_count


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


def plot_hit_heatmap(positions, batch_id, bins=50):
    """Create a 2D heat map of hit positions (X-Z plane)."""
    if not positions:
        print("No hit positions to plot.")
        return
    
    positions = np.array(positions)
    x = positions[:, 0]
    z = positions[:, 2]  # Use Z for the Y-axis in the plot
    
    fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(16, 6))
    
    # 2D Histogram (Heat map)
    h, xedges, yedges = np.histogram2d(x, z, bins=bins)
    h = h.T  # Transpose for correct orientation
    
    # Plot heat map
    extent = [xedges[0], xedges[-1], yedges[0], yedges[-1]]
    im = ax1.imshow(h, extent=extent, origin='lower', cmap='hot', 
                    aspect='auto', interpolation='bilinear')
    ax1.set_xlabel('X Position (m)', fontsize=12)
    ax1.set_ylabel('Z Position (m)', fontsize=12)
    ax1.set_title(f'Hit Heat Map (XZ Plane) - {batch_id}\n{len(positions)} total hits', 
                  fontsize=14, fontweight='bold')
    ax1.grid(True, alpha=0.3)
    
    cbar = plt.colorbar(im, ax=ax1)
    cbar.set_label('Number of Hits', fontsize=11)
    
    # Scatter plot with altitude color-coding
    y = positions[:, 1]
    scatter = ax2.scatter(x, z, c=y, cmap='viridis', alpha=0.6, s=20)
    ax2.set_xlabel('X Position (m)', fontsize=12)
    ax2.set_ylabel('Z Position (m)', fontsize=12)
    ax2.set_title(f'Hit Positions (colored by altitude)\n{batch_id}', 
                  fontsize=14, fontweight='bold')
    ax2.grid(True, alpha=0.3)
    
    cbar2 = plt.colorbar(scatter, ax=ax2)
    cbar2.set_label('Y Position (Altitude, m)', fontsize=11)
    
    plt.tight_layout()
    plt.show()


def main():
    parser = argparse.ArgumentParser(
        description="Aggregate interceptor hit/miss counts and visualize hit heat maps",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # Aggregate all batches
  python aggregate_runs.py
  
  # Aggregate and visualize a specific batch
  python aggregate_runs.py --batch-id sample_quad_repl
  
  # Aggregate a specific batch with custom bin size for heat map
  python aggregate_runs.py --batch-id sample_quad_repl --bins 100
        """
    )
    parser.add_argument("path", nargs="?", 
                       help="Logs directory (defaults to persistent Telemetry/Logs)")
    parser.add_argument("--batch-id", "-b", 
                       help="Specific batch ID to analyze and visualize")
    parser.add_argument("--bins", type=int, default=50,
                       help="Number of bins for heat map (default: 50)")
    parser.add_argument("--no-plot", action="store_true",
                       help="Skip plotting the heat map")
    args = parser.parse_args()

    base = Path(args.path) if args.path else get_logs_directory()
    if not base.exists():
        print(f"Logs directory not found: {base}")
        return 1

    # If batch ID is specified, analyze that specific batch
    if args.batch_id:
        batch_dir = base / args.batch_id
        if not batch_dir.exists():
            print(f"Batch directory not found: {batch_dir}")
            return 1
        
        print(f"Analyzing batch: {args.batch_id}")
        print(f"Batch directory: {batch_dir}\n")
        
        positions, run_count, hit_count, miss_count = aggregate_batch_hits(batch_dir)
        
        if run_count == 0:
            print(f"No run directories found in {batch_dir}")
            return 0
        
        total = hit_count + miss_count
        rate = (hit_count / total) if total else 0.0
        
        print(f"Batch: {args.batch_id}")
        print(f"  Runs analyzed      : {run_count}")
        print(f"  Interceptor hits   : {hit_count}")
        print(f"  Interceptor misses : {miss_count}")
        print(f"  Hit rate           : {rate:.2%}")
        print(f"  Hit positions      : {len(positions)}")
        
        # Plot heat map
        if not args.no_plot and positions:
            print(f"\nGenerating heat map with {args.bins} bins...")
            plot_hit_heatmap(positions, args.batch_id, bins=args.bins)
        elif not positions:
            print("\nNo hit positions found to plot.")
        
        return 0

    # Otherwise, aggregate all batches
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
        print(f"  Runs analyzed      : {runs}")
        print(f"  Interceptor hits   : {hits}")
        print(f"  Interceptor misses : {misses}")
        print(f"  Hit rate           : {rate:.2%}\n")
        overall_runs += runs
        overall_hits += hits
        overall_misses += misses

    overall_total = overall_hits + overall_misses
    print("Overall Summary")
    print(f"  Runs analyzed      : {overall_runs}")
    print(f"  Interceptor hits   : {overall_hits}")
    print(f"  Interceptor misses : {overall_misses}")
    if overall_total:
        print(f"  Hit rate           : {overall_hits / overall_total:.2%}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
