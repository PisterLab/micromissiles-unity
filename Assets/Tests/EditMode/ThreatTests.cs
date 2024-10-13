using System;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class ThreatTests : AgentTestBase {
  private FixedWingThreat fixedWingThreat;
  private RotaryWingThreat rotaryWingThreat;

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
    // Write the hard-coded JSON to the file
    string attackConfigPath = Path.Combine(Application.streamingAssetsPath,
                                           "Configs/Behaviors/Attack/test_direct_attack.json");
    Directory.CreateDirectory(Path.GetDirectoryName(attackConfigPath));
    File.WriteAllText(attackConfigPath, TestDirectAttackJson);
    // Load configurations using ConfigLoader
    // Create dynamic configurations for threats
    var ucavConfig = new DynamicAgentConfig {
      agent_model = "ucav.json", attack_behavior = "test_direct_attack.json",
      initial_state = new InitialState { position = new Vector3(2000, 100, 4000),
                                         rotation = new Vector3(90, 0, 0),
                                         velocity = new Vector3(-50, 0, -100) },
      standard_deviation = new StandardDeviation { position = new Vector3(400, 30, 400),
                                                   velocity = new Vector3(0, 0, 15) },
      dynamic_config =
          new DynamicConfig { launch_config = new LaunchConfig { launch_time = 0 },
                              sensor_config =
                                  new SensorConfig { type = SensorType.IDEAL, frequency = 100 } }
    };

    var quadcopterConfig = new DynamicAgentConfig {
      agent_model = "quadcopter.json", attack_behavior = "test_direct_attack.json",
      initial_state =
          new InitialState { position = new Vector3(0, 600, 6000), rotation = new Vector3(90, 0, 0),
                             velocity = new Vector3(0, 0, -50) },
      standard_deviation = new StandardDeviation { position = new Vector3(1000, 200, 100),
                                                   velocity = new Vector3(0, 0, 25) },
      dynamic_config =
          new DynamicConfig { launch_config = new LaunchConfig { launch_time = 0 },
                              sensor_config =
                                  new SensorConfig { type = SensorType.IDEAL, frequency = 100 } }
    };

    Agent threatAgent = CreateTestThreat(ucavConfig);
    Assert.IsNotNull(threatAgent);
    Assert.IsTrue(threatAgent is FixedWingThreat);
    fixedWingThreat = (FixedWingThreat)threatAgent;
    Assert.IsNotNull(fixedWingThreat);

    threatAgent = CreateTestThreat(quadcopterConfig);
    Assert.IsNotNull(threatAgent);
    Assert.IsTrue(threatAgent is RotaryWingThreat);
    rotaryWingThreat = (RotaryWingThreat)threatAgent;
    Assert.IsNotNull(rotaryWingThreat);
  }

  public override void Teardown() {
    base.Teardown();
    // Delete the attack configuration file
    string attackConfigPath = Path.Combine(Application.streamingAssetsPath,
                                           "Configs/Behaviors/Attack/test_direct_attack.json");
    if (File.Exists(attackConfigPath)) {
      File.Delete(attackConfigPath);
    }

    if (fixedWingThreat != null) {
      GameObject.DestroyImmediate(fixedWingThreat.gameObject);
    }

    if (rotaryWingThreat != null) {
      GameObject.DestroyImmediate(rotaryWingThreat.gameObject);
    }
  }

  [Test]
  public void TestDirectAttack_LoadedCorrectly() {
    // Arrange
    try {
      // Act
      DynamicAgentConfig config = new DynamicAgentConfig {
        agent_model = "ucav.json", attack_behavior = "test_direct_attack.json",
        initial_state = new InitialState(), standard_deviation = new StandardDeviation(),
        dynamic_config = new DynamicConfig()
      };

      Threat threat = CreateTestThreat(config);

      // Assert
      Assert.IsNotNull(threat, "Threat should not be null");

      AttackBehavior attackBehavior = GetPrivateField<AttackBehavior>(threat, "_attackBehavior");
      Assert.IsNotNull(attackBehavior, "Attack behavior should not be null");
      Assert.AreEqual("TestDirectAttack", attackBehavior.name);
      Assert.AreEqual(AttackBehavior.AttackBehaviorType.DIRECT_ATTACK,
                      attackBehavior.attackBehaviorType);

      Assert.IsTrue(attackBehavior is DirectAttackBehavior,
                    "Attack behavior should be a DirectAttackBehavior");
      DirectAttackBehavior directAttackBehavior = (DirectAttackBehavior)attackBehavior;

      Vector3 targetPosition = directAttackBehavior.targetPosition;
      Assert.AreEqual(new Vector3(0.01f, 0, 0), targetPosition);

      DTTFlightPlan flightPlan = directAttackBehavior.flightPlan;
      Assert.IsNotNull(flightPlan, "Flight plan should not be null");
      Assert.AreEqual("DistanceToTarget", flightPlan.type);

      List<DTTWaypoint> dttWaypoints = flightPlan.waypoints;
      Assert.IsNotNull(dttWaypoints, "Waypoints should not be null");
      Assert.AreEqual(2, dttWaypoints.Count, "There should be 2 waypoints");

      Assert.AreEqual(5000f, dttWaypoints[0].distance);
      Assert.AreEqual(100f, dttWaypoints[0].altitude);
      Assert.AreEqual(PowerSetting.MAX, dttWaypoints[0].power);

      Assert.AreEqual(10000f, dttWaypoints[1].distance);
      Assert.AreEqual(500f, dttWaypoints[1].altitude);
      Assert.AreEqual(PowerSetting.MIL, dttWaypoints[1].power);

      // Check targetVelocity
      Vector3 targetVelocity = directAttackBehavior.targetVelocity;
      Assert.AreEqual(new Vector3(0.0001f, 0f, 0f), targetVelocity,
                      "Target velocity should match the config");

      // Check targetColliderSize
      Vector3 targetColliderSize = directAttackBehavior.targetColliderSize;
      Assert.AreEqual(new Vector3(20f, 20f, 20f), targetColliderSize,
                      "Target collider size should match the config");

      // Check targetPosition (more precise check)
      Assert.AreEqual(0.01f, targetPosition.x, 0.0001f, "Target position X should be 0.01");
      Assert.AreEqual(0f, targetPosition.y, 0.0001f, "Target position Y should be 0");
      Assert.AreEqual(0f, targetPosition.z, 0.0001f, "Target position Z should be 0");

      // Clean up
      GameObject.DestroyImmediate(simManager.gameObject);
    } catch (AssertionException e) {
      throw new AssertionException(
          e.Message + "\n" + "This test likely failed because you have edited " +
          "The test string at the top of the test. Please update the test with the new values.\n" +
          "If you need to change the test values, please update the test string at the top of the test.");
    }
  }

  [Test]
  public void Threat_IsNotAssignable() {
    Assert.IsFalse(fixedWingThreat.IsAssignable());
    Assert.IsFalse(rotaryWingThreat.IsAssignable());
  }

  [Test]
  public void FixedWingThreat_CalculateAccelerationInput_RespectsMaxForwardAcceleration() {
    SetPrivateField(fixedWingThreat, "_currentWaypoint", Vector3.one * 1000f);
    SensorOutput sensorOutput =
        fixedWingThreat.GetComponent<Sensor>().SenseWaypoint(Vector3.one * 1000f);
    Vector3 acceleration =
        InvokePrivateMethod<Vector3>(fixedWingThreat, "CalculateAccelerationInput", sensorOutput);
    float maxForwardAcceleration =
        InvokePrivateMethod<float>(fixedWingThreat, "CalculateMaxForwardAcceleration");
    const float epsilon = 1e-5f;
    Assert.LessOrEqual((Vector3.Project(acceleration, fixedWingThreat.transform.forward)).magnitude,
                       maxForwardAcceleration + epsilon);
  }

  [Test]
  public void FixedWingThreat_CalculateAccelerationInput_RespectsMaxNormalAcceleration() {
    SetPrivateField(fixedWingThreat, "_currentWaypoint", Vector3.one * 1000f);
    SensorOutput sensorOutput =
        fixedWingThreat.GetComponent<Sensor>().SenseWaypoint(Vector3.one * 1000f);
    Vector3 acceleration =
        InvokePrivateMethod<Vector3>(fixedWingThreat, "CalculateAccelerationInput", sensorOutput);
    float maxNormalAcceleration =
        InvokePrivateMethod<float>(fixedWingThreat, "CalculateMaxNormalAcceleration");
    const float epsilon = 1e-5f;
    Assert.LessOrEqual(
        acceleration.magnitude, maxNormalAcceleration + epsilon,
        $"Acceleration magnitude {acceleration.magnitude} should be less than or equal to max normal acceleration {maxNormalAcceleration}");
  }

  [Test]
  public void RotaryWingThreat_CalculateAccelerationToWaypoint_RespectsMaxForwardAcceleration() {
    SetPrivateField(rotaryWingThreat, "_currentWaypoint", Vector3.one * 1000f);
    Vector3 acceleration =
        InvokePrivateMethod<Vector3>(rotaryWingThreat, "CalculateAccelerationToWaypoint");
    float maxForwardAcceleration =
        InvokePrivateMethod<float>(rotaryWingThreat, "CalculateMaxForwardAcceleration");
    const float epsilon = 1e-5f;
    Assert.LessOrEqual((Vector3.Project(acceleration, fixedWingThreat.transform.forward)).magnitude,
                       maxForwardAcceleration + epsilon);
  }

  [Test]
  public void RotaryWingThreat_CalculateAccelerationToWaypoint_RespectsMaxNormalAcceleration() {
    SetPrivateField(rotaryWingThreat, "_currentWaypoint", Vector3.one * 1000f);
    Vector3 acceleration =
        InvokePrivateMethod<Vector3>(rotaryWingThreat, "CalculateAccelerationToWaypoint");
    float maxNormalAcceleration =
        InvokePrivateMethod<float>(rotaryWingThreat, "CalculateMaxNormalAcceleration");
    const float epsilon = 1e-5f;
    Assert.LessOrEqual(
        (acceleration - Vector3.Project(acceleration, fixedWingThreat.transform.forward)).magnitude,
        maxNormalAcceleration + epsilon);
  }

  private class MockAttackBehavior : AttackBehavior {
    private Vector3 waypoint;
    private PowerSetting powerSetting;

    public MockAttackBehavior(Vector3 waypoint, PowerSetting powerSetting) {
      this.waypoint = waypoint;
      this.powerSetting = powerSetting;
      this.name = "MockAttackBehavior";
    }

    public override (Vector3, PowerSetting)
        GetNextWaypoint(Vector3 currentPosition, Vector3 targetPosition) {
      return (waypoint, powerSetting);
    }
  }

  [Test]
  public void RotaryWingThreat_CalculateAccelerationToWaypoint_ComputesCorrectly() {
    // Arrange
    Vector3 initialPosition = new Vector3(0, 0, 0);
    Vector3 waypoint = new Vector3(1000, 0, 0);
    Vector3 initialVelocity = new Vector3(0, 0, 0);
    float desiredSpeed = 50f;

    rotaryWingThreat.SetPosition(initialPosition);
    SetPrivateField(rotaryWingThreat, "_currentWaypoint", waypoint);
    rotaryWingThreat.SetVelocity(initialVelocity);
    SetPrivateField(rotaryWingThreat, "_currentPowerSetting", PowerSetting.MIL);

    // Assume PowerTableLookup returns 50 for PowerSetting.MIL
    // Assert that its true using PowerTableLookup
    float powerSetting =
        InvokePrivateMethod<float>(rotaryWingThreat, "PowerTableLookup", PowerSetting.MIL);
    Assert.AreEqual(desiredSpeed, powerSetting);

    // Act
    Vector3 accelerationInput =
        InvokePrivateMethod<Vector3>(rotaryWingThreat, "CalculateAccelerationToWaypoint");

    // Assert
    Vector3 toWaypoint = waypoint - initialPosition;
    Vector3 expectedAccelerationDir = toWaypoint.normalized;
    float expectedAccelerationMag = desiredSpeed / (float)Time.fixedDeltaTime;
    Vector3 expectedAcceleration = expectedAccelerationDir * expectedAccelerationMag;

    // Decompose acceleration into forward and normal acceleration components
    Vector3 forwardAcceleration =
        Vector3.Project(expectedAcceleration, rotaryWingThreat.transform.forward);
    Vector3 normalAcceleration = expectedAcceleration - forwardAcceleration;

    // Limit acceleration magnitude
    float maxForwardAcceleration =
        InvokePrivateMethod<float>(rotaryWingThreat, "CalculateMaxForwardAcceleration");
    forwardAcceleration = Vector3.ClampMagnitude(forwardAcceleration, maxForwardAcceleration);
    float maxNormalAcceleration =
        InvokePrivateMethod<float>(rotaryWingThreat, "CalculateMaxNormalAcceleration");
    normalAcceleration = Vector3.ClampMagnitude(normalAcceleration, maxNormalAcceleration);
    expectedAcceleration = forwardAcceleration + normalAcceleration;

    Assert.AreEqual(expectedAcceleration.magnitude, accelerationInput.magnitude, 0.1f,
                    "Acceleration magnitude should match expected");
    Assert.AreEqual(expectedAcceleration.normalized, accelerationInput.normalized,
                    "Acceleration direction should be towards waypoint");
  }
}
