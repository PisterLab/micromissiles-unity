// Interface for a launch planner.
//
// The launch planner finds the convergence point between the launch angle planner and the
// predictor, so that the interceptor reaches the same position as the target at the same time.
public interface ILaunchPlanner {
  ILaunchAnglePlanner LaunchAnglePlanner { get; set; }
  IPredictor Predictor { get; set; }

  LaunchPlan Plan();
}
