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
    foreach (var hierarchical in hierarchicals) {
      if (carrier.NumSubInterceptorsRemaining - releasedAgents.Count <= 0) {
        break;
      }
      IAgent releasedAgent = ReleaseSingle(hierarchical);
      if (releasedAgent != null) {
        releasedAgents.Add(releasedAgent);
      }
    }
    return releasedAgents;
  }

  // Plan the release for the given target.
  protected abstract LaunchPlan PlanRelease(IHierarchical target);

  private IAgent ReleaseSingle(IHierarchical hierarchical) {
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
        Agent.AgentConfig.SubAgentConfig.AgentConfig, initialState);
    if (subInterceptor == null || subInterceptor is not IInterceptor subInterceptorInterceptor) {
      return null;
    }
    var agentInterceptor = Agent as IInterceptor;
    subInterceptor.HierarchicalAgent.Target = target;
    if (subInterceptor.Movement is MissileMovement movement) {
      movement.FlightPhase = Simulation.FlightPhase.Boost;
    }
    hierarchical.AddLaunchedHierarchical(subInterceptor.HierarchicalAgent);

    Debug.Log(
        $"Launching a {subInterceptor.StaticConfig.AgentType} at an elevation of {launchPlan.LaunchAngle} degrees to position {launchPlan.InterceptPosition}.");
    UIManager.Instance.LogActionMessage(
        $"[IADS] Launching a {subInterceptor.StaticConfig.AgentType} at an elevation of {launchPlan.LaunchAngle} degrees to position {launchPlan.InterceptPosition}.");
    return subInterceptor;
  }
}
