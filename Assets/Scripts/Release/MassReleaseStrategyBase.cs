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

  protected override List<IAgent> Release(IEnumerable<IHierarchical> hierarchicals) {
    var carrier = Agent as CarrierBase;
    if (carrier.NumSubInterceptorsRemaining <= 0) {
      return new List<IAgent>();
    }

    Dictionary<IHierarchical, IHierarchical> targetToHierarchicalMap =
        hierarchicals.Where(hierarchical => hierarchical.Target != null)
            .ToDictionary(hierarchical => hierarchical.Target, hierarchical => hierarchical);
    List<IHierarchical> targets = targetToHierarchicalMap.Keys.ToList();
    if (targets.Count == 0) {
      return new List<IAgent>();
    }
    LaunchPlan launchPlan = PlanRelease(targets);
    if (!launchPlan.ShouldLaunch) {
      return new List<IAgent>();
    }

    // Release all sub-interceptors.
    Configs.SubAgentConfig subAgentConfig = carrier.AgentConfig.SubAgentConfig;
    Vector3 position = carrier.Position;
    Simulation.CartesianCoordinates positionCoordinates = Coordinates3.ToProto(position);

    Vector3 velocity = carrier.Velocity;
    Vector3 perpendicularDirection = Vector3.Cross(velocity, Vector3.up);

    var releasedAgents = new List<IAgent>();
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
      if (subInterceptor != null && subInterceptor is IInterceptor subInterceptorInterceptor) {
        releasedAgents.Add(subInterceptor);
      }
    }

    // Assign the released sub-interceptors to the targets.
    var releasedAgentHierarchicals =
        releasedAgents.Select(agent => agent.HierarchicalAgent).ToList();
    List<AssignmentItem> assignments = Assignment.Assign(releasedAgentHierarchicals, targets);
    foreach (var assignment in assignments) {
      assignment.First.Target = assignment.Second;
      targetToHierarchicalMap[assignment.Second].AddLaunchedHierarchical(assignment.First);
    }

    return releasedAgents;
  }

  // Plan the release for the given targets.
  protected abstract LaunchPlan PlanRelease(IEnumerable<IHierarchical> targets);
}
