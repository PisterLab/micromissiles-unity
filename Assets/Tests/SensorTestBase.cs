using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public abstract class SensorTestBase : TestBase {
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
}
