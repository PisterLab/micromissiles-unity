using UnityEngine;

// The iterative launch planner class is a launch planner that performs an iterative process to
// determine the intercept point. The algorithm continuously performs the following 2-step iterative
// process:
//  1. Time-to-intercept estimation: The algorithm determines the time it takes the interceptor to
//  reach the target at the predicted intercept position, which is initialized to the target's
//  current position.
//  2. Intercept position estimation: The algorithm predicts the target position at the estimated
//  time-to-intercept.
public class IterativeLaunchPlanner : ILaunchPlanner {
  // Maximum number of iterations before declaring failure.
  private const int MaxNumIterations = 10;

  // Convergence threshold for the difference vector magnitude.
  private const float ConvergenceThreshold = 1e-3f;

  // Maximum intercept position threshold to declare convergence.
  private const float InterceptPositionThreshold = 1000;

  public IterativeLaunchPlanner(ILaunchAnglePlanner launchAnglePlanner, IPredictor predictor)
      : base(launchAnglePlanner, predictor) {}

  // Plan the launch.
  public override LaunchPlan Plan() {
    PredictorState initialState = _predictor.Predict(time: 0);
    Vector3 targetPosition = initialState.Position;

    LaunchAngleOutput launchAngleOutput = new LaunchAngleOutput();
    Vector3 interceptPosition = new Vector3();
    LaunchPlan launchPlan = new LaunchPlan();
    for (int i = 0; i < MaxNumIterations; ++i) {
      // Estimate the time-to-intercept.
      launchAngleOutput = _launchAnglePlanner.Plan(targetPosition);
      float timeToIntercept = launchAngleOutput.TimeToPosition;

      // Estimate the target position.
      PredictorState predictedState = _predictor.Predict(timeToIntercept);
      targetPosition = predictedState.Position;

      // Check whether the intercept direction has changed, in which case the algorithm has
      // converged.
      Vector3 newInterceptPosition = _launchAnglePlanner.GetInterceptPosition(targetPosition);
      if ((interceptPosition - newInterceptPosition).magnitude < ConvergenceThreshold) {
        interceptPosition = newInterceptPosition;
        break;
      }
      interceptPosition = newInterceptPosition;

      // Check that the target is moving towards the intercept position. Otherwise, the interceptor
      // should wait to be launched.
      if (Vector3.Dot(interceptPosition - initialState.Position,
                      targetPosition - initialState.Position) < 0) {
        return LaunchPlan.NoLaunch;
      }
    }

    // Check that the interceptor is moving towards the target. If the target is moving too fast,
    // the interceptor might be launched backwards because the intercept position and the predicted
    // position are behind the asset. In this case, the interceptor should wait to be launched.
    if (Vector3.Dot(interceptPosition, targetPosition - initialState.Position) > 0) {
      return LaunchPlan.NoLaunch;
    }

    // Check that the intercept position and the predicted position are within some threshold
    // distance of each other.
    if (Vector3.Distance(interceptPosition, targetPosition) < InterceptPositionThreshold) {
      return new LaunchPlan(launchAngleOutput.LaunchAngle, targetPosition);
    }
    return LaunchPlan.NoLaunch;
  }
}
