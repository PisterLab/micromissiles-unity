using System.Collections.Generic;
using System.Linq;

public class FlightPlan {
  private Configs.AttackBehaviorConfig.Types.FlightPlan _flightPlan;

  public Configs.AttackBehaviorConfig.Types.FlightPlanType Type {
    get { return _flightPlan.Type; }
  }

  public List<Waypoint> Waypoints { get; }

  public FlightPlan(in Configs.AttackBehaviorConfig.Types.FlightPlan flightPlan) {
    _flightPlan = flightPlan ?? new Configs.AttackBehaviorConfig.Types.FlightPlan();
    // Sort waypoints in descending order based on distance.
    Waypoints = _flightPlan.Waypoints.Select(waypoint => new Waypoint(waypoint))
                    .OrderByDescending(waypoint => waypoint.Distance)
                    .ToList();
  }
}
