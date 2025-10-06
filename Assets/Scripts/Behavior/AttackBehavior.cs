using UnityEngine;

public abstract class AttackBehavior {
  private Configs.AttackBehaviorConfig _config;

  public string Name {
    get { return _config.Name; }
  }

  public Configs.AttackType Type {
    get { return _config.Type; }
  }

  public FlightPlan FlightPlan {
    get { return new FlightPlan(_config.FlightPlan); }
  }

  public AttackBehavior(Configs.AttackBehaviorConfig config) {
    _config = config;
  }

  // Return the next waypoint for the threat to navigate to and the power setting to use towards the
  // waypoint.
  public abstract (Vector3 waypointPosition, Configs.Power power)
      GetNextWaypoint(Vector3 currentPosition, Vector3 targetPosition);
}
