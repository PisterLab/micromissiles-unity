using NUnit.Framework;
using UnityEngine;

public class DirectAttackBehaviorTests : TestBase {
  private AgentBase _agent;
  private DirectAttackBehavior _attackBehavior;
  private Vector3 _targetPosition = new Vector3(1000, 0, 0);
  private Vector3 _targetPositionWithNonzeroZ = new Vector3(800, 0, 600);

  [SetUp]
  public void SetUp() {
    _agent = new GameObject("Agent").AddComponent<AgentBase>();
    Rigidbody agentRb = _agent.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(_agent, "Awake");
    var attackBehaviorConfig = new Configs.AttackBehaviorConfig() {
      Name = "Direct Attack",
      Type = Configs.AttackType.DirectAttack,
      FlightPlan =
          new Configs.FlightPlan() {
            Type = Configs.FlightPlanType.DistanceToTarget,
            Waypoints =
                {
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
                  },
                },
          },
    };
    _attackBehavior = new DirectAttackBehavior(_agent, attackBehaviorConfig);
  }

  [Test]
  public void GetNextWaypoint_BeyondFarthestWaypoint() {
    // Distance to target is 1100.
    _agent.Position = new Vector3(-100, 0, 0);
    // The farthest waypoint is (distance = 1000, altitude = 100).
    (Vector3 waypoint, Configs.Power power) = _attackBehavior.GetNextWaypoint(_targetPosition);
    // Waypoint position is at target - direction * 1000 = 1000 - 1 * 1000 = 0 at altitude 100.
    Assert.AreEqual(new Vector3(0, 100, 0), waypoint);
    Assert.AreEqual(Configs.Power.Cruise, power);
  }

  [Test]
  public void GetNextWaypoint_AtFarthestWaypoint() {
    // Distance to target is 1000.
    _agent.Position = new Vector3(0, 0, 0);
    // There is a waypoint at distance = 1000, so the attack behavior chooses the next waypoint at
    // (distance = 500, altitude = 50).
    (Vector3 waypoint, Configs.Power power) = _attackBehavior.GetNextWaypoint(_targetPosition);
    // Waypoint position is at target - direction * 500 = 1000 - 1 * 500 = 500 at altitude 50.
    Assert.AreEqual(new Vector3(500, 50, 0), waypoint);
    Assert.AreEqual(Configs.Power.Mil, power);
  }

  [Test]
  public void GetNextWaypoint_BetweenWaypoints() {
    // Distance to target is 400.
    _agent.Position = new Vector3(600, 0, 0);
    // The next waypoint is at (distance = 100, altitude = 25).
    (Vector3 waypoint, Configs.Power power) = _attackBehavior.GetNextWaypoint(_targetPosition);
    // Waypoint position is at target - direction * 100 = 1000 - 1 * 100 = 900 at altitude 25.
    Assert.AreEqual(new Vector3(900, 25, 0), waypoint);
    Assert.AreEqual(Configs.Power.Max, power);
  }

  [Test]
  public void GetNextWaypoint_PastClosestWaypoint() {
    // Distance to target is 80.
    _agent.Position = new Vector3(920, 0, 0);
    // The closest waypoint is at (distance = 100, altitude = 25), so the attack behavior chooses
    // the target position as the next waypoint.
    (Vector3 waypoint, Configs.Power power) = _attackBehavior.GetNextWaypoint(_targetPosition);
    Assert.AreEqual(new Vector3(1000, 0, 0), waypoint);
    Assert.AreEqual(Configs.Power.Max, power);
  }

  [Test]
  public void GetNextWaypoint_AtFarthestWaypoint_WithNonzeroZ() {
    // Distance to target is sqrt(800^2 + 600^2) = 1000.
    _agent.Position = new Vector3(0, 0, 0);
    // There is a waypoint at distance = 1000, so the attack behavior chooses the next waypoint at
    // (distance = 500, altitude = 50).
    (Vector3 waypoint, Configs.Power power) =
        _attackBehavior.GetNextWaypoint(_targetPositionWithNonzeroZ);
    // Waypoint position is at target - direction * 500 = (800, 600) - (400, 300) = (400, 300) at
    // altitude 50.
    Assert.AreEqual(new Vector3(400, 50, 300), waypoint);
    Assert.AreEqual(Configs.Power.Mil, power);
  }

  [Test]
  public void GetNextWaypoint_BetweenWaypoints_WithNonzeroZ() {
    // Distance to target is sqrt(360^2 + 270^2) = 450.
    _agent.Position = new Vector3(440, 0, 330);
    // The next waypoint is at (distance = 100, altitude = 25).
    (Vector3 waypoint, Configs.Power power) =
        _attackBehavior.GetNextWaypoint(_targetPositionWithNonzeroZ);
    // Waypoint position is at target - direction * 100 = (800, 600) - (80, 60) = (720, 540) at
    // altitude 25.
    Assert.AreEqual(new Vector3(720, 25, 540), waypoint);
    Assert.AreEqual(Configs.Power.Max, power);
  }
}
