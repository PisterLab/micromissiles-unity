using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class BehaviorTests : TestBase {
  [Test]
  public void TestDirectAttackBehaviorFactoryUsesOverride() {
    var config = new Configs.AttackBehaviorConfig() {
      Name = "Sample Attack", Type = Configs.AttackType.DirectAttack,
      FlightPlan = new Configs.FlightPlan() { Type = Configs.FlightPlanType.DistanceToTarget,
                                              Waypoints = { new List<Configs.FlightPlanWaypoint>() {
                                                new Configs.FlightPlanWaypoint() {
                                                  Distance = 1000,
                                                  Altitude = 100,
                                                  Power = Configs.Power.Cruise,
                                                }
                                              } } }
    };

    AttackBehavior attackBehavior = AttackBehaviorFactory.Create(config);
    Assert.IsNotNull(attackBehavior);

    Vector3 currentPosition = new Vector3(-100, 0, 0);
    Vector3 targetPosition = new Vector3(0, 0, 0);
    var result = attackBehavior.GetNextWaypoint(currentPosition, targetPosition);

    Assert.AreEqual(Configs.Power.Cruise, result.power);
  }

  [Test]
  public void TestDirectAttackBehaviorWaypoints() {
    // Create a sample direct attack behavior.
    var attackBehavior = new DirectAttackBehaviorLegacy(new Configs.AttackBehaviorConfig() {
      Name = "Sample Attack", Type = Configs.AttackType.DirectAttack,
      FlightPlan = new Configs.FlightPlan() { Type = Configs.FlightPlanType.DistanceToTarget,
                                              Waypoints = { new List<Configs.FlightPlanWaypoint>() {
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
                                                  Distance = 100,
                                                  Altitude = 25,
                                                  Power = Configs.Power.Max,
                                                }
                                              } } }
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
