using UnityEngine;

// Interface for an attack behavior.
//
// The attack behavior determines how the the threat navigates towards the asset.
public interface IAttackBehavior {
  Configs.AttackBehaviorConfig Config { get; set; }

  // Return the next waypoint for the agent to navigate to and the power setting to use towards the
  // waypoint.
  (Vector3 waypointPosition, Configs.Power power)
      GetNextWaypoint(IAgent agent, in Vector3 targetPosition);
}
