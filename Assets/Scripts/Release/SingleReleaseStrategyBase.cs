using System.Collections.Generic;
using UnityEngine;

// Base implementation of a single release strategy.
//
// The single release strategy checks whether a sub-interceptor should be released for every
// sub-hierarchical object of the target hierarchical object. No assignment is performed as a result
// of the per-target check.
public abstract class SingleReleaseStrategyBase : ReleaseStrategyBase {
  // The sub-interceptors are launched with a nonzero speed for their rotation to be set correctly.
  private const float _initialSpeed = 1e-3f;

  public SingleReleaseStrategyBase(IAgent agent) : base(agent) {}

  protected override List<IAgent> Release(IEnumerable<IHierarchical> hierarchicals) {
    var carrier = Agent as CarrierBase;
    var releasedAgents = new List<IAgent>();
    int firstChildIndex = carrier.NumSubInterceptors - carrier.NumSubInterceptorsRemaining + 1;
    foreach (var hierarchical in hierarchicals) {
      if (carrier.NumSubInterceptorsRemaining - releasedAgents.Count <= 0) {
        break;
      }
      IAgent releasedAgent = ReleaseSingle(hierarchical, firstChildIndex + releasedAgents.Count);
      if (releasedAgent != null) {
        releasedAgents.Add(releasedAgent);
      }
    }
    return releasedAgents;
  }

  // Plan the release for the given target.
  protected abstract LaunchPlan PlanRelease(IHierarchical target);

  private IAgent ReleaseSingle(IHierarchical hierarchical, int childIndex) {
    IHierarchical target = hierarchical.Target;
    if (target == null || hierarchical.LaunchedHierarchicals.Count != 0) {
      return null;
    }
    LaunchPlan launchPlan = PlanRelease(target);
    if (!launchPlan.ShouldLaunch) {
      return null;
    }

    Simulation.State initialState = new Simulation.State() {
      Position = Coordinates3.ToProto(Agent.Position),
      Velocity =
          Coordinates3.ToProto(launchPlan.NormalizedLaunchVector(Agent.Position) * _initialSpeed),
    };
    IAgent subInterceptor = SimManager.Instance.CreateInterceptor(
        Agent.AgentConfig.SubAgentConfig.AgentConfig, initialState, parentAgent: Agent,
        childIndex: childIndex);
    if (subInterceptor is not IInterceptor subInterceptorInterceptor) {
      return null;
    }
    subInterceptor.HierarchicalAgent.Target = target;
    hierarchical.AddLaunchedHierarchical(subInterceptor.HierarchicalAgent);

    string launchMessage =
        $"Launching {subInterceptor.AgentId} from {Agent.AgentId} at an elevation of " +
        $"{launchPlan.LaunchAngle} degrees to position {launchPlan.InterceptPosition}.";
    Debug.Log(launchMessage);
    UIManager.Instance.LogActionMessage($"[IADS] {launchMessage}", subInterceptor);
    return subInterceptor;
  }
}
