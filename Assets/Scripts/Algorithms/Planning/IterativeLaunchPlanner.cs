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
  // Maximum number of iterations before declaring failure. In certain cases, the predictor and
  // planner do not converge, so we need to limit the number of iterations.
  private const int MaxNumIterations = 10;

  // Convergence threshold in meters for the difference vector magnitude. Convergence is declared
  // when the intercept position has not changed by more than this threshold between iterations.
  private const float ConvergenceThreshold = 1e-3f;

  // Maximum intercept position threshold in meters to declare convergence. This threshold is used
  // as a final sanity check to ensure that the predicted target position and intercept position do
  // not differ by more than this threshold. This threshold should be set depending on the
  // granularity of the possible intercept positions.
  private const float InterceptPositionThreshold = 1000;

  public IterativeLaunchPlanner(ILaunchAnglePlanner launchAnglePlanner, IPredictor predictor)
      : base(launchAnglePlanner, predictor) {}

  // Plan the launch.
  public override LaunchPlan Plan() {
    return PlanFromZeroOrigin();
  }

  /// <summary>
  /// Plan the launch from a specific interceptor origin.
  /// This implementation accounts for the interceptor's starting position and the origin's
  /// current location (including movement for naval assets).
  /// </summary>
  /// <param name="origin">Interceptor origin configuration</param>
  /// <param name="currentTime">Current simulation time for moving origins</param>
  /// <returns>Launch plan with timing and angle information</returns>
  public override LaunchPlan Plan(InterceptorOriginConfig origin, float currentTime) {
    // Get the current origin position (accounts for moving origins)
    Vector3 originPosition = origin.GetCurrentPosition(currentTime);
    return PlanFromOrigin(originPosition);
  }

  /// <summary>
  /// Original implementation for zero origin (0,0,0).
  /// Preserved for backward compatibility with existing tests.
  /// </summary>
  /// <returns>Launch plan with timing and angle information</returns>
  private LaunchPlan PlanFromZeroOrigin() {
    PredictorState initialState = _predictor.Predict(time: 0);
    Vector3 targetPosition = initialState.Position;

    LaunchAngleOutput launchAngleOutput = new LaunchAngleOutput();
    Vector3 interceptPosition = new Vector3(); 
    
    for (int i = 0; i < MaxNumIterations; ++i) {
      // Estimate the time-to-intercept from the current origin position
      launchAngleOutput = _launchAnglePlanner.Plan(targetPosition);
      float timeToIntercept = launchAngleOutput.TimeToPosition;

      // Estimate the target position at intercept time
      PredictorState predictedState = _predictor.Predict(timeToIntercept);
      targetPosition = predictedState.Position;

      // Check whether the intercept position has converged
      Vector3 newInterceptPosition = _launchAnglePlanner.GetInterceptPosition(targetPosition);
      
      if ((interceptPosition - newInterceptPosition).magnitude < ConvergenceThreshold) {
        interceptPosition = newInterceptPosition;
        break;
      }
      interceptPosition = newInterceptPosition;

      // Check that the target is moving towards the intercept position relative to the threat's initial position.
      // This prevents launching when the threat is moving away from the predicted intercept.
      Vector3 targetToInterceptPosition = interceptPosition - initialState.Position;
      Vector3 targetToPredictedPosition = targetPosition - initialState.Position;
      if (Vector3.Dot(targetToInterceptPosition, targetToPredictedPosition) < 0) {
        return LaunchPlan.NoLaunch;
      }
    }

    // Check that the interceptor is moving towards the target. This prevents backwards launches
    // when the intercept position is behind the origin relative to the threat direction.
    // This is the core fix for the backwards launch issue identified in PR #46.
    Vector3 interceptorToInterceptPosition = interceptPosition - Vector3.zero;
    Vector3 threatDirection = initialState.Velocity.normalized;
    float dot2 = Vector3.Dot(interceptorToInterceptPosition.normalized, threatDirection);
    // Only flag as backwards if interceptor and threat are moving in very similar directions (> 0.8)
    if (dot2 > 0.8f) {
      return LaunchPlan.NoLaunch;
    }

    // Final validation: ensure intercept and predicted positions are reasonably close
    if (Vector3.Distance(interceptPosition, targetPosition) < InterceptPositionThreshold) {
      return new LaunchPlan(launchAngleOutput.LaunchAngle, interceptPosition);
    }
    
    return LaunchPlan.NoLaunch;
  }

  /// <summary>
  /// Origin-aware implementation for non-zero origins.
  /// This implementation properly accounts for interceptor starting position.
  /// </summary>
  /// <param name="originPosition">The position from which the interceptor will be launched</param>
  /// <returns>Launch plan with timing and angle information</returns>
  private LaunchPlan PlanFromOrigin(Vector3 originPosition) {
    PredictorState initialState = _predictor.Predict(time: 0);
    Vector3 targetPosition = initialState.Position;

    LaunchAngleOutput launchAngleOutput = new LaunchAngleOutput();
    Vector3 interceptPosition = new Vector3();
    
    for (int i = 0; i < MaxNumIterations; ++i) {
      // Estimate the time-to-intercept from the current origin position
      launchAngleOutput = _launchAnglePlanner.Plan(targetPosition, originPosition);
      float timeToIntercept = launchAngleOutput.TimeToPosition;

      // Estimate the target position at intercept time
      PredictorState predictedState = _predictor.Predict(timeToIntercept);
      targetPosition = predictedState.Position;

      // Check whether the intercept position has converged
      Vector3 newInterceptPosition = _launchAnglePlanner.GetInterceptPosition(targetPosition, originPosition);
      if ((interceptPosition - newInterceptPosition).magnitude < ConvergenceThreshold) {
        interceptPosition = newInterceptPosition;
        break;
      }
      interceptPosition = newInterceptPosition;

      // Check that the target is moving towards the intercept position relative to the threat's initial position.
      // This prevents launching when the threat is moving away from the predicted intercept.
      Vector3 targetToInterceptPosition = interceptPosition - initialState.Position;
      Vector3 targetToPredictedPosition = targetPosition - initialState.Position;
      if (Vector3.Dot(targetToInterceptPosition, targetToPredictedPosition) < 0) {
        return LaunchPlan.NoLaunch;
      }
    }

    // Check that the interceptor is moving towards the target. This prevents backwards launches
    // when the intercept position is behind the origin relative to the threat direction.
    // This is the core fix for the backwards launch issue identified in PR #46.
    Vector3 interceptorToInterceptPosition = interceptPosition - originPosition;
    Vector3 threatDirection = initialState.Velocity.normalized;
    float dot = Vector3.Dot(interceptorToInterceptPosition.normalized, threatDirection);
    // Only flag as backwards if interceptor and threat are moving in very similar directions (> 0.8)
    if (dot > 0.8f) {
      return LaunchPlan.NoLaunch;
    }

    // Final validation: ensure intercept and predicted positions are reasonably close
    if (Vector3.Distance(interceptPosition, targetPosition) < InterceptPositionThreshold) {
      return new LaunchPlan(launchAngleOutput.LaunchAngle, interceptPosition);
    }
    
    return LaunchPlan.NoLaunch;
  }
}
