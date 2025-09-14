using System;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class ThreatTests : AgentTestBase {
  private FixedWingThreat _fixedWingThreat;
  private RotaryWingThreat _rotaryWingThreat;

  private const string TestDirectAttackJson =
      @"
    {
        ""name"": ""TestDirectAttack"",
        ""attackBehaviorType"": ""DIRECT_ATTACK"",
        ""targetPosition"": {
            ""x"": 0.01,
            ""y"": 0.0,
            ""z"": 0.0
        },
        ""targetVelocity"": {
            ""x"": 0.0001,
            ""y"": 0.0,
            ""z"": 0.0
        },
        ""targetColliderSize"": {
            ""x"": 20.0,
            ""y"": 20.0,
            ""z"": 20.0
        },
        ""flightPlan"": {
            ""type"": ""DistanceToTarget"",
            ""waypoints"": [
                {
                    ""distance"": 10000.0,
                    ""altitude"": 500.0,
                    ""power"": ""MIL""
                },
                {
                    ""distance"": 5000.0,
                    ""altitude"": 100.0,
                    ""power"": ""MAX""
                }
            ]
        }
    }
    ";

  public override void Setup() {
    base.Setup();
    // Write the hard-coded attack behavior to a file.
    string attackConfigPath =
        ConfigLoader.GetStreamingAssetsFilePath("Configs/Behaviors/Attack/test_direct_attack.json");
    Directory.CreateDirectory(Path.GetDirectoryName(attackConfigPath));
    File.WriteAllText(attackConfigPath, TestDirectAttackJson);

    var ucavConfig = new Configs.AgentConfig() {
      ThreatType = Configs.ThreatType.Ucav, AttackBehavior = "test_direct_attack.json",
      InitialState =
          new Simulation.State() {
            Position = new Simulation.CartesianCoordinates() { X = 2000, Y = 100, Z = 4000 },
            Velocity = new Simulation.CartesianCoordinates() { X = -50, Y = 0, Z = -100 },
          },
      StandardDeviation =
          new Simulation.State() {
            Position = new Simulation.CartesianCoordinates() { X = 400, Y = 300, Z = 400 },
            Velocity = new Simulation.CartesianCoordinates() { X = 0, Y = 0, Z = 15 },
          },
      DynamicConfig =
          new Configs.DynamicConfig() {
            SensorConfig =
                new Simulation.SensorConfig {
                  Type = Simulation.SensorType.Ideal,
                  Frequency = 100,
                },
          }
    };
    ProtobufInitializer.Initialize(ucavConfig);

    var quadcopterConfig = new Configs.AgentConfig() {
      ThreatType = Configs.ThreatType.Quadcopter, AttackBehavior = "test_direct_attack.json",
      InitialState =
          new Simulation.State() {
            Position = new Simulation.CartesianCoordinates() { X = 0, Y = 600, Z = 6000 },
            Velocity = new Simulation.CartesianCoordinates() { X = 0, Y = 0, Z = -50 },
          },
      StandardDeviation =
          new Simulation.State() {
            Position = new Simulation.CartesianCoordinates() { X = 100, Y = 200, Z = 100 },
            Velocity = new Simulation.CartesianCoordinates() { X = 0, Y = 0, Z = 25 },
          },
      DynamicConfig =
          new Configs.DynamicConfig() {
            SensorConfig =
                new Simulation.SensorConfig {
                  Type = Simulation.SensorType.Ideal,
                  Frequency = 100,
                },
          }
    };
    ProtobufInitializer.Initialize(quadcopterConfig);

    Agent threatAgent = CreateTestThreat(ucavConfig);
    Assert.IsNotNull(threatAgent);
    Assert.IsTrue(threatAgent is FixedWingThreat);
    _fixedWingThreat = (FixedWingThreat)threatAgent;
    Assert.IsNotNull(_fixedWingThreat);

    threatAgent = CreateTestThreat(quadcopterConfig);
    Assert.IsNotNull(threatAgent);
    Assert.IsTrue(threatAgent is RotaryWingThreat);
    _rotaryWingThreat = (RotaryWingThreat)threatAgent;
    Assert.IsNotNull(_rotaryWingThreat);
  }

  public override void Teardown() {
    base.Teardown();
    // Delete the attack configuration file.
    string attackConfigPath =
        ConfigLoader.GetStreamingAssetsFilePath("Configs/Behaviors/Attack/test_direct_attack.json");
    if (File.Exists(attackConfigPath)) {
      File.Delete(attackConfigPath);
    }

    if (_fixedWingThreat != null) {
      GameObject.DestroyImmediate(_fixedWingThreat.gameObject);
    }

    if (_rotaryWingThreat != null) {
      GameObject.DestroyImmediate(_rotaryWingThreat.gameObject);
    }
  }

  [Test]
  public void TestDirectAttack_LoadedCorrectly() {
    // Arrange
    try {
      var config = new Configs.AgentConfig() {
        ThreatType = Configs.ThreatType.Ucav,
        AttackBehavior = "test_direct_attack.json",
      };
      ProtobufInitializer.Initialize(config);

      Threat threat = CreateTestThreat(config);
      Assert.IsNotNull(threat, "Threat should not be null.");

      AttackBehavior attackBehavior = GetPrivateField<AttackBehavior>(threat, "_attackBehavior");
      Assert.IsNotNull(attackBehavior, "Attack behavior should not be null.");
      Assert.AreEqual("TestDirectAttack", attackBehavior.name);
      Assert.AreEqual(AttackBehavior.AttackBehaviorType.DIRECT_ATTACK,
                      attackBehavior.attackBehaviorType);

      Assert.IsTrue(attackBehavior is DirectAttackBehavior,
                    "Attack behavior should be a DirectAttackBehavior.");
      DirectAttackBehavior directAttackBehavior = (DirectAttackBehavior)attackBehavior;

      Vector3 targetPosition = directAttackBehavior.targetPosition;
      Assert.AreEqual(new Vector3(0.01f, 0, 0), targetPosition);

      DTTFlightPlan flightPlan = directAttackBehavior.flightPlan;
      Assert.IsNotNull(flightPlan, "Flight plan should not be null.");
      Assert.AreEqual("DistanceToTarget", flightPlan.type);

      List<DTTWaypoint> dttWaypoints = flightPlan.waypoints;
      Assert.IsNotNull(dttWaypoints, "Waypoints should not be null.");
      Assert.AreEqual(2, dttWaypoints.Count, "There should be 2 waypoints.");

      Assert.AreEqual(5000f, dttWaypoints[0].distance);
      Assert.AreEqual(100f, dttWaypoints[0].altitude);
      Assert.AreEqual(Configs.Power.Max, dttWaypoints[0].power);

      Assert.AreEqual(10000f, dttWaypoints[1].distance);
      Assert.AreEqual(500f, dttWaypoints[1].altitude);
      Assert.AreEqual(Configs.Power.Mil, dttWaypoints[1].power);

      // Check the target velocity.
      Vector3 targetVelocity = directAttackBehavior.targetVelocity;
      Assert.AreEqual(new Vector3(0.0001f, 0f, 0f), targetVelocity,
                      "Target velocity should match the config.");

      // Check the target collider size.
      Vector3 targetColliderSize = directAttackBehavior.targetColliderSize;
      Assert.AreEqual(new Vector3(20f, 20f, 20f), targetColliderSize,
                      "Target collider size should match the config.");

      // Check the target position.
      Assert.AreEqual(0.01f, targetPosition.x, 0.0001f, "Target position X should be 0.01.");
      Assert.AreEqual(0f, targetPosition.y, 0.0001f, "Target position Y should be 0.");
      Assert.AreEqual(0f, targetPosition.z, 0.0001f, "Target position Z should be 0.");

      GameObject.DestroyImmediate(simManager.gameObject);
    } catch (AssertionException e) {
      throw new AssertionException(
          e.Message + "\n" + "This test likely failed because you have edited " +
          "the test string at the top of the test. Please update the test with the new values.\n" +
          "If you need to change the test values, please update the test string at the top of the test.");
    }
  }

  [Test]
  public void Threat_IsNotAssignable() {
    Assert.IsFalse(_fixedWingThreat.IsAssignable());
    Assert.IsFalse(_rotaryWingThreat.IsAssignable());
  }

  [Test]
  public void FixedWingThreat_CalculateAccelerationInput_RespectsMaxForwardAcceleration() {
    SetPrivateField(_fixedWingThreat, "_currentWaypoint", Vector3.one * 1000f);
    Vector3 acceleration =
        InvokePrivateMethod<Vector3>(_fixedWingThreat, "CalculateAccelerationInput");
    float maxForwardAcceleration = _fixedWingThreat.CalculateMaxForwardAcceleration();
    const float epsilon = 1e-5f;
    Assert.LessOrEqual(
        (Vector3.Project(acceleration, _fixedWingThreat.transform.forward)).magnitude,
        maxForwardAcceleration + epsilon);
  }

  [Test]
  public void FixedWingThreat_CalculateAccelerationInput_RespectsMaxNormalAcceleration() {
    SetPrivateField(_fixedWingThreat, "_currentWaypoint", Vector3.one * 1000f);
    Vector3 acceleration =
        InvokePrivateMethod<Vector3>(_fixedWingThreat, "CalculateAccelerationInput");
    float maxNormalAcceleration = _fixedWingThreat.CalculateMaxNormalAcceleration();
    const float epsilon = 1e-5f;
    Assert.LessOrEqual(
        acceleration.magnitude, maxNormalAcceleration + epsilon,
        $"Acceleration magnitude {acceleration.magnitude} should be less than or equal to max normal acceleration {maxNormalAcceleration}.");
  }

  [Test]
  public void RotaryWingThreat_CalculateAccelerationToWaypoint_RespectsMaxForwardAcceleration() {
    SetPrivateField(_rotaryWingThreat, "_currentWaypoint", Vector3.one * 1000f);
    Vector3 acceleration =
        InvokePrivateMethod<Vector3>(_rotaryWingThreat, "CalculateAccelerationToWaypoint");
    float maxForwardAcceleration = _rotaryWingThreat.CalculateMaxForwardAcceleration();
    const float epsilon = 1e-5f;
    Assert.LessOrEqual(
        (Vector3.Project(acceleration, _fixedWingThreat.transform.forward)).magnitude,
        maxForwardAcceleration + epsilon);
  }

  [Test]
  public void RotaryWingThreat_CalculateAccelerationToWaypoint_RespectsMaxNormalAcceleration() {
    SetPrivateField(_rotaryWingThreat, "_currentWaypoint", Vector3.one * 1000f);
    Vector3 acceleration =
        InvokePrivateMethod<Vector3>(_rotaryWingThreat, "CalculateAccelerationToWaypoint");
    float maxNormalAcceleration = _rotaryWingThreat.CalculateMaxNormalAcceleration();
    const float epsilon = 1e-5f;
    // Calculate the normal acceleration.
    Vector3 forwardComponent = Vector3.Project(acceleration, _rotaryWingThreat.transform.forward);
    Vector3 normalComponent = acceleration - forwardComponent;

    Assert.LessOrEqual(
        normalComponent.magnitude, maxNormalAcceleration + epsilon,
        $"Normal acceleration magnitude {normalComponent.magnitude} should be less than or equal to max normal acceleration {maxNormalAcceleration}.");
  }

  private class MockAttackBehavior : AttackBehavior {
    private Vector3 waypoint;
    private Configs.Power power;

    public MockAttackBehavior(Vector3 waypoint, Configs.Power power) {
      this.waypoint = waypoint;
      this.power = power;
      this.name = "MockAttackBehavior";
    }

    public override (Vector3, Configs.Power)
        GetNextWaypoint(Vector3 currentPosition, Vector3 targetPosition) {
      return (waypoint, power);
    }
  }

  [Test]
  public void RotaryWingThreat_CalculateAccelerationToWaypoint_ComputesCorrectly() {
    Vector3 initialPosition = new Vector3(0, 0, 0);
    Vector3 waypoint = new Vector3(1000, 0, 0);
    Vector3 initialVelocity = new Vector3(0, 0, 0);
    float desiredSpeed = 50f;

    _rotaryWingThreat.SetPosition(initialPosition);
    SetPrivateField(_rotaryWingThreat, "_currentWaypoint", waypoint);
    _rotaryWingThreat.SetVelocity(initialVelocity);
    SetPrivateField(_rotaryWingThreat, "_currentPower", Configs.Power.Mil);

    // Assume that the lookup power table returns 50 for MIL.
    float power =
        InvokePrivateMethod<float>(_rotaryWingThreat, "LookupPowerTable", Configs.Power.Mil);
    Assert.AreEqual(desiredSpeed, power);

    Vector3 accelerationInput =
        InvokePrivateMethod<Vector3>(_rotaryWingThreat, "CalculateAccelerationToWaypoint");

    Vector3 toWaypoint = waypoint - initialPosition;
    Vector3 expectedAccelerationDir = toWaypoint.normalized;
    float expectedAccelerationMag = desiredSpeed / (float)Time.fixedDeltaTime;
    Vector3 expectedAcceleration = expectedAccelerationDir * expectedAccelerationMag;

    // Decompose acceleration into forward and normal components.
    Vector3 forwardAcceleration =
        Vector3.Project(expectedAcceleration, _rotaryWingThreat.transform.forward);
    Vector3 normalAcceleration = expectedAcceleration - forwardAcceleration;

    // Limit the acceleration magnitude.
    float maxForwardAcceleration = _rotaryWingThreat.CalculateMaxNormalAcceleration();
    forwardAcceleration = Vector3.ClampMagnitude(forwardAcceleration, maxForwardAcceleration);
    float maxNormalAcceleration = _rotaryWingThreat.CalculateMaxNormalAcceleration();
    normalAcceleration = Vector3.ClampMagnitude(normalAcceleration, maxNormalAcceleration);
    expectedAcceleration = forwardAcceleration + normalAcceleration;

    Assert.AreEqual(expectedAcceleration.magnitude, accelerationInput.magnitude, 0.1f,
                    "Acceleration magnitude should match expected.");
    Assert.AreEqual(expectedAcceleration.normalized, accelerationInput.normalized,
                    "Acceleration direction should be towards waypoint.");
  }
}
