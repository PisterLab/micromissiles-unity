using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AttackBehavior {
  private Configs.AttackBehaviorConfig _config;

  public string Name {
    get { return _config.Name; }
  }

  public Configs.AttackType Type {
    get { return _config.Type; }
  }

  public Vector3 TargetPosition {
    get { return Coordinates3.FromProto(_config.TargetPosition); }
  }

  public Vector3 TargetVelocity {
    get { return Coordinates3.FromProto(_config.TargetVelocity); }
  }

  public Vector3 TargetColliderSize {
    get { return Coordinates3.FromProto(_config.TargetColliderSize); }
  }

  public FlightPlan FlightPlan {
    get { return new FlightPlan(_config.FlightPlan); }
  }

  public AttackBehavior(in Configs.AttackBehaviorConfig config) {
    _config = config;
  }

  // Return the next waypoint for the threat to navigate to and the power setting to use towards the
  // waypoint.
  public virtual (Vector3 waypointPosition, Configs.Power power)
      GetNextWaypoint(Vector3 currentPosition, Vector3 targetPosition) {
    return (targetPosition, Configs.Power.Idle);
  }
}
