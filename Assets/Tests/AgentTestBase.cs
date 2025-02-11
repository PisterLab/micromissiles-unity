using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public abstract class AgentTestBase : TestBase {
  protected SimManager simManager;

  [SetUp]
  public virtual void Setup() {
    // Initialize SimManager
    var simManagerGameObject = new GameObject("SimManager");
    simManager = simManagerGameObject.AddComponent<SimManager>();
    simManager.SimulationConfig = new SimulationConfig();
    SimManager.Instance = simManager;
  }

  [TearDown]
  public virtual void Teardown() {
    // Clean up SimManager
    if (simManager != null) {
      GameObject.DestroyImmediate(simManager.gameObject);
    }
  }

  protected Interceptor CreateTestInterceptor(DynamicAgentConfig config) {
    Interceptor interceptor =
        InvokePrivateMethod<Interceptor>(simManager, "CreateInterceptor", config);
    InvokePrivateMethod(interceptor, "Start");
    InvokePrivateMethod(interceptor.gameObject.GetComponent<Sensor>(), "Start");
    return interceptor;
  }

  protected Threat CreateTestThreat(DynamicAgentConfig config) {
    Threat threat = InvokePrivateMethod<Threat>(simManager, "CreateThreat", config);
    InvokePrivateMethod(threat, "Start");
    InvokePrivateMethod(threat.gameObject.GetComponent<Sensor>(), "Start");
    return threat;
  }
}
