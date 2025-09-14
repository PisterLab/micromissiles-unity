using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;
using System.Collections.Generic;
public class ConfigTest : TestBase {
  [OneTimeSetUp]
  public void LoadScene() {
    SceneManager.LoadScene("Scenes/MainScene");
  }

  [UnityTest]
  public IEnumerator TestAllConfigFilesLoad() {
    string configPath = ConfigLoader.GetStreamingAssetsFilePath("Configs");
    string[] jsonFiles = Directory.GetFiles(configPath, "*.json");

    Assert.IsTrue(jsonFiles.Length > 0, "No JSON files found in the configuration directory.");

    bool isPaused = false;
    double epsilon = 0.0002;
    for (int i = 0; i < jsonFiles.Length; ++i) {
      if (i % 2 == 1) {
        SimManager.Instance.PauseSimulation();
        isPaused = true;
      }
      yield return new WaitForSecondsRealtime(0.1f);
      SimManager.Instance.LoadNewConfig(jsonFiles[i]);
      yield return new WaitForSecondsRealtime(0.1f);
      double elapsedTime = SimManager.Instance.GetElapsedSimulationTime();
      if (isPaused) {
        List<Agent> agents = SimManager.Instance.GetActiveAgents();
        foreach (Agent agent in agents) {
          if (agent is Interceptor) {
            // All interceptors start in INITIALIZED phase
            Assert.AreEqual(
                Agent.FlightPhase.INITIALIZED, agent.GetFlightPhase(),
                "All interceptors should be in the INITIALIZED flight phase after loading while paused.");

          } else if (agent is Threat) {
            // All threats start in INITIALIZED phase
            Assert.AreEqual(
                Agent.FlightPhase.INITIALIZED, agent.GetFlightPhase(),
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
