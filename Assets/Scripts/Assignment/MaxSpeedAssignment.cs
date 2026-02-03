using UnityEngine;

// The maximum speed assignment assigns hierarchical agents to hierarchical agents by maximizing the
// overall intercept speed. The assignment cost is defined as the agent's fractional speed loss
// during the maneuver.
public class MaxSpeedAssignment : CostBasedAssignment {
  // Minimum fractional speed to prevent division by zero.
  private const float _minFractionalSpeed = 1e-6f;

  public MaxSpeedAssignment(AssignDelegate assignFunction)
      : base(CalculateSpeedLoss, assignFunction) {}

  private static float CalculateSpeedLoss(IHierarchical hierarchical, IHierarchical target) {
    if (hierarchical is not HierarchicalAgent hierarchicalAgent) {
      return 0;
    }

    IAgent agent = hierarchicalAgent.Agent;
    float fractionalSpeed = FractionalSpeed.Calculate(agent, target.Position);
    return agent.Speed / Mathf.Max(fractionalSpeed, _minFractionalSpeed);
  }
}
