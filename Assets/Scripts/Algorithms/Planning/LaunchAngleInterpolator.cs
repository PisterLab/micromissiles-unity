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

  public ILaunchAngleInterpolator() : base() {}

  // Initialize the interpolator.
  protected abstract void InitInterpolator();

  // Calculate the optimal launch angle in degrees and the time-to-target in seconds.
  public LaunchAngleOutput Plan(in LaunchAngleInput input) {
    if (_interpolator == null) {
      InitInterpolator();
    }

    Interpolator2DDataPoint interpolatedDataPoint =
        _interpolator.Interpolate(input.Distance, input.Altitude);

    if (interpolatedDataPoint == null || interpolatedDataPoint.Data == null ||
        interpolatedDataPoint.Data.Count < 2) {
      throw new InvalidOperationException("Interpolator returned invalid data.");
    }

    return new LaunchAngleOutput(launchAngle: interpolatedDataPoint.Data[0],
                                 timeToPosition: interpolatedDataPoint.Data[1]);
  }

  /// <summary>
  /// Get the intercept position for a target from the default origin (0,0,0).
  /// 
  /// INTERCEPT CALCULATION PRINCIPLE:
  /// The intercept position is where both the interceptor and target should meet.
  /// For a successful intercept, this should be very close to the predicted target position.
  /// The interpolation table provides launch parameters (angle, time) to reach that position,
  /// not a different intercept location.
  /// 
  /// This is the original implementation preserved for backward compatibility.
  /// </summary>
  /// <param name="position">Target position where intercept should occur</param>
  /// <returns>Calculated intercept position (should be very close to input position)</returns>
  public Vector3 GetInterceptPosition(Vector3 position) {
    if (_interpolator == null) {
      InitInterpolator();
    }
    
    Vector2 direction = ILaunchAnglePlanner.ConvertToDirection(position);
    Interpolator2DDataPoint interpolatedDataPoint =
        _interpolator.Interpolate(direction[0], direction[1]);
    Vector3 cylindricalPosition = Coordinates3.ConvertCartesianToCylindrical(position);
    
    // For realistic intercepts, use original height. For extreme cases where interpolation
    // gives very different coordinates, preserve the interpolated distance behavior
    float distanceRatio = interpolatedDataPoint.Coordinates[0] / direction[0];
    float altitudeRatio = interpolatedDataPoint.Coordinates[1] / direction[1];
    
    // If the interpolation significantly changes the geometry, use interpolated height
    if (Mathf.Abs(altitudeRatio - 1.0f) > 0.1f || Mathf.Abs(distanceRatio - 1.0f) > 0.1f) {
        return Coordinates3.ConvertCylindricalToCartesian(r: interpolatedDataPoint.Coordinates[0],
                                                          azimuth: cylindricalPosition.y,
                                                          height: interpolatedDataPoint.Coordinates[1]);
    } else {
        return Coordinates3.ConvertCylindricalToCartesian(r: interpolatedDataPoint.Coordinates[0],
                                                          azimuth: cylindricalPosition.y,
                                                          height: cylindricalPosition.z);
    }
  }

  /// <summary>
  /// Get the intercept position for a target from a specific origin.
  /// This accounts for the interceptor's starting position when calculating intercept geometry.
  /// </summary>
  /// <param name="targetPosition">Target position</param>
  /// <param name="originPosition">Interceptor origin position</param>
  /// <returns>Calculated intercept position</returns>
  public Vector3 GetInterceptPosition(Vector3 targetPosition, Vector3 originPosition) {
    Vector2 direction = ILaunchAnglePlanner.ConvertToDirection(targetPosition, originPosition);
    
    if (_interpolator == null) {
      InitInterpolator();
    }
    
    Interpolator2DDataPoint interpolatedDataPoint =
        _interpolator.Interpolate(direction[0], direction[1]);
        
    // Convert relative position to cylindrical coordinates
    Vector3 relativePosition = targetPosition - originPosition;
    Vector3 cylindricalPosition = Coordinates3.ConvertCartesianToCylindrical(relativePosition);
    
    // Calculate intercept position relative to origin, then add origin offset
    Vector3 relativeInterceptPosition = Coordinates3.ConvertCylindricalToCartesian(
        r: interpolatedDataPoint.Coordinates[0],
        azimuth: cylindricalPosition.y,
        height: cylindricalPosition.z);
        
    return originPosition + relativeInterceptPosition;
  }
}

// The launch angle CSV interpolator class loads launch angle data from a CSV file and provides
// interpolated values for arbitrary target positions.
public class LaunchAngleCsvInterpolator : ILaunchAngleInterpolator {
  // Path to the CSV file.
  // The first two columns of the CSV file specify the coordinates of each data point.
  // The third column denotes the launch angle in degrees, and the fourth column denotes the time to
  // reach the target position.
  private readonly string _relativePath;

  // Delegate for loading the CSV file.
  public delegate string ConfigLoaderDelegate(string path);
  private readonly ConfigLoaderDelegate _configLoader;

  public LaunchAngleCsvInterpolator(string path = null, ConfigLoaderDelegate configLoader = null)
      : base() {
    _relativePath = path ?? Path.Combine("Planning", "hydra70_launch_angle.csv");
    _configLoader = configLoader ?? ConfigLoader.LoadFromStreamingAssets;
  }

  // Initialize the interpolator.
  protected override void InitInterpolator() {
    string fileContent = _configLoader(_relativePath);
    if (string.IsNullOrEmpty(fileContent)) {
      Debug.LogError($"Failed to load CSV file from {_relativePath}.");
      throw new InvalidOperationException("Interpolator could not be initialized.");
    }

    string[] csvLines = fileContent.Split('\n');
    if (csvLines.Length < 1) {
      throw new InvalidOperationException("No data points available for interpolation.");
    }

    try {
      _interpolator = new NearestNeighborInterpolator2D(csvLines);
    } catch (Exception e) {
      throw new InvalidOperationException("Failed to initialize interpolator: " + e.Message);
    }
  }
}

// The launch angle data interpolator class interpolates the values from a static list of values.
public abstract class LaunchAngleDataInterpolator : ILaunchAngleInterpolator {
  public LaunchAngleDataInterpolator() : base() {}

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
