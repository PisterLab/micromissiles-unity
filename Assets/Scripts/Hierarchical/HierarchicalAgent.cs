using UnityEngine;

// Hierarchy node owned by an agent.
//
// Since agents cannot inherit from both HierarchicalBase and MonoBehaviour, agents use composition
// to know their position within the hierarchical strategy.
public class HierarchicalAgent : HierarchicalBase {
  // Agent to which this hierarchical node belongs.
  public IAgent Agent { get; init; }

  public override Vector3 Position => Agent.Position;
  public override Vector3 Velocity => Agent.Velocity;
  public override Vector3 Acceleration => Agent.Acceleration;
  public override bool IsTerminated => Agent.IsTerminated;

  public override IHierarchical Target {
    get { return base.Target; }
    set {
      if (base.Target != null) {
        base.Target.RemovePursuer(this);
        SimManager.Instance.DestroyDummyAgent(Agent.TargetModel);
        Agent.TargetModel = null;
      }
      base.Target = value;
      if (base.Target != null) {
        base.Target.AddPursuer(this);
        Agent.TargetModel =
            SimManager.Instance.CreateDummyAgent(base.Target.Position, base.Target.Velocity);
      }
    }
  }

  public HierarchicalAgent(IAgent agent) {
    Agent = agent;
  }
}
