using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Base implementation of a mass release strategy.
//
// The mass release strategy checks whether all sub-interceptors should be released simultaneously
// to target the target hierarchical object. The released sub-interceptors are then assigned to the
// sub-hierarchical objects of the target hierarchical object.
public abstract class MassReleaseStrategyBase : ReleaseStrategyBase {
  private const float _fanOutAngleDegrees = 60f;

  public IAssignment Assignment { get; set; }

  public MassReleaseStrategyBase(IAgent agent, IAssignment assignment) : base(agent) {
    Assignment = assignment;
  }

  public override List<IAgent> Release() {
    if (Agent is not CarrierBase carrier || carrier.NumSubInterceptorsRemaining <= 0) {
      return new List<IAgent>();
    }

    IHierarchical target = Agent.HierarchicalAgent.Target;
    if (target == null) {
      return new List<IAgent>();
    }

    var releasedAgents = new List<IAgent>();
    LaunchPlan launchPlan = PlanRelease(target);
    if (launchPlan.ShouldLaunch) {
      // Release all sub-interceptors.
      Configs.SubAgentConfig subAgentConfig = Agent.AgentConfig.SubAgentConfig;
      Vector3 position = Agent.Position;
      Simulation.CartesianCoordinates positionCoordinates = Coordinates3.ToProto(position);

      Vector3 velocity = Agent.Velocity;
      Vector3 perpendicularDirection = Vector3.Cross(velocity, Vector3.up);

      for (int i = 0; i < subAgentConfig.NumSubAgents; ++i) {
        // Fan the submunitions radially outwards from the carrier's velocity vector.
        Vector3 lateralDirection =
            Quaternion.AngleAxis(i * 360 / subAgentConfig.NumSubAgents, velocity) *
            perpendicularDirection;
        Simulation.CartesianCoordinates velocityCoordinates =
            Coordinates3.ToProto(Vector3.RotateTowards(
                velocity, lateralDirection, maxRadiansDelta: _fanOutAngleDegrees * Mathf.Deg2Rad,
                maxMagnitudeDelta: Mathf.Cos(_fanOutAngleDegrees * Mathf.Deg2Rad)));
        Simulation.State initialState = new Simulation.State() {
          Position = positionCoordinates,
          Velocity = velocityCoordinates,
        };
        IAgent subInterceptor =
            SimManager.Instance.CreateInterceptor(subAgentConfig.AgentConfig, initialState);
        if (subInterceptor != null) {
          if (subInterceptor.Movement is MissileMovement movement) {
            movement.FlightPhase = Simulation.FlightPhase.Boost;
          }
          releasedAgents.Add(subInterceptor);
        }
      }

      // Assign the released sub-interceptors to the sub-hierarchical objects of the target.
      if (target.SubHierarchicals.Count() != 0) {
        var releasedAgentHierarchicals =
            releasedAgents.Select(agent => agent.HierarchicalAgent).ToList();
        List<AssignmentItem> assignments =
            Assignment.Assign(releasedAgentHierarchicals, target.SubHierarchicals.ToList());
        foreach (var assignment in assignments) {
          assignment.First.Target = assignment.Second;
        }
      }
    }
    return releasedAgents;
  }
}
