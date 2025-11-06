using NUnit.Framework;
using System.Reflection;
using UnityEngine;

public abstract class AgentTestBase : TestBase {
  protected SimManager _simManager;

  [SetUp]
  public virtual void Setup() {
    // Initialize the simulation manager.
    var simManagerGameObject = new GameObject("SimManager");
    _simManager = simManagerGameObject.AddComponent<SimManager>();
    _simManager.SimulationConfig = new Configs.SimulationConfig();
    SimManager.Instance = _simManager;
  }

  [TearDown]
  public virtual void Teardown() {
    // Clean up the simulation manager.
    if (_simManager != null) {
      GameObject.DestroyImmediate(_simManager.gameObject);
    }
  }

  protected Interceptor CreateTestInterceptor(Configs.AgentConfig config) {
    Interceptor interceptor = SimManager.Instance.CreateInterceptor(
        config, new Simulation.State() { Position = Coordinates3.ToProto(Vector3.zero),
                                         Velocity = Coordinates3.ToProto(Vector3.zero) });
    InvokePrivateMethod(interceptor, "Start");
    InvokePrivateMethod(interceptor.gameObject.GetComponent<SensorLegacy>(), "Start");
    return interceptor;
  }

  protected Threat CreateTestThreat(Configs.AgentConfig config) {
    Threat threat = SimManager.Instance.CreateThreat(config);
    InvokePrivateMethod(threat, "Start");
    InvokePrivateMethod(threat.gameObject.GetComponent<SensorLegacy>(), "Start");
    return threat;
  }
}
