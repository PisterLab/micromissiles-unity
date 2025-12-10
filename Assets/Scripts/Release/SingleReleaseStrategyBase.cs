using System.Collections.Generic;
using UnityEngine;

// Base implementation of a single release strategy.
//
// The single release strategy checks whether a sub-interceptor should be released for every
// sub-hierarchical object of the target hierarchical object. No assignment is performed as a result
// of the per-target check.
public abstract class SingleReleaseStrategyBase : ReleaseStrategyBase {
  // The sub-interceptors are launched with a nonzero speed for their rotation to be set correctly.
  private const float _initialSpeed = 1e-6f;

  public SingleReleaseStrategyBase(IAgent agent) : base(agent) {}

  public override List<IAgent> Release() {
    IHierarchical target = Agent.HierarchicalAgent.Target;
    if (target == null || target.SubHierarchicals.Count == 0) {
      return new List<IAgent>();
    }

    var releasedAgents = new List<IAgent>();
    foreach (var subHierarchical in target.SubHierarchicals) {
      LaunchPlan launchPlan = PlanRelease(subHierarchical);
      if (launchPlan.ShouldLaunch) {
        Simulation.State initialState = new Simulation.State() {
          Position = Coordinates3.ToProto(Agent.Position),
          Velocity = Coordinates3.ToProto(launchPlan.NormalizedLaunchVector(Agent.Position) *
                                          _initialSpeed),
        };
        IAgent subInterceptor = SimManager.Instance.CreateInterceptor(
            Agent.AgentConfig.SubAgentConfig.AgentConfig, initialState);
        if (subInterceptor != null) {
          subInterceptor.HierarchicalAgent.Target = subHierarchical;
          if (subInterceptor.Movement is MissileMovement movement) {
            movement.FlightPhase = Simulation.FlightPhase.Boost;
          }
          releasedAgents.Add(subInterceptor);
        }
      }
    }
    return releasedAgents;
  }
}
