using UnityEngine;

// Direct attack behavior.
//
// The agent will navigate directly to each waypoint on the way to the final target.
public class DirectAttackBehavior : AttackBehaviorBase {
  public DirectAttackBehavior(IAgent agent, Configs.AttackBehaviorConfig config)
      : base(agent, config) {}

  // Return the next waypoint for the agent to navigate to and the power setting to use towards the
  // waypoint.
  public override (Vector3 waypointPosition, Configs.Power power)
      GetNextWaypoint(in Vector3 targetPosition) {
    if (FlightPlan.Waypoints.Count == 0) {
      // If no waypoints are defined, directly target the target position.
      return (targetPosition, Configs.Power.Max);
    }

    Vector3 directionToTarget = targetPosition - Agent.Position;
    float distanceToTarget = directionToTarget.magnitude;

    // Find the index of the first waypoint whose position is closer to the target than the current
    // position.
    int waypointIndex = 0;
    for (waypointIndex = 0; waypointIndex < FlightPlan.Waypoints.Count; ++waypointIndex) {
      if (distanceToTarget > FlightPlan.Waypoints[waypointIndex].Distance) {
        break;
      }
    }

    Vector3 waypointPosition = targetPosition;
    Configs.Power power = Configs.Power.Idle;
    if (waypointIndex == FlightPlan.Waypoints.Count) {
      // This is the last waypoint, so target the final position with the last waypoint's power
      // setting.
      power = FlightPlan.Waypoints[FlightPlan.Waypoints.Count - 1].Power;
      return (waypointPosition, power);
    }
    // There is a next waypoint.
    Configs.FlightPlanWaypoint waypoint = FlightPlan.Waypoints[waypointIndex];
    waypointPosition = targetPosition - directionToTarget.normalized * waypoint.Distance;
    waypointPosition.y = waypoint.Altitude;
    power = waypoint.Power;
    return (waypointPosition, power);
  }
}
