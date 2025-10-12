using UnityEngine;

// Base implementation of an attack behavior.
public abstract class AttackBehaviorBase : IAttackBehavior {
  // Attack behavior configuration.
  [SerializeField]
  private Configs.AttackBehaviorConfig _config;

  // Flight plan.
  [SerializeField]
  private FlightPlan _flightPlan;

  public Configs.AttackBehaviorConfig Config {
    get => _config;
    set => _config = value;
  }

  // Return the next waypoint for the agent to navigate to and the power setting to use towards the
  // waypoint.
  public abstract (Vector3 waypointPosition, Configs.Power power)
      GetNextWaypoint(IAgent agent, in Vector3 targetPosition);
}
