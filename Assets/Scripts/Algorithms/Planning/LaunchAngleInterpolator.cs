using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Launch angle output struct.
public struct LaunchAngleOutput {
  // Launch angle in degrees.
  public readonly float LaunchAngle;

  // Time to reach the target position in seconds.
  public readonly float TimeToPosition;

  public LaunchAngleOutput(float launchAngle, float timeToPosition) {
    LaunchAngle = launchAngle;
    TimeToPosition = timeToPosition;
  }
}

// The launch angle interpolator class determines the optimal launch angle given the horizontal
// distance and altitude of the target.
public class LaunchAngleInterpolator {
  // Path to the CSV file.
  // The first two columns of the CSV file specify the coordinates of each data point.
  // The third column denotes the launch angle in degrees, and the fourth column denotes the time to
  // reach the target position.
  public readonly string RelativePath = Path.Combine("Planning", "hydra70_launch_angle.csv");

  // Launch angle data interpolator.
  private readonly Interpolator2D _interpolator;

  public LaunchAngleInterpolator() {
    string fileContent = ConfigLoader.LoadFromStreamingAssets(RelativePath);
    if (string.IsNullOrEmpty(fileContent)) {
      Debug.LogError($"Failed to load CSV file from {RelativePath}.");
      throw new InvalidOperationException("Interpolator could not be initialized.");
    }

    string[] csvLines = fileContent.Split('\n');
    _interpolator = new Interpolator2D(csvLines);
  }

  // Calculate the optimal launch angle in degrees and the time to reach the target position in
  // seconds.
  public LaunchAngleOutput CalculateLaunchAngle(float deltaX, float deltaY) {
    List<float> data = _interpolator.Interpolate(deltaX, deltaY);
    if (data == null || data.Count < 2) {
      throw new InvalidOperationException("Interpolator returned invalid data.");
    }
    return new LaunchAngleOutput(launchAngle: data[0], timeToPosition: data[1]);
  }
}
