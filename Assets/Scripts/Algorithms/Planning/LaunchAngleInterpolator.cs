using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// The launch angle interpolator class determines the optimal launch angle and the time-to-target
// given the horizontal distance and altitude of the target.
public abstract class ILaunchAngleInterpolator : ILaunchAnglePlanner {
  // Launch angle data interpolator.
  protected IInterpolator2D _interpolator;

  public ILaunchAngleInterpolator() {
    // Remove InitInterpolator from constructor
  }

  // Initialize the interpolator.
  protected abstract void InitInterpolator();

  // Calculate the optimal launch angle in degrees and the time-to-target in seconds.
  public LaunchAngleOutput Plan(in LaunchAngleInput input) {
    if (_interpolator == null) {
      throw new InvalidOperationException("Interpolator not properly initialized.");
    }
    
    Interpolator2DDataPoint interpolatedDataPoint =
        _interpolator.Interpolate(input.Distance, input.Altitude);
        
    if (interpolatedDataPoint == null || interpolatedDataPoint.Data == null || interpolatedDataPoint.Data.Count < 2) {
      throw new InvalidOperationException("Interpolator returned invalid data.");
    }
    
    return new LaunchAngleOutput(
      launchAngle: interpolatedDataPoint.Data[0],
      timeToPosition: interpolatedDataPoint.Data[1]
    );
  }

  // Get the intercept position.
  public LaunchAngleInput GetInterceptPosition(in LaunchAngleInput input) {
    Interpolator2DDataPoint interpolatedDataPoint =
        _interpolator.Interpolate(input.Distance, input.Altitude);
    return new LaunchAngleInput(interpolatedDataPoint.Coordinates[0],
                                altitude: interpolatedDataPoint.Coordinates[1]);
  }
}

// The launch angle CSV interpolator class interpolates the values from a CSV file.
public class LaunchAngleCsvInterpolator : ILaunchAngleInterpolator {
  // Path to the CSV file.
  // The first two columns of the CSV file specify the coordinates of each data point.
  // The third column denotes the launch angle in degrees, and the fourth column denotes the time to
  // reach the target position.
  public readonly string RelativePath;

  // Delegate for loading configuration
  public delegate string ConfigLoaderDelegate(string path);

  private readonly ConfigLoaderDelegate _configLoader;

  // Initializes a new instance of the LaunchAngleInterpolator.
  // The interpolator loads launch angle data from a CSV file and provides interpolated values
  // for arbitrary target positions.
  public LaunchAngleCsvInterpolator(string path = null, ConfigLoaderDelegate configLoader = null) {
    RelativePath = path ?? Path.Combine("Planning", "hydra70_launch_angle.csv");
    _configLoader = configLoader ?? ConfigLoader.LoadFromStreamingAssets;
    InitInterpolator(); // Move initialization here after fields are set
  }

  // Initialize the interpolator.
  protected override void InitInterpolator() {
    string fileContent = _configLoader(RelativePath);
    if (string.IsNullOrEmpty(fileContent)) {
      Debug.LogError($"Failed to load CSV file from {RelativePath}.");
      throw new InvalidOperationException("Interpolator could not be initialized.");
    }

    string[] csvLines = fileContent.Split('\n');
    if (csvLines.Length < 1) {
      throw new InvalidOperationException("No data points available for interpolation.");
    }

    try {
      _interpolator = new NearestNeighborInterpolator2D(csvLines);
    }
    catch (Exception e) {
      throw new InvalidOperationException("Failed to initialize interpolator: " + e.Message);
    }
  }
}

// The launch angle data interpolator class interpolates the values from a static list of values.
public abstract class LaunchAngleDataInterpolator : ILaunchAngleInterpolator {
  public LaunchAngleDataInterpolator() : base() {
    InitInterpolator(); // Move initialization here
  }

  // Initialize the interpolator.
  protected override void InitInterpolator() {
    List<Interpolator2DDataPoint> interpolatorDataPoints = new List<Interpolator2DDataPoint>();
    foreach (var dataPoint in GenerateData()) {
      Interpolator2DDataPoint interpolatorDataPoint = new Interpolator2DDataPoint(
          new Vector2(dataPoint.Input.Distance, dataPoint.Input.Altitude),
          new List<float> { dataPoint.Output.LaunchAngle, dataPoint.Output.TimeToPosition });
      interpolatorDataPoints.Add(interpolatorDataPoint);
    }
    _interpolator = new NearestNeighborInterpolator2D(interpolatorDataPoints);
  }

  // Generate the list of launch angle data points to interpolate.
  protected abstract List<LaunchAngleDataPoint> GenerateData();
}
