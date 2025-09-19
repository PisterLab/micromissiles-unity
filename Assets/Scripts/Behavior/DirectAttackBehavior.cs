using UnityEngine;

public class DirectAttackBehavior : AttackBehavior {
  public DirectAttackBehavior(in Configs.AttackBehaviorConfig config) : base(config) {}

  // Return the next waypoint for the threat to navigate to and the power setting to use towards the
  // waypoint.
  public virtual (Vector3 waypointPosition, Configs.Power power)
      GetNextWaypoint(Vector3 currentPosition, Vector3 targetPosition) {
    if (FlightPlan.Waypoints.Count == 0) {
      // If no waypoints are defined, directly target the target position.
      return (TargetPosition, Configs.Power.Max);
    }

    Vector3 directionToTarget = TargetPosition - currentPosition;
    float distanceToTarget = directionToTarget.magnitude;

    // Find the current waypoint based on the distance to target.
    int waypointIndex = 0;
    for (int i = 0; i < FlightPlan.Waypoints.Count; ++i) {
      if (distanceToTarget > FlightPlan.Waypoints[i].Distance) {
        break;
      }
      waypointIndex = i;
    }

    Vector3 waypointPosition = TargetPosition;
    Configs.Power power = Configs.Power.Idle;
    if (waypointIndex == FlightPlan.Waypoints.Count - 1 &&
        distanceToTarget < FlightPlan.Waypoints[waypointIndex].Distance) {
      // This is the last waypoint, so target the final position.
      waypointPosition = TargetPosition;
      power = FlightPlan.Waypoints[0].Power;
    } else {
      // There is a next waypoint.
      var nextWaypoint = FlightPlan.Waypoints[waypointIndex + 1];
      waypointPosition = TargetPosition + directionToTarget.normalized * nextWaypoint.Distance;
      waypointPosition.y = nextWaypoint.Altitude;
      power = nextWaypoint.Power;
    }

    return (waypointPosition, power);
  }
}
