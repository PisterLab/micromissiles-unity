using NUnit.Framework;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

public class ConfigsTests : TestBase {
  [OneTimeSetUp]
  public void LoadScene() {
    SceneManager.LoadScene("Scenes/MainScene");
  }

  [UnityTest]
  public IEnumerator TestAllSimulationConfigFilesLoad() {
    string configPath = ConfigLoader.GetStreamingAssetsFilePath("Configs/Simulations");
    string[] configFiles = Directory.GetFiles(configPath, "*.pbtxt");

    Assert.IsTrue(configFiles.Length > 0, "No simulation configuration files found.");

    bool isPaused = false;
    double epsilon = 0.0002;
    for (int i = 0; i < configFiles.Length; ++i) {
      if (i % 2 == 1) {
        SimManager.Instance.PauseSimulation();
        isPaused = true;
      }
      yield return new WaitForSecondsRealtime(0.1f);
      SimManager.Instance.LoadNewSimulationConfig(configFiles[i]);
      yield return new WaitForSecondsRealtime(0.1f);
      double elapsedTime = SimManager.Instance.GetElapsedSimulationTime();
      if (isPaused) {
        List<Agent> agents = SimManager.Instance.GetActiveAgents();
        foreach (Agent agent in agents) {
          if (agent is Interceptor interceptor) {
            // All interceptors start in the INITIALIZED phase.
            Assert.AreEqual(
                Agent.FlightPhase.INITIALIZED, interceptor.GetFlightPhase(),
                "All interceptors should be in the INITIALIZED flight phase after loading while paused.");

          } else if (agent is Threat threat) {
            // All threats start in the INITIALIZED phase.
            Assert.AreEqual(
                Agent.FlightPhase.INITIALIZED, threat.GetFlightPhase(),
                "All threats should be in the INITIALIZED flight phase after loading while paused.");
          }
        }
        Assert.LessOrEqual(
            Mathf.Abs(Time.fixedDeltaTime), epsilon,
            "Fixed delta time should be approximately 0 after loading while paused.");
        Assert.LessOrEqual(Mathf.Abs(Time.timeScale), epsilon,
                           "Time scale should be approximately 0 after loading while paused.");
        Assert.IsFalse(elapsedTime > 0 + epsilon,
                       "Simulation time should not have advanced after loading while paused.");
      } else {
        Assert.IsTrue(elapsedTime > 0 + epsilon,
                      "Simulation time should have advanced after loading while not paused.");
        Assert.LessOrEqual(
            Mathf.Abs(Time.fixedDeltaTime -
                      (1.0f / SimManager.Instance.SimulatorConfig.PhysicsUpdateRate)),
            epsilon, "Physics update rate should be 1 / SimulatorConfig.PhysicsUpdateRate.");
      }

      if (isPaused) {
        SimManager.Instance.ResumeSimulation();
        isPaused = false;
        yield return new WaitForSecondsRealtime(0.1f);
        Assert.IsTrue(SimManager.Instance.GetElapsedSimulationTime() > 0 + epsilon,
                      "Simulation time should have advanced after resuming.");
      }
    }
  }
}
