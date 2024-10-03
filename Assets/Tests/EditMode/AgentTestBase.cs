using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public abstract class AgentTestBase : TestBase
{
    protected SimManager simManager;

    [SetUp]
    public virtual void Setup()
    {
        // Initialize SimManager
        var simManagerGameObject = new GameObject("SimManager");
        simManager = simManagerGameObject.AddComponent<SimManager>();
        simManager.simulationConfig = new SimulationConfig(); // Set up a basic config if needed

        SimManager.Instance = simManager;
    }

    [TearDown]
    public virtual void Teardown()
    {
        // Clean up SimManager
        if (simManager != null)
        {
            GameObject.DestroyImmediate(simManager.gameObject);
        }
    }
    protected Threat CreateTestThreat(DynamicAgentConfig config) {
        return InvokePrivateMethod<Threat>(simManager, "CreateThreat", config);
    }

    protected Interceptor CreateTestInterceptor(DynamicAgentConfig config) {
        return InvokePrivateMethod<Interceptor>(simManager, "CreateInterceptor", config);
    }

    protected void SetVelocity(Agent agent, Vector3 velocity)
    {
        Rigidbody rb = agent.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = velocity;
        }
    }
}