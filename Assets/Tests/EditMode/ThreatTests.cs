using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ThreatTests : AgentTestBase {
  private FixedWingThreatLegacy _fixedWingThreat;
  private RotaryWingThreatLegacy _rotaryWingThreat;

  private const string TestDirectAttackPbtxt =
      @"
      name: ""Test Direct Attack""
      type: DIRECT_ATTACK
      flight_plan {
        type: DISTANCE_TO_TARGET
        waypoints {
          distance: 10000
          altitude: 500
          power: MIL
        }
        waypoints {
          distance: 5000
          altitude: 100
          power: MAX
        }
      }
    ";

  public override void Setup() {
    base.Setup();
    // Write the hard-coded attack behavior to a file.
    string attackConfigPath =
        ConfigLoader.GetStreamingAssetsFilePath("Configs/Attacks/test_direct_attack.pbtxt");
    Directory.CreateDirectory(Path.GetDirectoryName(attackConfigPath));
    File.WriteAllText(attackConfigPath, TestDirectAttackPbtxt);

    var ucavConfig = new Configs.AgentConfig() {
      ConfigFile = "ucav.pbtxt", AttackBehaviorConfigFile = "test_direct_attack.pbtxt",
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

    var quadcopterConfig = new Configs.AgentConfig() {
      ConfigFile = "quadcopter.pbtxt", AttackBehaviorConfigFile = "test_direct_attack.pbtxt",
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

    Agent threatAgent = CreateTestThreat(ucavConfig);
    Assert.IsNotNull(threatAgent);
    Assert.IsTrue(threatAgent is FixedWingThreatLegacy);
    _fixedWingThreat = (FixedWingThreatLegacy)threatAgent;
    Assert.IsNotNull(_fixedWingThreat);

    threatAgent = CreateTestThreat(quadcopterConfig);
    Assert.IsNotNull(threatAgent);
    Assert.IsTrue(threatAgent is RotaryWingThreatLegacy);
    _rotaryWingThreat = (RotaryWingThreatLegacy)threatAgent;
    Assert.IsNotNull(_rotaryWingThreat);
  }

  public override void Teardown() {
    base.Teardown();
    // Delete the attack configuration file.
    string attackConfigPath =
        ConfigLoader.GetStreamingAssetsFilePath("Configs/Attacks/test_direct_attack.pbtxt");
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
    Assert.LessOrEqual(Vector3.Project(acceleration, _fixedWingThreat.transform.forward).magnitude,
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
        $"Acceleration magnitude {acceleration.magnitude} should be less than or equal to the maximum normal acceleration {maxNormalAcceleration}.");
  }

  [Test]
  public void RotaryWingThreat_CalculateAccelerationToWaypoint_RespectsMaxForwardAcceleration() {
    SetPrivateField(_rotaryWingThreat, "_currentWaypoint", Vector3.one * 1000f);
    Vector3 acceleration =
        InvokePrivateMethod<Vector3>(_rotaryWingThreat, "CalculateAccelerationToWaypoint");
    float maxForwardAcceleration = _rotaryWingThreat.CalculateMaxForwardAcceleration();
    const float epsilon = 1e-5f;
    Assert.LessOrEqual(Vector3.Project(acceleration, _fixedWingThreat.transform.forward).magnitude,
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
        $"Normal acceleration magnitude {normalComponent.magnitude} should be less than or equal to the maximum normal acceleration {maxNormalAcceleration}.");
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

  [Test]
  public void AttackBehavior_LoadedCorrectly() {
    try {
      AttackBehavior attackBehavior =
          GetPrivateField<AttackBehavior>(_fixedWingThreat, "_attackBehavior");
      Assert.IsNotNull(attackBehavior, "Attack behavior should not be null.");
      Assert.AreEqual("Test Direct Attack", attackBehavior.Name);
      Assert.AreEqual(Configs.AttackType.DirectAttack, attackBehavior.Type);

      Assert.IsTrue(attackBehavior is DirectAttackBehaviorLegacy,
                    "Attack behavior should be a DirectAttackBehaviorLegacy.");
      var directAttackBehavior = (DirectAttackBehaviorLegacy)attackBehavior;

      Assert.IsNotNull(attackBehavior.FlightPlan, "Flight plan should not be null.");
      Assert.AreEqual(Configs.FlightPlanType.DistanceToTarget,
                      attackBehavior.FlightPlan.Config.Type);

      var waypoints = attackBehavior.FlightPlan.Waypoints;
      Assert.IsNotNull(waypoints, "Waypoints should not be null.");
      Assert.AreEqual(2, waypoints.Count, "There should be 2 waypoints.");

      Assert.AreEqual(10000f, waypoints[0].Distance);
      Assert.AreEqual(500f, waypoints[0].Altitude);
      Assert.AreEqual(Configs.Power.Mil, waypoints[0].Power);

      Assert.AreEqual(5000f, waypoints[1].Distance);
      Assert.AreEqual(100f, waypoints[1].Altitude);
      Assert.AreEqual(Configs.Power.Max, waypoints[1].Power);

      GameObject.DestroyImmediate(_simManager.gameObject);
    } catch (AssertionException e) {
      throw new AssertionException(
          e.Message + "\n" + "This test likely failed because you have edited " +
          "the test string at the top of the test. Please update the test with the new values.\n" +
          "If you need to change the test values, please update the test string at the top of the test.");
    }
  }
}
