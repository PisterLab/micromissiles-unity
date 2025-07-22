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

  // Convergence threshold in meters. If the intercept position changes by less than this amount
  // between iterations, the algorithm is considered to have converged. This value was determined
  // empirically for a NearestNeighborInterpolator2D.
  private const float ConvergenceThreshold = 10f;

  // Maximum distance in meters between the final intercept position and the predicted target
  // position. This validates that the solution is physically realistic.
  //
  // This threshold is highly dependent on the ILaunchAnglePlanner implementation. The current
  // value was determined empirically for a NearestNeighborInterpolator2D using the data in
  // hydra70_launch_angle.csv. It accommodates inaccuracies inherent in a nearest-neighbor search
  // with non-uniform data granularity. This value may need to be retuned if the interpolation
  // method or its underlying data source is changed.
  private const float InterceptPositionThreshold = 100f;

  public IterativeLaunchPlanner(ILaunchAnglePlanner launchAnglePlanner, IPredictor predictor)
      : base(launchAnglePlanner, predictor) {}

  // Plan the launch from a specific interceptor origin.
  // This implementation accounts for the interceptor's starting position and the origin's
  // current location (including movement for naval assets).
  // Returns: Launch plan with timing and angle information
  public override LaunchPlan Plan(InterceptorOrigin origin) {
    // Get the current origin position (accounts for moving origins)
    Vector3 originPosition = origin.GetPosition();
    return Plan(originPosition);
  }

  // Origin-aware implementation for non-zero origins.
  // This implementation properly accounts for interceptor starting position.
  //   originPosition: The position from which the interceptor will be launched
  // Returns: Launch plan with timing and angle information
  private LaunchPlan Plan(Vector3 originPosition) {
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
      Vector3 newInterceptPosition =
          _launchAnglePlanner.GetInterceptPosition(targetPosition, originPosition);
      if ((interceptPosition - newInterceptPosition).magnitude < ConvergenceThreshold) {
        interceptPosition = newInterceptPosition;
        break;
      }
      interceptPosition = newInterceptPosition;

      // Check that the target is moving towards the intercept position relative to the threat's
      // initial position. This prevents launching when the threat is moving away from the predicted
      // intercept.
      Vector3 targetToInterceptPosition = interceptPosition - initialState.Position;
      Vector3 targetToPredictedPosition = targetPosition - initialState.Position;
      if (Vector3.Dot(targetToInterceptPosition, targetToPredictedPosition) < 0) {
        return LaunchPlan.NoLaunch;
      }
    }

    // Check for backwards/sideways launch scenarios using proper geometric analysis.
    if (IsInvalidLaunchGeometry(originPosition, interceptPosition, initialState)) {
      return LaunchPlan.NoLaunch;
    }

    // Final validation: ensure intercept and predicted positions are reasonably close
    if (Vector3.Distance(interceptPosition, targetPosition) < InterceptPositionThreshold) {
      return new LaunchPlan(launchAngleOutput.LaunchAngle, targetPosition);
    }

    return LaunchPlan.NoLaunch;
  }

  // Determines if a launch scenario is geometrically invalid (e.g., into the posterior hemisphere
  // behind the space between the origin and the threat).
  //   originPosition: Position from which the interceptor will be launched.
  //   interceptPosition: Calculated intercept position.
  //   threatState: Initial state of the threat (position and velocity).
  // Returns: True if the launch geometry is invalid and should be prevented.
  private bool IsInvalidLaunchGeometry(Vector3 originPosition, Vector3 interceptPosition,
                                       PredictorState threatState) {
    Vector3 originToThreat = threatState.Position - originPosition;
    Vector3 threatVelocity = threatState.Velocity;

    // A launch is invalid if the threat is moving away from the origin.
    // A dot product > 0 means the angle between the vector from the origin to the threat
    // and its velocity is < 90 degrees, indicating it's moving away.
    if (Vector3.Dot(originToThreat, threatVelocity) > 0.0f) {
      return true;
    }

    // A launch is also invalid if the intercept point is "behind" the origin
    // relative to the threat's direction of approach. "Behind" means the angle
    // between the vector to the threat and the vector to the intercept point is > 90 degrees.
    Vector3 originToIntercept = interceptPosition - originPosition;
    return Vector3.Dot(originToIntercept.normalized, originToThreat.normalized) < 0.0f;
  }
}
