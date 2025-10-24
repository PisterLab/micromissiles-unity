using System.Collections.Generic;
using System.Linq;

// Flight plan.
//
// The flight plan contains a sorted list of waypoints for the agent on the way to the asset.
public class FlightPlan {
  // Flight plan configuration.
  private Configs.FlightPlan _config;

  // List of waypoints sorted by distance in descending order.
  private List<Configs.FlightPlanWaypoint> _waypoints;

  public Configs.FlightPlan Config {
    get => _config;
    set {
      _config = value;
      _waypoints = null;
    }
  }

  public IReadOnlyList<Configs.FlightPlanWaypoint> Waypoints {
    get {
      if (_waypoints == null) {
        // Sort the waypoints by distance in descending order to allow attack behaviors to iterate
        // from the farthest waypoint to the closest one.
        _waypoints = Config.Waypoints.OrderByDescending(waypoint => waypoint.Distance).ToList();
      }
      return _waypoints.AsReadOnly();
    }
  }

  public FlightPlan(Configs.FlightPlan config) {
    Config = config ?? new Configs.FlightPlan();
  }
}
