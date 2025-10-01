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


def collect_interceptor_launches(csv_path: Path):
    """Collect NEW_INTERCEPTOR event positions, separated by type.
    
    Returns:
        hydra_positions: List of (x, y, z) for Hydra 70 interceptors
        micro_positions: List of (x, y, z) for Micromissile interceptors
    """
    hydra_positions = []
    micro_positions = []
    
    with csv_path.open("r", newline="") as f:
        reader = csv.DictReader(f)
        for row in reader:
            evt = (row.get("Event") or "").strip().upper()
            if evt == "NEW_INTERCEPTOR":
                details = (row.get("Details") or "").strip()
                try:
                    x = float(row.get("PositionX", 0))
                    y = float(row.get("PositionY", 0))
                    z = float(row.get("PositionZ", 0))
                    
                    # Check interceptor type from Details field
                    if "Hydra 70" in details or "Hydra70" in details:
                        hydra_positions.append((x, y, z))
                    elif "Micromissile_Interceptor" in details:
                        micro_positions.append((x, y, z))
                except (ValueError, TypeError):
                    continue
    
    return hydra_positions, micro_positions


def aggregate_batch_hits(batch_dir: Path):
    """Aggregate all hit positions and interceptor launches from all runs in a batch."""
    all_hit_positions = []
    all_hydra_positions = []
    all_micro_positions = []
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
            hit_positions = collect_hit_positions(csv_path)
            all_hit_positions.extend(hit_positions)
            
            # Collect interceptor launch positions
            hydra_positions, micro_positions = collect_interceptor_launches(csv_path)
            all_hydra_positions.extend(hydra_positions)
            all_micro_positions.extend(micro_positions)
            
            # Also count hits and misses for statistics
            hits, misses = count_events(csv_path)
            hit_count += hits
            miss_count += misses
    
    return all_hit_positions, all_hydra_positions, all_micro_positions, run_count, hit_count, miss_count


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


def plot_hit_heatmap(hit_positions, hydra_positions, micro_positions, batch_id, bins=50):
    """Create a 2x2 grid: 3 separate heat maps + 1 combined scatter plot."""
    
    # Check if we have any data to plot
    has_data = bool(hit_positions or hydra_positions or micro_positions)
    if not has_data:
        print("No positions to plot.")
        return
    
    # Convert to numpy arrays (handle empty lists)
    hit_arr = np.array(hit_positions) if hit_positions else np.zeros((0, 3))
    hydra_arr = np.array(hydra_positions) if hydra_positions else np.zeros((0, 3))
    micro_arr = np.array(micro_positions) if micro_positions else np.zeros((0, 3))
    
    # Determine common bounds for all data
    all_x = []
    all_z = []
    if len(hit_arr) > 0:
        all_x.extend(hit_arr[:, 0])
        all_z.extend(hit_arr[:, 2])
    if len(hydra_arr) > 0:
        all_x.extend(hydra_arr[:, 0])
        all_z.extend(hydra_arr[:, 2])
    if len(micro_arr) > 0:
        all_x.extend(micro_arr[:, 0])
        all_z.extend(micro_arr[:, 2])
    
    if not all_x or not all_z:
        print("Insufficient data to plot.")
        return
    
    # Create common bin edges
    xmin, xmax = min(all_x), max(all_x)
    zmin, zmax = min(all_z), max(all_z)
    pad_x = (xmax - xmin) * 0.05 if xmax > xmin else 1.0
    pad_z = (zmax - zmin) * 0.05 if zmax > zmin else 1.0
    xedges = np.linspace(xmin - pad_x, xmax + pad_x, bins + 1)
    zedges = np.linspace(zmin - pad_z, zmax + pad_z, bins + 1)
    extent = [xedges[0], xedges[-1], zedges[0], zedges[-1]]
    
    # Create histograms for each type
    h_hits = np.zeros((bins, bins))
    h_hydra = np.zeros((bins, bins))
    h_micro = np.zeros((bins, bins))
    
    if len(hit_arr) > 0:
        h_hits, _, _ = np.histogram2d(hit_arr[:, 0], hit_arr[:, 2], bins=[xedges, zedges])
        h_hits = h_hits.T
    
    if len(hydra_arr) > 0:
        h_hydra, _, _ = np.histogram2d(hydra_arr[:, 0], hydra_arr[:, 2], bins=[xedges, zedges])
        h_hydra = h_hydra.T
    
    if len(micro_arr) > 0:
        h_micro, _, _ = np.histogram2d(micro_arr[:, 0], micro_arr[:, 2], bins=[xedges, zedges])
        h_micro = h_micro.T
    
    # Create simple 2x2 subplot grid
    fig, axes = plt.subplots(2, 2, figsize=(14, 11))
    ax1, ax2, ax3, ax4 = axes.flatten()
    
    # Top-left: Hit heat map
    if np.any(h_hits > 0):
        im1 = ax1.imshow(h_hits, extent=extent, origin='lower', cmap='hot',
                        aspect='auto', interpolation='bilinear')
        plt.colorbar(im1, ax=ax1)
    ax1.set_xlabel('X (m)')
    ax1.set_ylabel('Z (m)')
    ax1.set_title(f'Intercepts (n={len(hit_positions)})')
    ax1.grid(True, alpha=0.3)
    
    # Top-right: Hydra 70 heat map
    if np.any(h_hydra > 0):
        im2 = ax2.imshow(h_hydra, extent=extent, origin='lower', cmap='Greens',
                        aspect='auto', interpolation='bilinear')
        plt.colorbar(im2, ax=ax2)
    ax2.set_xlabel('X (m)')
    ax2.set_ylabel('Z (m)')
    ax2.set_title(f'Hydra 70 Launches (n={len(hydra_positions)})')
    ax2.grid(True, alpha=0.3)
    
    # Bottom-left: Micromissile heat map
    if np.any(h_micro > 0):
        im3 = ax3.imshow(h_micro, extent=extent, origin='lower', cmap='viridis',
                        aspect='auto', interpolation='bilinear')
        plt.colorbar(im3, ax=ax3)
    ax3.set_xlabel('X (m)')
    ax3.set_ylabel('Z (m)')
    ax3.set_title(f'Micromissile Dispense Points (n={len(micro_positions)})')
    ax3.grid(True, alpha=0.3)
    
    # Bottom-right: Combined scatter plot
    if len(micro_arr) > 0:
        ax4.scatter(micro_arr[:, 0], micro_arr[:, 2], c='blue', alpha=0.3, s=5, label='Micromissile Dispense Points')
    if len(hydra_arr) > 0:
        ax4.scatter(hydra_arr[:, 0], hydra_arr[:, 2], c='cyan', alpha=0.6, s=20, marker='s', label='Hydra 70 Launches')
    if len(hit_arr) > 0:
        ax4.scatter(hit_arr[:, 0], hit_arr[:, 2], c='red', alpha=0.7, s=30, marker='*', label='Intercepts')
    ax4.set_xlabel('X (m)')
    ax4.set_ylabel('Z (m)')
    ax4.set_title('Combined')
    ax4.set_xlim(extent[0], extent[1])
    ax4.set_ylim(extent[2], extent[3])
    ax4.grid(True, alpha=0.3)
    ax4.legend()
    
    plt.suptitle(f'{batch_id}: {len(hit_positions)} hits, {len(hydra_positions)} Hydra, {len(micro_positions)} Micro', 
                 fontsize=11)
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
        
        hit_positions, hydra_positions, micro_positions, run_count, hit_count, miss_count = aggregate_batch_hits(batch_dir)
        
        if run_count == 0:
            print(f"No run directories found in {batch_dir}")
            return 0
        
        total = hit_count + miss_count
        rate = (hit_count / total) if total else 0.0
        
        print(f"Batch: {args.batch_id}")
        print(f"  Runs analyzed        : {run_count}")
        print(f"  Interceptor hits     : {hit_count}")
        print(f"  Interceptor misses   : {miss_count}")
        print(f"  Hit rate             : {rate:.2%}")
        print(f"  Hit positions        : {len(hit_positions)}")
        print(f"  Hydra 70 launches    : {len(hydra_positions)}")
        print(f"  Micromissile launches: {len(micro_positions)}")
        
        # Plot heat map
        has_data = bool(hit_positions or hydra_positions or micro_positions)
        if not args.no_plot and has_data:
            print(f"\nGenerating composite heat map with {args.bins} bins...")
            plot_hit_heatmap(hit_positions, hydra_positions, micro_positions, args.batch_id, bins=args.bins)
        elif not has_data:
            print("\nNo positions found to plot.")
        
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
