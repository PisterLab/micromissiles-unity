using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public class DirectAttackBehavior : AttackBehavior {
  public new DTTFlightPlan flightPlan;

  // Returns the next waypoint for the threat to navigate to.
  // In addition, return the power setting to use toward the waypoint.
  public override (Vector3 waypointPosition, Micromissiles.Power power)
      GetNextWaypoint(Vector3 currentPosition, Vector3 targetPosition) {
    if (flightPlan.waypoints == null || flightPlan.waypoints.Count == 0) {
      // If no waypoints are defined, directly target the target position.
      return (targetPosition, Micromissiles.Power.Max);
    }

    Vector3 directionToTarget = targetPosition - currentPosition;
    float distanceToTarget = directionToTarget.magnitude;

    // Find the current waypoint based on the distance to target.
    int currentWaypointIndex = 0;
    for (int i = 0; i < flightPlan.waypoints.Count; ++i) {
      if (distanceToTarget > flightPlan.waypoints[i].distance) {
        break;
      }
      currentWaypointIndex = i;
    }

    Vector3 waypointPosition;
    Micromissiles.Power power;

    if (currentWaypointIndex == flightPlan.waypoints.Count - 1 &&
        distanceToTarget < flightPlan.waypoints[currentWaypointIndex].distance) {
      // This is the last waypoint, so target the final position.
      waypointPosition = targetPosition;
      // TODO(titan): Replace with the actual power flightPlan.waypoints[0].power after the attack
      // behavior proto has been implemented.
      power = Micromissiles.Power.Mil;
    } else {
      // There is a next waypoint.
      DTTWaypoint nextWaypoint = flightPlan.waypoints[currentWaypointIndex + 1];
      waypointPosition = targetPosition + directionToTarget.normalized * nextWaypoint.distance;
      waypointPosition.y = nextWaypoint.altitude;
      // TODO(titan): Replace with the actual power nextWaypoint.power after the attack behavior
      // proto has been implemented.
      power = Micromissiles.Power.Mil;
    }

    return (waypointPosition, power);
  }

  public new static DirectAttackBehavior FromJson(string json) {
    string resolvedPath = ResolveBehaviorPath(json);
  string fileContent = ConfigLoader.LoadFromStreamingAssets(resolvedPath);
  DirectAttackBehavior behavior = JsonConvert.DeserializeObject<DirectAttackBehavior>(
      fileContent, new JsonSerializerSettings { Converters = { new StringEnumConverter() } });

  // Sort waypoints in ascending order based on distance.
  if (behavior.flightPlan != null && behavior.flightPlan.waypoints != null) {
    behavior.flightPlan.waypoints.Sort((a, b) => a.distance.CompareTo(b.distance));
  }

  return behavior;
}
}

[System.Serializable]
public class DTTWaypoint : Waypoint {
  public float distance;
  public float altitude;
  public Micromissiles.Power power;
}

public class DTTFlightPlan : FlightPlan {
  public List<DTTWaypoint> waypoints;
}
