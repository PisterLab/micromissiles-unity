using UnityEngine;

// The launch planner class is an interface for planning when and where to launch an interceptor to
// intercept a target.
public abstract class ILaunchPlannerLegacy {
  // Launch angle planner.
  protected ILaunchAnglePlanner _launchAnglePlanner;

  // Agent trajectory predictor.
  protected IPredictorLegacy _predictor;

  public ILaunchPlannerLegacy(ILaunchAnglePlanner launchAnglePlanner, IPredictorLegacy predictor) {
    _launchAnglePlanner = launchAnglePlanner;
    _predictor = predictor;
  }

  // Plan the launch.
  public abstract LaunchPlan Plan();
}
