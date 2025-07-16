using UnityEngine;

// Launch planner output.
public class LaunchPlan {
  // No-launch launch plan.
  public static LaunchPlan NoLaunch = new LaunchPlan();

  // Whether the interceptor should be launched.
  public bool ShouldLaunch { get; }

  // Launch angle in degrees measured from the horizon.
  public float LaunchAngle { get; }

  // Intercept position.
  public Vector3 InterceptPosition { get; }

  public LaunchPlan() {
    ShouldLaunch = false;
  }
  public LaunchPlan(float launchAngle, Vector3 interceptPosition, bool shouldLaunch = true) {
    LaunchAngle = launchAngle;
    InterceptPosition = interceptPosition;
    ShouldLaunch = shouldLaunch;
  }

  /// <summary>
  /// Gets the normalized launch vector from the interceptor origin.
  /// This vector represents the direction the interceptor should be launched.
  /// </summary>
  /// <param name="originPosition">Position of the interceptor origin (default: Vector3.zero for backward compatibility)</param>
  /// <returns>Normalized launch direction vector</returns>
  public Vector3 GetNormalizedLaunchVector(Vector3 originPosition = default(Vector3)) {
    // Calculate direction from origin to intercept position
    Vector3 targetDirection = InterceptPosition - originPosition;
    Vector3 interceptDirection = Coordinates3.ConvertCartesianToSpherical(targetDirection);
    return Coordinates3.ConvertSphericalToCartesian(r: 1, azimuth: interceptDirection[1],
                                                    elevation: LaunchAngle);
  }

  /// <summary>
  /// Determines whether the specified LaunchPlan is equal to the current LaunchPlan.
  /// </summary>
  public override bool Equals(object obj) {
    if (obj == null || GetType() != obj.GetType()) {
      return false;
    }
    
    LaunchPlan other = (LaunchPlan)obj;
    
    // Handle NoLaunch comparison specially - both must have ShouldLaunch == false
    if (!ShouldLaunch && !other.ShouldLaunch) {
      return true;
    }
    
    // If only one should launch, they're not equal
    if (ShouldLaunch != other.ShouldLaunch) {
      return false;
    }
    
    // Both should launch - compare angle and position
    return Mathf.Approximately(LaunchAngle, other.LaunchAngle) &&
           Vector3.Distance(InterceptPosition, other.InterceptPosition) < 0.001f;
  }

  /// <summary>
  /// Returns a hash code for this LaunchPlan.
  /// </summary>
  public override int GetHashCode() {
    return ShouldLaunch.GetHashCode() ^ LaunchAngle.GetHashCode() ^ InterceptPosition.GetHashCode();
  }

  /// <summary>
  /// Returns a string representation of this LaunchPlan.
  /// </summary>
  public override string ToString() {
    if (!ShouldLaunch) {
      return "LaunchPlan: NoLaunch";
    }
    return $"LaunchPlan: Angle={LaunchAngle:F1}°, Position=({InterceptPosition.x:F1}, {InterceptPosition.y:F1}, {InterceptPosition.z:F1})";
  }
}

// The launch planner class is an interface for planning when and where to launch an interceptor to
// intercept a target.
public abstract class ILaunchPlanner {
  // Launch angle planner.
  protected ILaunchAnglePlanner _launchAnglePlanner;

  // Agent trajectory predictor.
  protected IPredictor _predictor;

  public ILaunchPlanner(ILaunchAnglePlanner launchAnglePlanner, IPredictor predictor) {
    _launchAnglePlanner = launchAnglePlanner;
    _predictor = predictor;
  }

  /// <summary>
  /// Plan the launch from the default origin (0,0,0).
  /// Maintained for backward compatibility.
  /// </summary>
  /// <returns>Launch plan with timing and angle information</returns>
  public abstract LaunchPlan Plan();

  /// <summary>
  /// Plan the launch from a specific interceptor origin.
  /// This method accounts for the interceptor's starting position when calculating
  /// intercept trajectories and launch angles.
  /// </summary>
  /// <param name="origin">Interceptor origin configuration</param>
  /// <param name="currentTime">Current simulation time for moving origins</param>
  /// <returns>Launch plan with timing and angle information</returns>
  public abstract LaunchPlan Plan(InterceptorOriginConfig origin, float currentTime);
}
