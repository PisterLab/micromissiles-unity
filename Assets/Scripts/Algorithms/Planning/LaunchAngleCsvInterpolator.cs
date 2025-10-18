using System;
using System.IO;
using System.Linq;
using UnityEngine;

// The launch angle CSV interpolator loads launch angle data from a CSV file and provides
// interpolated values for arbitrary target positions.
public class LaunchAngleCsvInterpolator : LaunchAngleInterpolatorBase {
  // Path to the CSV file.
  // The first two columns of the CSV file specify the coordinates of each data point.
  // The third column denotes the launch angle in degrees, and the fourth column denotes the time to
  // reach the target position.
  private readonly string _relativePath;

  // Delegate for loading the CSV file.
  public delegate string ConfigLoaderDelegate(string path);
  private readonly ConfigLoaderDelegate _configLoader;

  public LaunchAngleCsvInterpolator(IAgent agent)
      : this(agent, path: Path.Combine("Planning", "hydra70_launch_angle.csv"),
             configLoader: ConfigLoader.LoadFromStreamingAssets) {}
  public LaunchAngleCsvInterpolator(IAgent agent, string path, ConfigLoaderDelegate configLoader)
      : base(agent) {
    _relativePath = path;
    _configLoader = configLoader;
  }

  // Initialize the interpolator.
  protected override void InitInterpolator() {
    string fileContent = _configLoader(_relativePath);
    if (string.IsNullOrEmpty(fileContent)) {
      Debug.LogError($"Failed to load CSV file from {_relativePath}.");
      throw new InvalidOperationException("Interpolator could not be initialized.");
    }

    string[] csvLines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
    string[] dataLines = csvLines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
    if (dataLines.Length < 1) {
      throw new InvalidOperationException("No data points available for interpolation.");
    }

    try {
      _interpolator = new NearestNeighborInterpolator2D(dataLines);
    } catch (Exception e) {
      throw new InvalidOperationException("Failed to initialize interpolator: " + e.Message);
    }
  }
}
