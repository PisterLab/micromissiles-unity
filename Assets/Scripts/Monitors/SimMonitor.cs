using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class SimMonitor : MonoBehaviour {
  private static class EventTypes {
    public const string NewInterceptor = "NEW_INTERCEPTOR";
    public const string NewThreat = "NEW_THREAT";
    public const string InterceptorHit = "INTERCEPTOR_HIT";
    public const string InterceptorMiss = "INTERCEPTOR_MISS";
    public const string ThreatHit = "THREAT_HIT";
    public const string ThreatMiss = "THREAT_MISS";
  }

  [System.Serializable]
  private class EventRecord {
    public float Time;
    public string EventType;
    public string AgentType;
    public string AgentID;
    public float PositionX;
    public float PositionY;
    public float PositionZ;
  }

  private const float _updatePeriod = 0.1f;  // 100 Hz

  public static SimMonitor Instance { get; private set; }

  public string Timestamp { get; private set; } = "";

  private Coroutine _monitorRoutine;

  private string _sessionDirectory;

  private string _telemetryBinPath;
  private FileStream _telemetryFileStream;
  private BinaryWriter _telemetryBinaryWriter;

  private string _eventLogPath;
  [SerializeField]
  private List<EventRecord> _eventLog;

  private void Awake() {
    if (Instance != null && Instance != this) {
      Destroy(gameObject);
      return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject);
  }

  private void Start() {
    SimManager.Instance.OnSimulationStarted += RegisterSimulationStarted;
    SimManager.Instance.OnSimulationEnded += RegisterSimulationEnded;
    SimManager.Instance.OnNewThreat += RegisterNewThreat;
    SimManager.Instance.OnNewInterceptor += RegisterNewInterceptor;
  }

  private void OnDestroy() {
    DestroyLogging();
  }

  private void RegisterSimulationStarted() {
    Timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    InitializeSessionDirectory();
    if (SimManager.Instance.SimulatorConfig.EnableTelemetryLogging) {
      InitializeTelemetryLogging();
      _monitorRoutine = StartCoroutine(MonitorRoutine());
    }
    if (SimManager.Instance.SimulatorConfig.EnableEventLogging) {
      InitializeEventLogging();
    }
  }

  private void RegisterSimulationEnded() {
    RecordTelemetry();
    DestroyLogging();
  }

  private void RegisterNewInterceptor(IInterceptor interceptor) {
    RegisterAgentEvent(interceptor, EventTypes.NewInterceptor);
    interceptor.OnHit += RegisterInterceptorHit;
    interceptor.OnMiss += RegisterInterceptorMiss;
  }

  private void RegisterNewThreat(IThreat threat) {
    RegisterAgentEvent(threat, EventTypes.NewThreat);
    threat.OnHit += RegisterThreatHit;
    threat.OnMiss += RegisterThreatMiss;
  }

  private void RegisterInterceptorHit(IInterceptor interceptor) {
    RegisterAgentEvent(interceptor, EventTypes.InterceptorHit);
  }

  private void RegisterInterceptorMiss(IInterceptor interceptor) {
    RegisterAgentEvent(interceptor, EventTypes.InterceptorMiss);
  }

  private void RegisterThreatHit(IThreat threat) {
    RegisterAgentEvent(threat, EventTypes.ThreatHit);
  }

  private void RegisterThreatMiss(IThreat threat) {
    RegisterAgentEvent(threat, EventTypes.ThreatMiss);
  }

  private void RegisterAgentEvent(IAgent agent, string eventType) {
    if (SimManager.Instance.SimulatorConfig.EnableEventLogging) {
      float time = SimManager.Instance.ElapsedTime;
      Vector3 position = agent.Position;
      var record = new EventRecord {
        Time = time,
        EventType = eventType,
        AgentType = agent.StaticConfig.AgentType.ToString(),
        AgentID = agent.gameObject.name,
        PositionX = position.x,
        PositionY = position.y,
        PositionZ = position.z,
      };
      _eventLog.Add(record);
    }
  }

  private void InitializeSessionDirectory() {
    if (RunManager.Instance.HasRunConfig()) {
      _sessionDirectory =
          Path.Combine(Application.persistentDataPath, "Logs",
                       $"{RunManager.Instance.RunConfig.Name}_{RunManager.Instance.Timestamp}",
                       $"run_{RunManager.Instance.RunIndex + 1}_seed_{RunManager.Instance.Seed}");
    } else {
      _sessionDirectory = Path.Combine(Application.persistentDataPath, "Logs",
                                       $"run_{SimManager.Instance.Timestamp}");
    }
    Directory.CreateDirectory(_sessionDirectory);
    Debug.Log($"Monitoring simulation logs to {_sessionDirectory}.");
  }

  private void InitializeTelemetryLogging() {
    string telemetryFile = $"sim_telemetry_{Timestamp}.bin";
    _telemetryBinPath = Path.Combine(_sessionDirectory, telemetryFile);
    _telemetryFileStream =
        new FileStream(_telemetryBinPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
    _telemetryBinaryWriter = new BinaryWriter(_telemetryFileStream);
    Debug.Log($"Telemetry file initialized successfully: {telemetryFile}.");
  }

  private void InitializeEventLogging() {
    string eventLog = $"sim_events_{Timestamp}.csv";
    _eventLogPath = Path.Combine(_sessionDirectory, eventLog);
    _eventLog = new List<EventRecord>();
    Debug.Log($"Event log initialized successfully: {eventLog}.");
  }

  private void DestroyLogging() {
    if (SimManager.Instance.SimulatorConfig.EnableTelemetryLogging) {
      StopCoroutine(_monitorRoutine);
      DestroyTelemetryLogging();
      ConvertTelemetryBinaryToCsv(_telemetryBinPath,
                                  Path.ChangeExtension(_telemetryBinPath, ".csv"));
    }
    if (SimManager.Instance.SimulatorConfig.EnableEventLogging) {
      WriteEventsToFile();
    }
  }

  private void DestroyTelemetryLogging() {
    if (_telemetryBinaryWriter != null) {
      _telemetryBinaryWriter.Flush();
      _telemetryBinaryWriter.Close();
      _telemetryBinaryWriter = null;
    }

    if (_telemetryFileStream != null) {
      _telemetryFileStream.Close();
      _telemetryFileStream = null;
    }
  }

  private IEnumerator MonitorRoutine() {
    while (true) {
      RecordTelemetry();
      yield return new WaitForSeconds(_updatePeriod);
    }
  }

  private void RecordTelemetry() {
    float time = SimManager.Instance.ElapsedTime;
    List<IAgent> agents = SimManager.Instance.Agents.Where(agent => !agent.IsTerminated).ToList();
    if (_telemetryBinaryWriter == null) {
      Debug.LogWarning("Telemetry binary writer is null.");
      return;
    }
    foreach (var agent in agents) {
      if (!agent.gameObject.activeInHierarchy) {
        continue;
      }

      Vector3 position = agent.Position;
      if (position == Vector3.zero) {
        continue;
      }
      Vector3 velocity = agent.Velocity;

      // Write telemetry data directly to the binary file.
      _telemetryBinaryWriter.Write(time);
      _telemetryBinaryWriter.Write((int)agent.StaticConfig.AgentType);
      _telemetryBinaryWriter.Write(agent.gameObject.name);
      _telemetryBinaryWriter.Write(position.x);
      _telemetryBinaryWriter.Write(position.y);
      _telemetryBinaryWriter.Write(position.z);
      _telemetryBinaryWriter.Write(velocity.x);
      _telemetryBinaryWriter.Write(velocity.y);
      _telemetryBinaryWriter.Write(velocity.z);
    }
  }

  private void ConvertTelemetryBinaryToCsv(string binaryFilePath, string csvFilePath) {
    try {
      using var fs =
          new FileStream(binaryFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      using var reader = new BinaryReader(fs);
      using var writer = new StreamWriter(csvFilePath, false);
      {
        // Write the CSV header.
        writer.WriteLine(
            "Time,AgentType,AgentID,PositionX,PositionY,PositionZ,VelocityX,VelocityY,VelocityZ");

        while (reader.BaseStream.Position != reader.BaseStream.Length) {
          float time = reader.ReadSingle();
          var agentType = (Configs.AgentType)reader.ReadInt32();
          string agentID = reader.ReadString();
          float positionX = reader.ReadSingle();
          float positionY = reader.ReadSingle();
          float positionZ = reader.ReadSingle();
          float velocityX = reader.ReadSingle();
          float velocityY = reader.ReadSingle();
          float velocityZ = reader.ReadSingle();

          // Write the data to CSV.
          writer.WriteLine($"{time:F2},{agentType.ToString()},{agentID}," +
                           $"{positionX:F2},{positionY:F2},{positionZ:F2}," +
                           $"{velocityX:F2},{velocityY:F2},{velocityZ:F2}");
        }
        Debug.Log($"Telemetry CSV file converted successfully: {csvFilePath}.");
      }
    } catch (IOException e) {
      Debug.LogWarning(
          $"An IO error occurred while converting binary telemetry file to CSV format: {e.Message}.");
    }
  }

  private void WriteEventsToFile() {
    using (var writer = new StreamWriter(_eventLogPath, append: false)) {
      // Write the CSV header.
      writer.WriteLine("Time,Event,AgentType,AgentID,PositionX,PositionY,PositionZ");

      foreach (var record in _eventLog) {
        writer.WriteLine(
            $"{record.Time:F2},{record.EventType},{record.AgentType},{record.AgentID}," +
            $"{record.PositionX:F2},{record.PositionY:F2},{record.PositionZ:F2}");
      }
    }
  }
}
