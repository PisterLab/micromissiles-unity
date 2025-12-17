using NUnit.Framework;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

public class ConfigsTests : TestBase {
  private const double _epsilon = 0.0002;

  private readonly HashSet<string> _excludedConfigFiles =
      new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
        // The CI runner times out when clustering 1000 threats.
        "5_swarms_1000_ucav.pbtxt",
      };

  [OneTimeSetUp]
  public void LoadScene() {
    SceneManager.LoadScene("Scenes/MainScene");
  }

  [UnityTest]
  public IEnumerator TestAllSimulationConfigFilesLoad() {
    string configPath = ConfigLoader.GetStreamingAssetsFilePath("Configs/Simulations");
    string[] allConfigFiles = Directory.GetFiles(configPath, "*.pbtxt");
    Assert.IsTrue(allConfigFiles.Length > 0, "No simulation configuration files found.");

    // Exclude the specified simulation configuration files.
    List<string> configFiles =
        allConfigFiles.Where(path => !_excludedConfigFiles.Contains(Path.GetFileName(path)))
            .ToList();

    bool isPaused = false;
    for (int i = 0; i < configFiles.Count; ++i) {
      if (i % 2 == 1) {
        SimManager.Instance.PauseSimulation();
        isPaused = true;
      }
      yield return new WaitForSecondsRealtime(0.1f);
      SimManager.Instance.LoadNewSimulationConfig(configFiles[i]);
      yield return new WaitForSecondsRealtime(0.1f);
      double elapsedTime = SimManager.Instance.ElapsedTime;
      if (isPaused) {
        Assert.LessOrEqual(
            Mathf.Abs(Time.fixedDeltaTime), _epsilon,
            "Fixed delta time should be approximately 0 after loading while paused.");
        Assert.LessOrEqual(Mathf.Abs(Time.timeScale), _epsilon,
                           "Time scale should be approximately 0 after loading while paused.");
        Assert.IsFalse(elapsedTime > 0 + _epsilon,
                       "Simulation time should not have advanced after loading while paused.");
      } else {
        Assert.IsTrue(elapsedTime > 0 + _epsilon,
                      "Simulation time should have advanced after loading while not paused.");
        Assert.LessOrEqual(
            Mathf.Abs(Time.fixedDeltaTime -
                      (1.0f / SimManager.Instance.SimulatorConfig.PhysicsUpdateRate)),
            _epsilon, "Physics update rate should be 1 / SimulatorConfig.PhysicsUpdateRate.");
      }

      if (isPaused) {
        SimManager.Instance.ResumeSimulation();
        isPaused = false;
        yield return new WaitForSecondsRealtime(0.1f);
        Assert.IsTrue(SimManager.Instance.ElapsedTime > 0 + _epsilon,
                      "Simulation time should have advanced after resuming.");
      }
    }
  }
}
