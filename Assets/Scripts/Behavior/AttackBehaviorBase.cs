using UnityEngine;

// Base implementation of an attack behavior.
public abstract class AttackBehaviorBase : IAttackBehavior {
  // Attack behavior configuration.
  [SerializeField]
  private Configs.AttackBehaviorConfig _config;

  // Flight plan.
  private FlightPlan _flightPlan;

  // Agent that will execute the attack behavior.
  public IAgent Agent { get; init; }

  public Configs.AttackBehaviorConfig Config {
    get => _config;
    init => _config = value;
  }

  public FlightPlan FlightPlan {
    get {
      if (_flightPlan == null) {
        _flightPlan = new FlightPlan(Config?.FlightPlan);
      }
      return _flightPlan;
    }
  }

  public AttackBehaviorBase(IAgent agent, Configs.AttackBehaviorConfig config) {
    Agent = agent;
    Config = config;
  }

  // Return the next waypoint for the agent to navigate to and the power setting to use towards the
  // waypoint.
  public abstract (Vector3 waypointPosition, Configs.Power power)
      GetNextWaypoint(in Vector3 targetPosition);
}
