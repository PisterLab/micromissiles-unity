using UnityEngine;

// Interface for an attack behavior.
//
// The attack behavior determines how the the threat navigates towards the asset.
public interface IAttackBehavior {
  IAgent Agent { get; set; }

  Configs.AttackBehaviorConfig Config { get; set; }
  FlightPlan FlightPlan { get; }

  // Return the next waypoint for the agent to navigate to and the power setting to use towards the
  // waypoint.
  (Vector3 waypointPosition, Configs.Power power) GetNextWaypoint(in Vector3 targetPosition);
}
