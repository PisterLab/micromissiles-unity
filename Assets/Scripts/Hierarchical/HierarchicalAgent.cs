using UnityEngine;

// Hierarchy node owned by an agent.
//
// Since agents cannot inherit from both HierarchicalBase and MonoBehaviour, agents use composition
// to know their position within the hierarchical strategy.
public class HierarchicalAgent : HierarchicalBase {
  [SerializeField]
  private AgentBase _agent;

  public AgentBase Agent {
    get => _agent;
    set => _agent = value;
  }

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
