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
        if (Agent.IsPursuable) {
          // Remove the interceptor as a pursuer from all target sub-hierarchical objects.
          void RemovePursuerFromHierarchical(IHierarchical target) {
            target.RemovePursuer(this);
            foreach (var subHierarchical in target.SubHierarchicals) {
              RemovePursuerFromHierarchical(subHierarchical);
            }
          }
          RemovePursuerFromHierarchical(base.Target);
        }
        ClearSubHierarchicals();
        SimManager.Instance.DestroyDummyAgent(Agent.TargetModel);
        Agent.TargetModel = null;
      }
      base.Target = value;
      if (base.Target != null) {
        base.Target.AddPursuer(this);
        if (Agent is IInterceptor interceptor) {
          // Subscribe to the target events.
          foreach (var targetHierarchical in Target.ActiveSubHierarchicals) {
            if (targetHierarchical is HierarchicalAgent targetAgent) {
              targetAgent.Agent.OnTerminated += (IAgent agent) =>
                  agent.HierarchicalAgent.RemoveTargetHierarchical(targetHierarchical);
            }
          }

          // Perform recursive clustering on the new targets.
          RecursiveCluster(maxClusterSize: interceptor.CapacityPerSubInterceptor);

          if (Agent.IsPursuable) {
            // Add the interceptor as a pursuer to all target sub-hierarchical objects.
            void AddPursuerToHierarchical(IHierarchical target) {
              target.AddPursuer(this);
              foreach (var subHierarchical in target.SubHierarchicals) {
                AddPursuerToHierarchical(subHierarchical);
              }
            }
            AddPursuerToHierarchical(base.Target);
          }
        }
        Agent.TargetModel =
            SimManager.Instance.CreateDummyAgent(base.Target.Position, base.Target.Velocity);
      }
    }
  }

  public HierarchicalAgent(IAgent agent) {
    Agent = agent;
  }
}
