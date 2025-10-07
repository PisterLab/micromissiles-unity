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
    simManager.SimulationConfig = new Configs.SimulationConfig();
    SimManager.Instance = simManager;
  }

  [TearDown]
  public virtual void Teardown() {
    // Clean up SimManager
    if (simManager != null) {
      GameObject.DestroyImmediate(simManager.gameObject);
    }
  }

  protected Interceptor CreateTestInterceptor(Configs.AgentConfig config) {
    Interceptor interceptor = SimManager.Instance.CreateInterceptor(
        config, new Simulation.State() { Position = Coordinates3.ToProto(Vector3.zero),
                                         Velocity = Coordinates3.ToProto(Vector3.zero) });
    InvokePrivateMethod(interceptor, "Start");
    InvokePrivateMethod(interceptor.gameObject.GetComponent<Sensor>(), "Start");
    return interceptor;
  }

  protected Threat CreateTestThreat(Configs.AgentConfig config) {
    Threat threat = SimManager.Instance.CreateThreat(config);
    InvokePrivateMethod(threat, "Start");
    InvokePrivateMethod(threat.gameObject.GetComponent<Sensor>(), "Start");
    return threat;
  }
}
