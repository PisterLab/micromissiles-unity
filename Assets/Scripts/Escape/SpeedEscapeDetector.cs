using UnityEngine;

// Speed escape detector.
//
// The speed escape detector checks whether the agent has a speed greater than the threat's when it
// has navigated to the threat's current position.
public class SpeedEscapeDetector : EscapeDetectorBase {
  // Minimum fractional speed to prevent division by zero.
  private const float _minFractionalSpeed = 1e-6f;

  public SpeedEscapeDetector(IAgent agent) : base(agent) {}

  public override bool IsEscaping(IHierarchical target) {
    if (target == null) {
      return false;
    }

    float predictedAgentSpeed = CalculatePredictedAgentSpeed(target.Position);
    return predictedAgentSpeed <= target.Speed;
  }

  // Calculate the predicted agent speed when it has reached the target's current position.
  private float CalculatePredictedAgentSpeed(in Vector3 targetPosition) {
    // The speed decays exponentially with the traveled distance and with the bearing change.
    float distanceTimeConstant = 2 * (Agent.StaticConfig.BodyConfig?.Mass ?? 0) /
                                 (Constants.CalculateAirDensityAtAltitude(Agent.Position.y) *
                                  (Agent.StaticConfig.LiftDragConfig?.DragCoefficient ?? 0) *
                                  (Agent.StaticConfig.BodyConfig?.CrossSectionalArea ?? 0));
    float angleTimeConstant = Agent.StaticConfig.LiftDragConfig?.LiftDragRatio ?? 1;
    // During the turn, the minimum radius dictates the minimum distance needed to make the turn.
    float minTurningRadius = Agent.Velocity.sqrMagnitude / Agent.MaxNormalAcceleration();

    Vector3 directionToTarget = targetPosition - Agent.Position;
    float distanceToTarget = directionToTarget.magnitude;
    float angleToTarget = Vector3.Angle(Agent.Velocity, directionToTarget) * Mathf.Deg2Rad;
    // The fractional speed is the product of the fractional speed after traveling the distance and
    // of the fractional speed after turning.
    float fractionalSpeed =
        Mathf.Exp(-((distanceToTarget + angleToTarget * minTurningRadius) / distanceTimeConstant +
                    angleToTarget / angleTimeConstant));
    // Prevent division by zero.
    fractionalSpeed = Mathf.Max(fractionalSpeed, _minFractionalSpeed);
    return fractionalSpeed * Agent.Speed;
  }
}
