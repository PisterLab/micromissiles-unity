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

  // Maximum intercept position threshold to declare convergence.
  private const float InterceptPositionThreshold = 200;

  public IterativeLaunchPlanner(ILaunchAnglePlanner launchAnglePlanner, IPredictor predictor)
      : base(launchAnglePlanner, predictor) {}

  // Plan the launch.
  public override LaunchPlan Plan() {
    PredictorState initialState = _predictor.Predict(time: 0);
    Vector3 targetPosition = initialState.Position;
    Vector2 initialTargetPositionDirection =
        ILaunchAnglePlanner.ConvertToDirection(initialState.Position);

    LaunchAngleOutput launchAngleOutput = new LaunchAngleOutput();
    Vector2 interceptDirection = Vector2.zero;
    Vector2 predictedDirection = Vector2.zero;
    for (int i = 0; i < MaxNumIterations; ++i) {
      Debug.Log(i);
      // Estimate the time-to-intercept.
      launchAngleOutput = _launchAnglePlanner.Plan(targetPosition);
      float timeToIntercept = launchAngleOutput.TimeToPosition;
      LaunchAngleInput launchAngleInput = _launchAnglePlanner.GetInterceptPosition(targetPosition);

      // Estimate the intercept position.
      PredictorState predictedState = _predictor.Predict(timeToIntercept);
      targetPosition = predictedState.Position;

      // Check whether the intercept direction has changed, in which case the algorithm has
      // converged.
      Vector2 newInterceptDirection =
          new Vector2(launchAngleInput.Distance, launchAngleInput.Altitude);
      if (interceptDirection == newInterceptDirection) {
        break;
      }
      interceptDirection = newInterceptDirection;
      predictedDirection = ILaunchAnglePlanner.ConvertToDirection(targetPosition);

      // Check that the target is moving towards the intercept position.
      if (Vector2.Dot(interceptDirection - initialTargetPositionDirection,
                      predictedDirection - initialTargetPositionDirection) < 0) {
        // The interceptor should wait to be launched.
        return LaunchPlan.NoLaunch;
      }
    }

    // Check whether the intercept position and the predicted position are within some threshold
    // distance of each other.
    if (Vector2.Distance(interceptDirection, predictedDirection) < InterceptPositionThreshold) {
      return new LaunchPlan(launchAngleOutput.LaunchAngle, targetPosition);
    }
    return LaunchPlan.NoLaunch;
  }
}
