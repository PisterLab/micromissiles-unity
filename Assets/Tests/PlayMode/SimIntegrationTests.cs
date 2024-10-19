using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;
using System.Collections.Generic;
public class SimIntegrationTests : TestBase {
  [OneTimeSetUp]
  public void LoadScene() {
    SceneManager.LoadScene("Scenes/MainScene");
  }

  [UnityTest]
  public IEnumerator TestAllConfigFilesLoad() {
    string configPath = Path.Combine(Application.streamingAssetsPath, "Configs");
    string[] jsonFiles = Directory.GetFiles(configPath, "*.json");

    Assert.IsTrue(jsonFiles.Length > 0, "No JSON files found in the Configs directory");

    bool isPaused = false;
    double epsilon = 0.0002;
    for (int i = 0; i < jsonFiles.Length; i++) {
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
                "All INTERCEPTOR agents should be in the INITIALIZED flight phase after loading while paused");

          } else if (agent is Threat) {
            // All threats start in MIDCOURSE phase
            Assert.AreEqual(
                Agent.FlightPhase.MIDCOURSE, agent.GetFlightPhase(),
                "All THREAT agents should be in the MIDCOURSE flight phase after loading while paused");
          }
        }
        Assert.LessOrEqual(Mathf.Abs(Time.fixedDeltaTime), epsilon,
                           "Fixed delta time should be approximately 0 after loading while paused");
        Assert.LessOrEqual(Mathf.Abs(Time.timeScale), epsilon,
                           "Time scale should be approximately 0 after loading while paused");
        Assert.IsFalse(elapsedTime > 0 + epsilon,
                       "Simulation time should not have advanced after loading while paused");
      } else {
        Assert.IsTrue(elapsedTime > 0 + epsilon,
                      "Simulation time should have advanced after loading while not paused");
        Assert.LessOrEqual(
            Mathf.Abs(Time.fixedDeltaTime -
                      (1.0f / SimManager.Instance.simulatorConfig.physicsUpdateRate)),
            epsilon, "Physics update rate should be 1 / simulatorConfig.physicsUpdateRate");
      }

      if (isPaused) {
        SimManager.Instance.ResumeSimulation();
        isPaused = false;
        yield return new WaitForSecondsRealtime(0.1f);
        Assert.IsTrue(SimManager.Instance.GetElapsedSimulationTime() > 0 + epsilon,
                      "Simulation time should have advanced after resuming");
      }
    }
  }

  private const string TestSimpleConfigJson =
      @"
    {
      ""endTime"": 20,
      ""timeScale"": 2,
      ""interceptor_swarm_configs"": [
        {
          ""num_agents"": 1,
          ""dynamic_agent_config"": {
            ""agent_model"": ""hydra70.json"",
            ""initial_state"": {
              ""position"": { ""x"": 0, ""y"": 20, ""z"": 0 },
              ""velocity"": { ""x"": 0, ""y"": 10, ""z"": 10 }
            },
            ""standard_deviation"": {
              ""position"": { ""x"": 10, ""y"": 0, ""z"": 10 },
              ""velocity"": { ""x"": 5, ""y"": 0, ""z"": 1 }
            },
            ""dynamic_config"": {
              ""launch_config"": { ""launch_time"": 0 },
              ""sensor_config"": {
                ""type"": ""IDEAL"",
                ""frequency"": 100
              },
              ""flight_config"": {
                ""augmentedPnEnabled"": false
              }
            },
            ""submunitions_config"": {
              ""num_submunitions"": 7,
              ""dispense_time"": 0.5,
              ""dynamic_agent_config"": {
                ""agent_model"": ""micromissile.json"",
                ""initial_state"": {
                  ""position"": { ""x"": 0, ""y"": 0, ""z"": 0 },
                  ""velocity"": { ""x"": 0, ""y"": 0, ""z"": 0 }
                },
                ""standard_deviation"": {
                  ""position"": { ""x"": 5, ""y"": 5, ""z"": 5 },
                  ""velocity"": { ""x"": 0, ""y"": 0, ""z"": 0 }
                },
                ""dynamic_config"": {
                  ""launch_config"": { ""launch_time"": 0 },
                  ""sensor_config"": {
                    ""type"": ""IDEAL"",
                    ""frequency"": 100
                  },
                  ""flight_config"": {
                    ""augmentedPnEnabled"": false
                  }
                }
              }
            }
          }
        }
      ],
      ""threat_swarm_configs"": [
        {
          ""num_agents"": 7,
          ""dynamic_agent_config"": {
            ""agent_model"": ""quadcopter_kp1.json"",
            ""attack_behavior"": ""default_direct_attack.json"",
            ""initial_state"": {
              ""position"": { ""x"": 0, ""y"": 600, ""z"": 3000 },
              ""velocity"": { ""x"": 0, ""y"": 0, ""z"": -50 }
            },
            ""standard_deviation"": {
              ""position"": { ""x"": 50, ""y"": 50, ""z"": 50 },
              ""velocity"": { ""x"": 0, ""y"": 0, ""z"": 25 }
            },
            ""dynamic_config"": {
              ""launch_config"": { ""launch_time"": 0 },
              ""sensor_config"": {
                ""type"": ""IDEAL"",
                ""frequency"": 100
              },
              ""flight_config"": {
                ""evasionEnabled"": false,
                ""evasionRangeThreshold"": 1000
              }
            },
            ""submunitions_config"": {
              ""num_submunitions"": 0
            }
          }
        }
      ]
    }
    ";

  private int _simulationStartedCount = 0;
  private int _simulationEndedCount = 0;
  private int _interceptorHitCount = 0;
  private int _interceptorMissCount = 0;
  public void RegisterSimulationStarted() {
    _simulationStartedCount++;
  }
  public void RegisterSimulationEnded() {
    _simulationEndedCount++;
  }
  public void RegisterInterceptorHit(Interceptor interceptor, Threat threat) {
    _interceptorHitCount++;
  }

  public void RegisterInterceptorMiss(Interceptor interceptor, Threat threat) {
    _interceptorMissCount++;
  }

  public void RegisterNewInterceptor(Interceptor interceptor) {
    interceptor.OnInterceptHit += RegisterInterceptorHit;
    interceptor.OnInterceptMiss += RegisterInterceptorMiss;
  }

  [UnityTest]
  public IEnumerator TestBasicMicromissilePerformance() {
    // In a simple, reduced scenario, the micromissiles should have a minimum
    // level of performance that results in a 100% kill rate of quadcopters
    SimManager.Instance.OnSimulationStarted += RegisterSimulationStarted;
    SimManager.Instance.OnSimulationEnded += RegisterSimulationEnded;
    SimManager.Instance.OnNewInterceptor += RegisterNewInterceptor;
    string configPath =
        Path.Combine(Application.streamingAssetsPath, "Configs/test_simple_config.json");
    Directory.CreateDirectory(Path.GetDirectoryName(configPath));
    File.WriteAllText(configPath, TestSimpleConfigJson);
    Time.fixedDeltaTime = 0.001f;  // Force 1000 Hz Physics update rate
    SimManager.Instance.LoadNewConfig(configPath);

    double dispenseTime = SimManager.Instance.GetActiveAgents()[0]
                              .dynamicAgentConfig.submunitions_config.dispense_time;
    for (int i = 0; i < 3; i++) {
      double elapsedTime = SimManager.Instance.GetElapsedSimulationTime();
      // Wait for 10ms, micromissiles should not have been dispensed yet
      yield return new WaitUntil(() => SimManager.Instance.GetElapsedSimulationTime() >=
                                       elapsedTime + 0.01);
      // TODO: Here is a good opportunity to test things before dispense.
      // BUT keep in mind, dispense time is noisy (1sd = 0.5s)
      // Wait for 600ms, micromissiles should have been dispensed
      elapsedTime = SimManager.Instance.GetElapsedSimulationTime();
      yield return new WaitUntil(() => SimManager.Instance.GetElapsedSimulationTime() >=
                                       elapsedTime + dispenseTime);
      List<Interceptor> interceptors = SimManager.Instance.GetActiveInterceptors();
      Assert.AreEqual(8, interceptors.Count, "All micromissiles should have been dispensed");

      // All interceptors which are not CarrierInterceptor should have been
      // assigned a target
      foreach (Interceptor interceptor in interceptors) {
        if (interceptor is CarrierInterceptor) {
          continue;
        }
        Assert.IsTrue(interceptor.HasAssignedTarget(),
                      $"Interceptor {interceptor.name} should have a target");
      }
      elapsedTime = SimManager.Instance.GetElapsedSimulationTime();
      yield return new WaitUntil(() => SimManager.Instance.GetActiveThreats().Count == 0 ||
                                       SimManager.Instance.GetElapsedSimulationTime() >=
                                           elapsedTime + 6);
      Assert.GreaterOrEqual(_simulationStartedCount, 1,
                            "Simulation should be started at least once");
      Assert.GreaterOrEqual(_simulationEndedCount, 1, "Simulation should be ended at least once");
      Assert.AreEqual(_interceptorHitCount, 7, "All threats should be hit");
      Assert.AreEqual(_interceptorMissCount, 0, "No threats should be missed");
      Debug.Log($"Simulation started {_simulationStartedCount} times");
      Debug.Log($"Simulation ended {_simulationEndedCount} times");
      _simulationStartedCount = 0;
      _simulationEndedCount = 0;
      _interceptorHitCount = 0;
      _interceptorMissCount = 0;
      SimManager.Instance.RestartSimulation();
    }

    // Clean up the config file
    File.Delete(configPath);
  }
}
