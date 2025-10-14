using System.Collections.Generic;
using System.Linq;

// Flight plan.
//
// The flight plan contains a sorted list of waypoints for the agent on the way to the asset.
public class FlightPlan {
  // List of waypoints sorted by distance in descending order.
  List<Configs.FlightPlanWaypoint> _waypoints;

  public Configs.FlightPlan Config { get; set; }

  public IReadOnlyList<Configs.FlightPlanWaypoint> Waypoints {
    get {
      if (_waypoints == null) {
        _waypoints = Config.Waypoints.OrderByDescending(waypoint => waypoint.Distance).ToList();
      }
      return _waypoints.AsReadOnly();
    }
  }

  public FlightPlan(Configs.FlightPlan config) {
    Config = config;
  }
}
