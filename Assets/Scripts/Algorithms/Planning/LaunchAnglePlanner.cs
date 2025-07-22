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

// The launch angle planner interface defines methods for calculating optimal launch angles,
// time-to-intercept, and intercept positions.
public interface ILaunchAnglePlanner {
  // Calculate the optimal launch angle and time-to-target for a given input.
  //   input: Launch angle input parameters
  // Returns: Launch angle output with timing information
  public LaunchAngleOutput Plan(in LaunchAngleInput input);

  // Calculate launch parameters for a target position from a specific origin.
  // This method accounts for the distance and direction from origin to target.
  //   targetPosition: Target position
  //   originPosition: Interceptor origin position
  // Returns: Launch angle output with origin-relative calculations
  public LaunchAngleOutput Plan(Vector3 targetPosition, Vector3 originPosition) {
    Vector2 direction = ConvertToDirection(targetPosition, originPosition);
    float distance = Vector3.Distance(originPosition, targetPosition);
    var output = Plan(new LaunchAngleInput(distance: direction[0], altitude: direction[1]));
    return new LaunchAngleOutput(output.LaunchAngle, output.TimeToPosition);
  }

  // Get the intercept position for a target from a specific origin.
  // This accounts for the interceptor's starting position when calculating intercept geometry.
  //   targetPosition: Target position
  //   originPosition: Interceptor origin position
  // Returns: Calculated intercept position
  public Vector3 GetInterceptPosition(Vector3 targetPosition, Vector3 originPosition);

  // Convert from a 3D vector to a 2D direction that ignores the azimuth.
  // This method now supports origin-relative calculations.
  //   targetPosition: Target position
  //   originPosition: Origin position (default: Vector3.zero)
  // Returns: 2D direction vector (horizontal distance, altitude)
  protected static Vector2 ConvertToDirection(Vector3 targetPosition,
                                              Vector3 originPosition = default(Vector3)) {
    Vector3 relativePosition = targetPosition - originPosition;
    return new Vector2(Vector3.ProjectOnPlane(relativePosition, Vector3.up).magnitude,
                       Vector3.Project(relativePosition, Vector3.up).magnitude);
  }
}
