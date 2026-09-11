using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

public class SimMonitorMetadataTests : TestBase {
  private const string _timestamp = "20260102_030405";

  private string _outputDirectory;
  private GameObject _agentObject;
  private SimManager _simManager;
  private SimMonitor _simMonitor;

  [SetUp]
  public void SetUp() {
    SetRunWorkerProperty("IsWorkerMode", true);
    SetRunWorkerProperty("Seed", 321);

    _outputDirectory = Path.Combine(Path.GetTempPath(), $"sim-metadata-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_outputDirectory);

    _simManager = new GameObject("SimManager").AddComponent<SimManager>();
    SetSingleton(_simManager);
    _simManager.SimulatorConfig = new Configs.SimulatorConfig();
    _simManager.SimulationConfig = new Configs.SimulationConfig {
      CommunicationConfig =
          new Configs.CommunicationConfig {
            LinkConfig =
                new Configs.LinkConfig {
                  LatencySeconds = 0.15f,
                  LatencyStdSeconds = 0.025f,
                },
          },
    };
    _simManager.SimulationConfig.CommunicationConfig.LinkOverrides.Add(new Configs.LinkOverride {
      From = Configs.AgentType.Vessel,
      To = Configs.AgentType.CarrierInterceptor,
      LinkConfig =
          new Configs.LinkConfig {
            LatencySeconds = 0.4f,
            LatencyStdSeconds = 0.05f,
          },
    });
    SetPrivateProperty(_simManager, "SimulationConfigFile", "comparison.pbtxt");

    _simMonitor = new GameObject("SimMonitor").AddComponent<SimMonitor>();
    SetSingleton(_simMonitor);
    SetPrivateProperty(_simMonitor, "Timestamp", _timestamp);
    SetPrivateField(_simMonitor, "_sessionDirectory", _outputDirectory);
  }

  [TearDown]
  public void TearDown() {
    if (_simMonitor != null) {
      UnityEngine.Object.DestroyImmediate(_simMonitor.gameObject);
    }
    if (_simManager != null) {
      UnityEngine.Object.DestroyImmediate(_simManager.gameObject);
    }
    if (_agentObject != null) {
      UnityEngine.Object.DestroyImmediate(_agentObject);
    }
    SetSingleton<SimMonitor>(null);
    SetSingleton<SimManager>(null);
    SetRunWorkerProperty("Seed", 0);
    SetRunWorkerProperty("IsWorkerMode", false);

    if (!string.IsNullOrEmpty(_outputDirectory) && Directory.Exists(_outputDirectory)) {
      Directory.Delete(_outputDirectory, recursive: true);
    }
  }

  [Test]
  public void WriteRunMetadata_RecordsSeedAndDefaultAndOverrideLatency() {
    InvokePrivateMethod(_simMonitor, "WriteRunMetadata");

    string metadataPath = Path.Combine(_outputDirectory, $"run_metadata_{_timestamp}.json");
    Assert.IsTrue(File.Exists(metadataPath));
    string metadataJson = File.ReadAllText(metadataPath);
    StringAssert.Contains("\"SimulationConfigFile\": \"comparison.pbtxt\"", metadataJson);
    StringAssert.Contains("\"IsWorkerMode\": true", metadataJson);
    StringAssert.Contains("\"Seed\": 321", metadataJson);
    StringAssert.Contains("\"LatencySeconds\": 0.15", metadataJson);
    StringAssert.Contains("\"LatencyStdSeconds\": 0.025", metadataJson);
    StringAssert.Contains("\"FromAgentType\": \"Vessel\"", metadataJson);
    StringAssert.Contains("\"ToAgentType\": \"CarrierInterceptor\"", metadataJson);
    StringAssert.Contains("\"LatencySeconds\": 0.4", metadataJson);
    StringAssert.Contains("\"LatencyStdSeconds\": 0.05", metadataJson);
  }

  [Test]
  public void EventLogging_WritesOriginalAndTargetEnrichedCsvFiles() {
    _simManager.SimulatorConfig.EnableEventLogging = true;
    InvokePrivateMethod(_simMonitor, "InitializeEventLogging");

    _agentObject = new GameObject("Test Carrier");
    _agentObject.AddComponent<Rigidbody>();
    var agent = _agentObject.AddComponent<AgentBase>();
    agent.AgentId = "LCH-001-C001";
    agent.StaticConfig =
        new Configs.StaticConfig { AgentType = Configs.AgentType.CarrierInterceptor };
    agent.HierarchicalAgent = new HierarchicalAgent(agent);

    InvokePrivateMethod(_simMonitor, "RegisterAgentEvent", agent, "INTERCEPTOR_HIT");
    InvokePrivateMethod(_simMonitor, "RegisterTargetChanged", agent,
                        new string[] { "THR-S001-A0001" },
                        new string[] { "THR-S001-A0002", "THR-S001-A0003" });
    InvokePrivateMethod(_simMonitor, "WriteEventsToFile");

    string originalPath = Path.Combine(_outputDirectory, $"sim_events_{_timestamp}.csv");
    string enrichedPath = Path.Combine(_outputDirectory, $"sim_target_events_{_timestamp}.csv");
    string originalCsv = File.ReadAllText(originalPath);
    string enrichedCsv = File.ReadAllText(enrichedPath);

    StringAssert.StartsWith("Time,Event,AgentType,AgentID,PositionX,PositionY,PositionZ",
                            originalCsv);
    StringAssert.Contains("INTERCEPTOR_HIT", originalCsv);
    StringAssert.DoesNotContain("TARGET_CHANGED", originalCsv);

    StringAssert.StartsWith("Time,Event,AgentType,AgentID,TargetID,TargetIDs,PreviousTargetID," +
                                "PreviousTargetIDs,PositionX,PositionY,PositionZ",
                            enrichedCsv);
    StringAssert.Contains("INTERCEPTOR_HIT", enrichedCsv);
    StringAssert.Contains("TARGET_CHANGED", enrichedCsv);
    StringAssert.Contains("THR-S001-A0002|THR-S001-A0003", enrichedCsv);
    StringAssert.Contains("THR-S001-A0001", enrichedCsv);
  }

  private static void SetRunWorkerProperty<T>(string propertyName, T value) {
    PropertyInfo property =
        typeof(RunWorker).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
    Assert.IsNotNull(property);
    property.SetValue(null, value);
  }
}
