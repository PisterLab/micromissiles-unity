using UnityEngine;

public static class FractionalSpeed {
  // Calculate the estimated fractional speed for the given hierarchical object to reach the given
  // target position.
  public static float Calculate(IAgent agent, in Vector3 targetPosition) {
    // The speed decays exponentially with the traveled distance and with the bearing change.
    float distanceTimeConstant = 2 * (agent.StaticConfig.BodyConfig?.Mass ?? 1) /
                                 (Constants.CalculateAirDensityAtAltitude(agent.Position.y) *
                                  (agent.StaticConfig.LiftDragConfig?.DragCoefficient ?? 0) *
                                  (agent.StaticConfig.BodyConfig?.CrossSectionalArea ?? 0));
    float angleTimeConstant =
        agent.StaticConfig.LiftDragConfig?.LiftDragRatio ?? float.PositiveInfinity;
    // During the turn, the minimum radius dictates the minimum distance needed to make the turn.
    float minTurningRadius = agent.Velocity.sqrMagnitude / agent.MaxNormalAcceleration();

    Vector3 directionToTarget = targetPosition - agent.Position;
    float distanceToTarget = directionToTarget.magnitude;
    float angleToTarget = Vector3.Angle(agent.Velocity, directionToTarget) * Mathf.Deg2Rad;
    // The fractional speed is the product of the fractional speed after traveling the distance and
    // of the fractional speed after turning.
    float fractionalSpeed =
        Mathf.Exp(-((distanceToTarget + angleToTarget * minTurningRadius) / distanceTimeConstant +
                    angleToTarget / angleTimeConstant));
    return fractionalSpeed;
  }
}
