using NUnit.Framework;
using UnityEngine;

public class FlightPlanTests {
  [Test]
  public void Waypoints_ReturnsWaypointsSortedByDescendingDistance() {
    var flightPlanConfig = new Configs.FlightPlan() {
      Type = Configs.FlightPlanType.DistanceToTarget,
      Waypoints =
          {
            new Configs.FlightPlanWaypoint() {
              Distance = 100,
              Altitude = 25,
              Power = Configs.Power.Max,
            },
            new Configs.FlightPlanWaypoint() {
              Distance = 1000,
              Altitude = 100,
              Power = Configs.Power.Cruise,
            },
            new Configs.FlightPlanWaypoint() {
              Distance = 500,
              Altitude = 50,
              Power = Configs.Power.Mil,
            },
            new Configs.FlightPlanWaypoint() {
              Distance = 2000,
              Altitude = 200,
              Power = Configs.Power.Cruise,
            },
          },
    };
    var flightPlan = new FlightPlan(flightPlanConfig);
    Assert.AreEqual(4, flightPlan.Waypoints.Count);
    Assert.AreEqual(2000, flightPlan.Waypoints[0].Distance);
    Assert.AreEqual(1000, flightPlan.Waypoints[1].Distance);
    Assert.AreEqual(500, flightPlan.Waypoints[2].Distance);
    Assert.AreEqual(100, flightPlan.Waypoints[3].Distance);
  }

  [Test]
  public void Waypoints_HandlesNullConfig() {
    var flightPlan = new FlightPlan(config: null);
    Assert.AreEqual(0, flightPlan.Waypoints.Count);
  }
}
