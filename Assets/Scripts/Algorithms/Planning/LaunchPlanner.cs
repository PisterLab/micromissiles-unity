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

  // Plan the launch.
  public abstract LaunchPlan Plan();
}
