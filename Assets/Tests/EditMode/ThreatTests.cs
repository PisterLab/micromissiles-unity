using System;
using NUnit.Framework;
using UnityEngine;

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


        fixedWingThreat = CreateTestThreat(ucavConfig) as FixedWingThreat;
        Assert.IsNotNull(fixedWingThreat);
        rotaryWingThreat = CreateTestThreat(quadcopterConfig) as RotaryWingThreat;
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

    // Additional tests...
}