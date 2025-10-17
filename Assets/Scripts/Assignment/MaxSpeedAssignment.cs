using UnityEngine;

// The maximum speed assignment assigns hierarchical agents to hierarchical agents by maximizing the
// overall intercept speed. The assignment cost is defined as the agent's fractional speed loss
// during the maneuver.
public class MaxSpeedAssignment : CostBasedAssignment {
  public MaxSpeedAssignment(AssignDelegate assignFunction)
      : base(CalculateSpeedLoss, assignFunction) {}

  private static float CalculateSpeedLoss(IHierarchical hierarchical, IHierarchical target) {
    if (hierarchical is not HierarchicalAgent hierarchicalAgent) {
      return 0;
    }

    IAgent agent = hierarchicalAgent.Agent;
    // The speed decays exponentially with the traveled distance and with the bearing change.
    float distanceTimeConstant = 2 * (agent.StaticConfig.BodyConfig?.Mass ?? 0) /
                                 (Constants.CalculateAirDensityAtAltitude(agent.Position.y) *
                                  (agent.StaticConfig.LiftDragConfig?.DragCoefficient ?? 0) *
                                  (agent.StaticConfig.BodyConfig?.CrossSectionalArea ?? 0));
    float angleTimeConstant = agent.StaticConfig.LiftDragConfig?.LiftDragRatio ?? 1;
    // During the turn, the minimum radius dictates the minimum distance needed to make the turn.
    float minTurningRadius = agent.Velocity.sqrMagnitude / agent.MaxNormalAcceleration();

    Vector3 directionToTarget = target.Position - agent.Position;
    float distanceToTarget = directionToTarget.magnitude;
    float angleToTarget = Vector3.Angle(agent.Velocity, directionToTarget) * Mathf.Deg2Rad;
    // The fractional speed is the product of the fractional speed after traveling the distance and
    // of the fractional speed after turning.
    float fractionalSpeed =
        Mathf.Exp(-((distanceToTarget + angleToTarget * minTurningRadius) / distanceTimeConstant +
                    angleToTarget / angleTimeConstant));
    return agent.Speed / fractionalSpeed;
  }
}
