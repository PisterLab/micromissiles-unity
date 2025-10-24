// Base implementation of a launch planner.
public abstract class LaunchPlannerBase : ILaunchPlanner {
  public ILaunchAnglePlanner LaunchAnglePlanner { get; set; }
  public IPredictor Predictor { get; set; }

  public LaunchPlannerBase(ILaunchAnglePlanner launchAnglePlanner, IPredictor predictor) {
    LaunchAnglePlanner = launchAnglePlanner;
    Predictor = predictor;
  }

  // Plan the launch by finding the convergence point between the launch angle planner and the
  // predictor.
  public abstract LaunchPlan Plan();
}
