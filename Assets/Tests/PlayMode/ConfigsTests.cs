using System;
using NUnit.Framework;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

public class ConfigsTests : TestBase {
  private static bool IsCi() {
    string ci = Environment.GetEnvironmentVariable("CI");
    if (string.IsNullOrEmpty(ci)) {
      return false;
    }
    return ci.Equals("1") || ci.Equals("true", StringComparison.OrdinalIgnoreCase);
  }

  private static HashSet<string> GetCiExcludedSimulationConfigs(string simulationsConfigPath) {
    var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    if (!IsCi()) {
      return excluded;
    }

    string exclusionFile =
        Path.Combine(simulationsConfigPath, "ci_excluded_simulation_configs.txt");
    if (!File.Exists(exclusionFile)) {
      return excluded;
    }

    foreach (string line in File.ReadAllLines(exclusionFile)) {
      string trimmed = line.Trim();
      if (trimmed.Length == 0 || trimmed.StartsWith("#")) {
        continue;
      }
      excluded.Add(trimmed);
    }
    return excluded;
  }

  [OneTimeSetUp]
  public void LoadScene() {
    SceneManager.LoadScene("Scenes/MainScene");
  }

  [UnityTest]
  public IEnumerator TestAllSimulationConfigFilesLoad() {
    string configPath = ConfigLoader.GetStreamingAssetsFilePath("Configs/Simulations");
    string[] configFiles = Directory.GetFiles(configPath, "*.pbtxt");
    Array.Sort(configFiles, StringComparer.Ordinal);
    Assert.IsTrue(configFiles.Length > 0, "No simulation configuration files found.");

    HashSet<string> ciExcludedConfigs = GetCiExcludedSimulationConfigs(configPath);
    bool isPaused = false;
    double epsilon = 0.0002;
    int numConfigsLoaded = 0;
    for (int i = 0; i < configFiles.Length; ++i) {
      string configFile = configFiles[i];
      string configFilename = Path.GetFileName(configFile);
      if (ciExcludedConfigs.Contains(configFilename)) {
        Debug.Log($"[CI] Skipping simulation config: {configFilename}.");
        continue;
      }

      if (numConfigsLoaded % 2 == 1) {
        SimManager.Instance.PauseSimulation();
        isPaused = true;
      }
      yield return new WaitForSecondsRealtime(0.1f);
      SimManager.Instance.LoadNewSimulationConfig(configFile);
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

      ++numConfigsLoaded;
    }

    Assert.IsTrue(numConfigsLoaded > 0, "No simulation configuration files were loaded.");
  }
}
