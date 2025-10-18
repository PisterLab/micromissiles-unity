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
    _agent.Position = new Vector3(-100, 0, 0);
    (Vector3 waypoint, Configs.Power power) = _attackBehavior.GetNextWaypoint(_targetPosition);
    Assert.AreEqual(new Vector3(0, 100, 0), waypoint);
    Assert.AreEqual(Configs.Power.Cruise, power);
  }

  [Test]
  public void GetNextWaypoint_AtFarthestWaypoint() {
    _agent.Position = new Vector3(0, 0, 0);
    (Vector3 waypoint, Configs.Power power) = _attackBehavior.GetNextWaypoint(_targetPosition);
    Assert.AreEqual(new Vector3(500, 50, 0), waypoint);
    Assert.AreEqual(Configs.Power.Mil, power);
  }

  [Test]
  public void GetNextWaypoint_BetweenWaypoints() {
    _agent.Position = new Vector3(600, 0, 0);
    (Vector3 waypoint, Configs.Power power) = _attackBehavior.GetNextWaypoint(_targetPosition);
    Assert.AreEqual(new Vector3(900, 25, 0), waypoint);
    Assert.AreEqual(Configs.Power.Max, power);
  }

  [Test]
  public void GetNextWaypoint_PastClosestWaypoint() {
    _agent.Position = new Vector3(920, 0, 0);
    (Vector3 waypoint, Configs.Power power) = _attackBehavior.GetNextWaypoint(_targetPosition);
    Assert.AreEqual(new Vector3(1000, 0, 0), waypoint);
    Assert.AreEqual(Configs.Power.Max, power);
  }

  [Test]
  public void GetNextWaypoint_AtFarthestWaypoint_WithNonzeroZ() {
    _agent.Position = new Vector3(0, 0, 0);
    (Vector3 waypoint, Configs.Power power) =
        _attackBehavior.GetNextWaypoint(_targetPositionWithNonzeroZ);
    Assert.AreEqual(new Vector3(400, 50, 300), waypoint);
    Assert.AreEqual(Configs.Power.Mil, power);
  }

  [Test]
  public void GetNextWaypoint_BetweenWaypoints_WithNonzeroZ() {
    _agent.Position = new Vector3(440, 0, 330);
    (Vector3 waypoint, Configs.Power power) =
        _attackBehavior.GetNextWaypoint(_targetPositionWithNonzeroZ);
    Assert.AreEqual(new Vector3(720, 25, 540), waypoint);
    Assert.AreEqual(Configs.Power.Max, power);
  }
}
