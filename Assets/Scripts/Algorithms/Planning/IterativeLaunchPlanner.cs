using UnityEngine;

// The iterative launch planner class is a launch planner that performs an iterative process to
// determine the intercept point. The algorithm continuously performs the following 2-step iterative
// process:
//  1. Time-to-intercept estimation: The algorithm determines the time it takes the interceptor to
//  reach the target at the predicted intercept position, which is initialized to the target's
//  current position.
//  2. Intercept position estimation: The algorithm predicts the target position at the estimated
//  time-to-intercept.
// If the distance to the predicted target position is greater than the initial distance to the
// target position, the interceptor should wait to be launched.
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
    Vector2 initialTargetDirection =
        new Vector2(Vector3.ProjectOnPlane(initialState.Position, Vector3.up).magnitude,
                    Vector3.Project(initialState.Position, Vector3.up).magnitude);

    for (int i = 0; i < MaxNumIterations; ++i) {
      // Estimate the time-to-intercept.
      LaunchAngleOutput launchAngleOutput = _launchAnglePlanner.Plan(targetPosition);
      float timeToIntercept = launchAngleOutput.TimeToPosition;

      // Estimate the intercept position.
      PredictorState predictedState = _predictor.Predict(timeToIntercept);
      Vector3 predictedPosition = predictedState.Position;

      // Calculate the intercept and predicted directions.
      LaunchAngleInput launchAngleInput = _launchAnglePlanner.GetInterceptPosition(targetPosition);
      Vector2 interceptDirection =
          new Vector2(launchAngleInput.Distance, launchAngleInput.Altitude);
      Vector2 predictedDirection =
          new Vector2(Vector3.ProjectOnPlane(predictedPosition, Vector3.up).magnitude,
                      Vector3.Project(predictedPosition, Vector3.up).magnitude);

      // Check whether the distance from the predicted position to the intercept position is less
      // than the distance from the initial position to the intercept position.
      if (Vector2.Distance(predictedDirection, interceptDirection) >
          Vector2.Distance(initialTargetDirection, interceptDirection)) {
        // The interceptor should wait to be launched.
        return LaunchPlan.NoLaunch;
      }

      // Check whether the algorithm has converged by checking whether the intercept position and
      // the predicted position are close together.
      if (Vector2.Distance(interceptDirection, predictedDirection) < InterceptPositionThreshold) {
        return new LaunchPlan(launchAngleOutput.LaunchAngle, predictedPosition);
      }

      targetPosition = predictedPosition;
    }
    return LaunchPlan.NoLaunch;
  }
}
