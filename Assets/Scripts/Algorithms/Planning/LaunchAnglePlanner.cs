using UnityEngine;

// Launch angle input.
public struct LaunchAngleInput {
  // Horizontal distance in meters.
  public float Distance { get; }

  // Altitude in meters.
  public float Altitude { get; }

  public LaunchAngleInput(float distance, float altitude) {
    Distance = distance;
    Altitude = altitude;
  }
}

// Launch angle output.
public struct LaunchAngleOutput {
  // Launch angle in degrees.
  public float LaunchAngle { get; }

  // Time to reach the target position in seconds.
  public float TimeToPosition { get; }

  public LaunchAngleOutput(float launchAngle, float timeToPosition) {
    LaunchAngle = launchAngle;
    TimeToPosition = timeToPosition;
  }
}

// Launch angle data point.
public struct LaunchAngleDataPoint {
  // Launch angle input.
  public LaunchAngleInput Input { get; }

  // Launch angle output.
  public LaunchAngleOutput Output { get; }

  public LaunchAngleDataPoint(in LaunchAngleInput input, in LaunchAngleOutput output) {
    Input = input;
    Output = output;
  }
}

// The launch angle planner class is an interface for a planner that outputs the optimal launch
// angle and the time-to-target.
public interface ILaunchAnglePlanner {
  // Calculate the optimal launch angle in degrees and the time-to-target in seconds.
  public LaunchAngleOutput Plan(in LaunchAngleInput input);
  public LaunchAngleOutput Plan(float distance, float altitude) {
    return Plan(new LaunchAngleInput(distance, altitude));
  }
  public LaunchAngleOutput Plan(Vector3 position) {
    Vector2 direction = ConvertToDirection(position);
    return Plan(distance: direction[0], altitude: direction[1]);
  }

  // Get the intercept position.
  public LaunchAngleInput GetInterceptPosition(in LaunchAngleInput input);
  public LaunchAngleInput GetInterceptPosition(float distance, float altitude) {
    return GetInterceptPosition(new LaunchAngleInput(distance, altitude));
  }
  public LaunchAngleInput GetInterceptPosition(Vector3 position) {
    Vector2 direction = ConvertToDirection(position);
    return GetInterceptPosition(distance: direction[0], altitude: direction[1]);
  }

  // Convert from a 3D vector to a 2D direction that ignores the azimuth.
  public static Vector2 ConvertToDirection(Vector3 position) {
    return new Vector2(Vector3.ProjectOnPlane(position, Vector3.up).magnitude,
                       Vector3.Project(position, Vector3.up).magnitude);
  }
}
