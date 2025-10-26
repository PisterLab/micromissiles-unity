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

  public override List<IAgent> Release() {
    IHierarchical target = Agent.HierarchicalAgent.Target;
    if (target == null || target.SubHierarchicals.Count == 0) {
      return new List<IAgent>();
    }
    var carrier = Agent as CarrierBase;
    if (carrier == null) {
      return new List<IAgent>();
    }

    var releasedAgents = new List<IAgent>();
    foreach (var subHierarchical in target.SubHierarchicals) {
      if (carrier.NumSubInterceptorsRemaining - releasedAgents.Count <= 0) {
        break;
      }
      if (subHierarchical.IsTerminated) {
        continue;
      }
      if (subHierarchical.Pursuers.Count != 0 && !subHierarchical.IsEscapingPursuers()) {
        continue;
      }
      LaunchPlan launchPlan = PlanRelease(subHierarchical);
      if (launchPlan.ShouldLaunch) {
        Simulation.State initialState = new Simulation.State() {
          Position = Coordinates3.ToProto(carrier.Position),
          Velocity = Coordinates3.ToProto(launchPlan.NormalizedLaunchVector(carrier.Position) *
                                          _initialSpeed),
        };
        IAgent subInterceptor = SimManager.Instance.CreateInterceptor(
            carrier.AgentConfig.SubAgentConfig.AgentConfig, initialState);
        if (subInterceptor != null) {
          subInterceptor.HierarchicalAgent.Target = subHierarchical;
          if (subInterceptor.Movement is MissileMovement movement) {
            movement.FlightPhase = Simulation.FlightPhase.Boost;
          }
          if (subInterceptor is IInterceptor subInterceptorInterceptor) {
            subInterceptorInterceptor.OnHit += carrier.RegisterSubInterceptorHit;
            subInterceptorInterceptor.OnMiss += carrier.RegisterSubInterceptorMiss;
          }
          releasedAgents.Add(subInterceptor);

          Debug.Log(
              $"Launching a {subInterceptor.StaticConfig.AgentType} at an elevation of {launchPlan.LaunchAngle} degrees to position {launchPlan.InterceptPosition}.");
          UIManager.Instance.LogActionMessage(
              $"[IADS] Launching a {subInterceptor.StaticConfig.AgentType} at an elevation of {launchPlan.LaunchAngle} degrees to position {launchPlan.InterceptPosition}.");
        }
      }
    }
    return releasedAgents;
  }
}
