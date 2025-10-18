using UnityEngine;

// Hierarchy node owned by an agent.
//
// Since agents cannot inherit from both HierarchicalBase and MonoBehaviour, agents use composition
// to know their position within the hierarchical strategy.
public class HierarchicalAgent : HierarchicalBase {
  // Agent to which this hierarchical node belongs.
  public IAgent Agent { get; set; }

  protected override Vector3 GetPosition() {
    return Agent.Position;
  }
  protected override Vector3 GetVelocity() {
    return Agent.Velocity;
  }
  protected override Vector3 GetAcceleration() {
    return Agent.Acceleration;
  }
}
