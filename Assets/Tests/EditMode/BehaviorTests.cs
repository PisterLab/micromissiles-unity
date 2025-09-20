using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class BehaviorTests : TestBase {
  [Test]
  public void TestDirectAttackBehaviorWaypoints() {
    // Create a sample direct attack behavior.
    DirectAttackBehavior attackBehavior =
        new DirectAttackBehavior(
            new Configs.AttackBehaviorConfig() {
              Name = "Sample Attack", Type = Configs.AttackType.DirectAttack,
              FlightPlan =
                  new Configs.AttackBehaviorConfig.Types.FlightPlan() {
                    Type = Configs.AttackBehaviorConfig.Types.FlightPlanType.DistanceToTarget,
                    Waypoints =
                        { new List<Configs.AttackBehaviorConfig.Types.FlightPlan.Types.Waypoint>() {
                          new Configs.AttackBehaviorConfig.Types.FlightPlan.Types.Waypoint() {
                            Distance = 1000,
                            Altitude = 100,
                            Power = Configs.Power.Cruise,
                          },
                          new Configs.AttackBehaviorConfig.Types.FlightPlan.Types.Waypoint() {
                            Distance = 500,
                            Altitude = 50,
                            Power = Configs.Power.Mil,
                          },
                          new Configs.AttackBehaviorConfig.Types.FlightPlan.Types.Waypoint() {
                            Distance = 100,
                            Altitude = 25,
                            Power = Configs.Power.Max,
                          }
                        } }
                  }
            });

    Vector3 targetPosition = new Vector3(1000, 0, 0);
    const float epsilon = 0.001f;

    // Test the waypoint selection based on distance.
    Vector3 currentPosition = new Vector3(-100, 0, 0);
    var result = attackBehavior.GetNextWaypoint(currentPosition, targetPosition);
    Assert.AreEqual(0, result.waypointPosition.x, epsilon);
    Assert.AreEqual(100, result.waypointPosition.y, epsilon);
    Assert.AreEqual(0, result.waypointPosition.z, epsilon);
    Assert.AreEqual(Configs.Power.Cruise, result.power);

    currentPosition = new Vector3(0, 0, 0);
    result = attackBehavior.GetNextWaypoint(currentPosition, targetPosition);
    Assert.AreEqual(500, result.waypointPosition.x, epsilon);
    Assert.AreEqual(50, result.waypointPosition.y, epsilon);
    Assert.AreEqual(0, result.waypointPosition.z, epsilon);
    Assert.AreEqual(Configs.Power.Mil, result.power);

    currentPosition = new Vector3(600, 0, 0);
    result = attackBehavior.GetNextWaypoint(currentPosition, targetPosition);
    Assert.AreEqual(900, result.waypointPosition.x, epsilon);
    Assert.AreEqual(25, result.waypointPosition.y, epsilon);
    Assert.AreEqual(0, result.waypointPosition.z, epsilon);
    Assert.AreEqual(Configs.Power.Max, result.power);

    // Test the attack behavior within the final distance.
    currentPosition = new Vector3(920, 0, 0);
    result = attackBehavior.GetNextWaypoint(currentPosition, targetPosition);
    Assert.AreEqual(1000, result.waypointPosition.x, epsilon);
    Assert.AreEqual(0, result.waypointPosition.y, epsilon);
    Assert.AreEqual(0, result.waypointPosition.z, epsilon);
    Assert.AreEqual(Configs.Power.Max, result.power);

    // Test with non-zero z-coordinate.
    targetPosition = new Vector3(800, 0, 600);
    currentPosition = new Vector3(0, 0, 0);
    result = attackBehavior.GetNextWaypoint(currentPosition, targetPosition);
    Assert.AreEqual(400, result.waypointPosition.x, epsilon);
    Assert.AreEqual(50, result.waypointPosition.y, epsilon);
    Assert.AreEqual(300, result.waypointPosition.z, epsilon);
    Assert.AreEqual(Configs.Power.Mil, result.power);

    currentPosition = new Vector3(400, 0, 300);
    result = attackBehavior.GetNextWaypoint(currentPosition, targetPosition);
    Assert.AreEqual(720, result.waypointPosition.x, epsilon);
    Assert.AreEqual(25, result.waypointPosition.y, epsilon);
    Assert.AreEqual(540, result.waypointPosition.z, epsilon);
    Assert.AreEqual(Configs.Power.Max, result.power);
  }
}
