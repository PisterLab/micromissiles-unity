using UnityEngine;

// The proximity release strategy decides to launch an interceptor against an incoming target
// depending on the distance and bearing to the target.
public class ProximityReleaseStrategy : MassReleaseStrategyBase {
  // Geometric parameters for determining when to launch.
  // The carrier interceptor will spawn submunitions when any target is greater than 30 degrees away
  // from the carrier interceptor's current velocity or when any threat is within 1000 meters of the
  // interceptor.
  private const float _maxBearingDegrees = 30f;
  private const float _minDistanceToThreat = 1000f;
  private const float _maxDistanceToThreat = 2500f;
  // TODO(titan): The prediction time should be a function of the sub-interceptor characteristic,
  // such as the boost time.
  private const float _predictionTime = 0.6f;

  public ProximityReleaseStrategy(IAgent agent, IAssignment assignment) : base(agent, assignment) {}

  protected override LaunchPlan PlanRelease(IHierarchical target) {
    var predictor = new LinearExtrapolator(target);
    PredictorState predictedState = predictor.Predict(_predictionTime);
    Vector3 positionToPredictedTarget = predictedState.Position - Agent.Position;
    float predictedDistanceToTarget = positionToPredictedTarget.magnitude;

    // Check whether the distance to the target is less than the minimum distance.
    if (predictedDistanceToTarget < _minDistanceToThreat) {
      return new LaunchPlan { ShouldLaunch = true };
    }

    // Check whether the bearing exceeds the maximum bearing.
    float lateralDistance =
        (Vector3.ProjectOnPlane(positionToPredictedTarget, Agent.Velocity)).magnitude;
    float sinBearing = Mathf.Clamp01(lateralDistance / predictedDistanceToTarget);
    float bearing = Mathf.Asin(sinBearing) * Mathf.Rad2Deg;
    if (bearing > _maxBearingDegrees && predictedDistanceToTarget < _maxDistanceToThreat) {
      return new LaunchPlan { ShouldLaunch = true };
    }
    return LaunchPlan.NoLaunch();
  }
}
