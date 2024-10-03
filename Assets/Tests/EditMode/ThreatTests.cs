using System;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ThreatTests : AgentTestBase
{
    private FixedWingThreat fixedWingThreat;
    private RotaryWingThreat rotaryWingThreat;

    public override void Setup()
    {
        base.Setup();
        // Load configurations using ConfigLoader
        // Create dynamic configurations for threats
        var ucavConfig = new DynamicAgentConfig
        {
            agent_model = "ucav.json",
            attack_behavior = "default_direct_attack.json",
            initial_state = new InitialState
            {
                position = new Vector3(2000, 100, 4000),
                rotation = new Vector3(90, 0, 0),
                velocity = new Vector3(-50, 0, -100)
            },
            standard_deviation = new StandardDeviation
            {
                position = new Vector3(400, 30, 400),
                velocity = new Vector3(0, 0, 15)
            },
            dynamic_config = new DynamicConfig
            {
                launch_config = new LaunchConfig { launch_time = 0 },
                sensor_config = new SensorConfig { type = SensorType.IDEAL, frequency = 100 }
            }
        };

        var quadcopterConfig = new DynamicAgentConfig
        {
            agent_model = "quadcopter.json",
            attack_behavior = "default_direct_attack.json",
            initial_state = new InitialState
            {
                position = new Vector3(0, 600, 6000),
                rotation = new Vector3(90, 0, 0),
                velocity = new Vector3(0, 0, -50)
            },
            standard_deviation = new StandardDeviation
            {
                position = new Vector3(1000, 200, 100),
                velocity = new Vector3(0, 0, 25)
            },
            dynamic_config = new DynamicConfig
            {
                launch_config = new LaunchConfig { launch_time = 0 },
                sensor_config = new SensorConfig { type = SensorType.IDEAL, frequency = 100 }
            }
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

    public override void Teardown()
    {
        base.Teardown();

        if (fixedWingThreat != null)
        {
            GameObject.DestroyImmediate(fixedWingThreat.gameObject);
        }

        if (rotaryWingThreat != null)
        {
            GameObject.DestroyImmediate(rotaryWingThreat.gameObject);
        }
    }

    [Test]
    public void DefaultDirectAttack_LoadedCorrectly()
    {
        // Arrange


        // Act
        DynamicAgentConfig config = new DynamicAgentConfig
        {
            agent_model = "ucav.json",
            attack_behavior = "default_direct_attack.json",
            initial_state = new InitialState(),
            standard_deviation = new StandardDeviation(),
            dynamic_config = new DynamicConfig()
        };

        Threat threat = CreateTestThreat(config);

        // Assert
        Assert.IsNotNull(threat, "Threat should not be null");

        AttackBehavior attackBehavior = GetPrivateField<AttackBehavior>(threat, "_attackBehavior");
        Assert.IsNotNull(attackBehavior, "Attack behavior should not be null");
        Assert.AreEqual("DefaultDirectAttack", attackBehavior.name);
        Assert.AreEqual(AttackBehavior.AttackBehaviorType.DIRECT_ATTACK, attackBehavior.attackBehaviorType);

        Assert.IsTrue(attackBehavior is DirectAttackBehavior, "Attack behavior should be a DirectAttackBehavior");
        DirectAttackBehavior directAttackBehavior = (DirectAttackBehavior)attackBehavior;

        Vector3 targetPosition = directAttackBehavior.targetPosition;
        Assert.AreEqual(new Vector3(0, 0, 0), targetPosition);

        DTTFlightPlan flightPlan = directAttackBehavior.flightPlan;
        Assert.IsNotNull(flightPlan, "Flight plan should not be null");
        Assert.AreEqual("DistanceToTarget", flightPlan.type);


        List<DTTWaypoint> dttWaypoints = flightPlan.waypoints;
        Assert.IsNotNull(dttWaypoints, "Waypoints should not be null");
        Assert.AreEqual(2, dttWaypoints.Count, "There should be 2 waypoints");

        Assert.AreEqual(10000f, dttWaypoints[0].distance);
        Assert.AreEqual(500f, dttWaypoints[0].altitude);
        Assert.AreEqual(PowerSetting.MIL, dttWaypoints[0].power);

        Assert.AreEqual(1000f, dttWaypoints[1].distance);
        Assert.AreEqual(100f, dttWaypoints[1].altitude);
        Assert.AreEqual(PowerSetting.MAX, dttWaypoints[1].power);

        // Check targetVelocity
        Vector3 targetVelocity = directAttackBehavior.targetVelocity;
        Assert.AreEqual(new Vector3(0.0001f, 0f, 0f), targetVelocity, "Target velocity should match the config");

        // Check targetColliderSize
        Vector3 targetColliderSize = directAttackBehavior.targetColliderSize;
        Assert.AreEqual(new Vector3(20f, 20f, 20f), targetColliderSize, "Target collider size should match the config");

        // Check targetPosition (more precise check)
        Assert.AreEqual(0.01f, targetPosition.x, 0.0001f, "Target position X should be 0.01");
        Assert.AreEqual(0f, targetPosition.y, 0.0001f, "Target position Y should be 0");
        Assert.AreEqual(0f, targetPosition.z, 0.0001f, "Target position Z should be 0");

        // Clean up
        GameObject.DestroyImmediate(simManager.gameObject);
    }

    [Test]
    public void Threat_IsNotAssignable()
    {
        Assert.IsFalse(fixedWingThreat.IsAssignable());
        Assert.IsFalse(rotaryWingThreat.IsAssignable());
    }

    [Test]
    public void FixedWingThreat_CalculateAccelerationCommand_RespectsMaxAcceleration()
    {
        SetPrivateField(fixedWingThreat, "_currentWaypoint", Vector3.one * 1000f);
        Vector3 acceleration = InvokePrivateMethod<Vector3>(fixedWingThreat, "CalculateAccelerationCommand");
        float maxAcceleration = InvokePrivateMethod<float>(fixedWingThreat, "CalculateMaxAcceleration");
        Assert.LessOrEqual(acceleration.magnitude, maxAcceleration);
    }

    [Test]
    public void RotaryWingThreat_CalculateAccelerationToWaypoint_RespectsMaxAcceleration()
    {
        SetPrivateField(rotaryWingThreat, "_currentWaypoint", Vector3.one * 1000f);
        Vector3 acceleration = InvokePrivateMethod<Vector3>(rotaryWingThreat, "CalculateAccelerationToWaypoint");
        float maxAcceleration = InvokePrivateMethod<float>(rotaryWingThreat, "CalculateMaxAcceleration");
        Assert.LessOrEqual(acceleration.magnitude, maxAcceleration);
    }

    [Test]
    public void FixedWingThreat_CalculateAccelerationCommand_ReturnsZeroWithSimplifiedLOSRate()
    {
        // Arrange
        Vector3 initialPosition = new Vector3(0, 0, 0);
        Vector3 waypoint = new Vector3(1000, 0, 0);
        Vector3 initialVelocity = new Vector3(100, 0, 0);

        fixedWingThreat.transform.position = initialPosition;
        SetPrivateField(fixedWingThreat, "_currentWaypoint", waypoint);
        SetVelocity(fixedWingThreat, initialVelocity);

        // Act
        Vector3 accelerationCommand = InvokePrivateMethod<Vector3>(fixedWingThreat, "CalculateAccelerationCommand");

        // Assert
        Assert.IsNotNull(accelerationCommand);
        Assert.AreEqual(Vector3.zero, accelerationCommand, "Acceleration command should be zero with simplified LOS rate");
    }

    private class MockAttackBehavior : AttackBehavior
    {
        private Vector3 waypoint;
        private PowerSetting powerSetting;

        public MockAttackBehavior(Vector3 waypoint, PowerSetting powerSetting)
        {
            this.waypoint = waypoint;
            this.powerSetting = powerSetting;
            this.name = "MockAttackBehavior";
        }

        public override (Vector3, PowerSetting) GetNextWaypoint(Vector3 currentPosition, Vector3 targetPosition)
        {
            return (waypoint, powerSetting);
        }
    }

    [Test]
    public void FixedWingThreat_UpdateMidCourse_MovesTowardsWaypoint()
    {
        // Arrange
        Vector3 initialPosition = new Vector3(0, 0, 0);
        Vector3 waypoint = new Vector3(1000, 0, 0);
        Vector3 initialVelocity = new Vector3(100, 0, 0);

        fixedWingThreat.transform.position = initialPosition;
        SetVelocity(fixedWingThreat, initialVelocity);

        // Use MockAttackBehavior
        MockAttackBehavior mockAttackBehavior = new MockAttackBehavior(waypoint, PowerSetting.MIL);
        SetPrivateField(fixedWingThreat, "_attackBehavior", mockAttackBehavior);

        // Set target
        GameObject targetObject = new GameObject("Target");
        targetObject.transform.position = new Vector3(2000, 0, 0);
        SetPrivateField(fixedWingThreat, "_target", targetObject.transform);

        // Act
        double deltaTime = Time.fixedDeltaTime;
        InvokePrivateMethod(fixedWingThreat, "UpdateMidCourse", deltaTime);

        // Assert
        Vector3 newPosition = fixedWingThreat.transform.position;
        float initialDistance = Vector3.Distance(initialPosition, waypoint);
        float newDistance = Vector3.Distance(newPosition, waypoint);
        Assert.Less(newDistance, initialDistance, "Threat should move closer to the waypoint");
    }

    [Test]
    public void RotaryWingThreat_CalculateAccelerationToWaypoint_ComputesCorrectly()
    {
        // Arrange
        Vector3 initialPosition = new Vector3(0, 0, 0);
        Vector3 waypoint = new Vector3(1000, 0, 0);
        Vector3 initialVelocity = new Vector3(0, 0, 0);
        float desiredSpeed = 50f;

        rotaryWingThreat.transform.position = initialPosition;
        SetPrivateField(rotaryWingThreat, "_currentWaypoint", waypoint);
        SetVelocity(rotaryWingThreat, initialVelocity);
        SetPrivateField(rotaryWingThreat, "_currentPowerSetting", PowerSetting.MIL);

        // Assume PowerTableLookup returns 50 for PowerSetting.MIL
        SetPrivateField(rotaryWingThreat, "_powerSettings", new Dictionary<PowerSetting, float>
        {
            { PowerSetting.MIL, desiredSpeed }
        });

        // Act
        Vector3 accelerationCommand = InvokePrivateMethod<Vector3>(rotaryWingThreat, "CalculateAccelerationToWaypoint");

        // Assert
        Vector3 toWaypoint = waypoint - initialPosition;
        Vector3 expectedAccelerationDir = toWaypoint.normalized;
        float expectedAccelerationMag = desiredSpeed / (float)Time.fixedDeltaTime;

        float maxAcceleration = InvokePrivateMethod<float>(rotaryWingThreat, "CalculateMaxAcceleration");
        expectedAccelerationMag = Mathf.Min(expectedAccelerationMag, maxAcceleration);

        Vector3 expectedAcceleration = expectedAccelerationDir * expectedAccelerationMag;

        Assert.AreEqual(expectedAcceleration.magnitude, accelerationCommand.magnitude, 0.1f, "Acceleration magnitude should match expected");
        Assert.AreEqual(expectedAcceleration.normalized, accelerationCommand.normalized, "Acceleration direction should be towards waypoint");
    }

    [Test]
    public void RotaryWingThreat_UpdateMidCourse_MovesTowardsWaypoint()
    {
        // Arrange
        Vector3 initialPosition = new Vector3(0, 0, 0);
        Vector3 waypoint = new Vector3(1000, 0, 0);
        Vector3 initialVelocity = Vector3.zero;

        rotaryWingThreat.transform.position = initialPosition;
        SetVelocity(rotaryWingThreat, initialVelocity);

        // Use MockAttackBehavior
        MockAttackBehavior mockAttackBehavior = new MockAttackBehavior(waypoint, PowerSetting.MIL);
        SetPrivateField(rotaryWingThreat, "_attackBehavior", mockAttackBehavior);

        // Set target
        GameObject targetObject = new GameObject("Target");
        targetObject.transform.position = new Vector3(2000, 0, 0);
        SetPrivateField(rotaryWingThreat, "_target", targetObject.transform);

        // Act
        double deltaTime = Time.fixedDeltaTime;
        InvokePrivateMethod(rotaryWingThreat, "UpdateMidCourse", deltaTime);

        // Assert
        Vector3 newPosition = rotaryWingThreat.transform.position;
        float initialDistance = Vector3.Distance(initialPosition, waypoint);
        float newDistance = Vector3.Distance(newPosition, waypoint);
        Assert.Less(newDistance, initialDistance, "Threat should move closer to the waypoint");
    }

    // Additional tests...
}