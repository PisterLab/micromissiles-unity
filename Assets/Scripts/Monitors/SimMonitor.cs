using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class SimMonitor : MonoBehaviour {
  [System.Serializable]
  private class EventRecord {
    public float Time;
    public float PositionX;
    public float PositionY;
    public float PositionZ;
    public string EventType;
    public string Details;
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
  private List<EventRecord> _eventLogCache;

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
    RegisterNewAgent(interceptor, "NEW_INTERCEPTOR");
    interceptor.OnHit += RegisterInterceptorHit;
    interceptor.OnMiss += RegisterInterceptorMiss;
  }

  private void RegisterNewThreat(IThreat threat) {
    RegisterNewAgent(threat, "NEW_THREAT");
    threat.OnHit += RegisterThreatHit;
    threat.OnMiss += RegisterThreatMiss;
  }

  private void RegisterNewAgent(IAgent agent, string eventType) {
    if (SimManager.Instance.SimulatorConfig.EnableEventLogging) {
      float time = SimManager.Instance.ElapsedTime;
      Vector3 pos = agent.transform.position;
      var record = new EventRecord {
        Time = time,       PositionX = pos.x,     PositionY = pos.y,
        PositionZ = pos.z, EventType = eventType, Details = agent.gameObject.name,
      };
      _eventLogCache.Add(record);
    }
  }

  private void RegisterInterceptorHit(IInterceptor interceptor) {
    RegisterAgentEvent(interceptor, hit: true, event_prefix: "INTERCEPTOR");
  }

  private void RegisterInterceptorMiss(IInterceptor interceptor) {
    RegisterAgentEvent(interceptor, hit: false, event_prefix: "INTERCEPTOR");
  }

  private void RegisterThreatHit(IThreat threat) {
    RegisterAgentEvent(threat, hit: true, event_prefix: "THREAT");
  }

  private void RegisterThreatMiss(IThreat threat) {
    RegisterAgentEvent(threat, hit: false, event_prefix: "THREAT");
  }

  private void RegisterAgentEvent(IAgent agent, bool hit, string event_prefix) {
    if (SimManager.Instance.SimulatorConfig.EnableEventLogging) {
      float time = SimManager.Instance.ElapsedTime;
      Vector3 position = agent.Position;
      string eventType = hit ? $"{event_prefix}_HIT" : $"{event_prefix}_MISS";
      var record = new EventRecord {
        Time = time,
        PositionX = position.x,
        PositionY = position.y,
        PositionZ = position.z,
        EventType = eventType,
        Details = $"{agent.gameObject.name}",
      };
      _eventLogCache.Add(record);
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
    _eventLogCache = new List<EventRecord>();
    Debug.Log($"Event log initialized successfully: {eventLog}.");
  }

  private void DestroyLogging() {
    if (SimManager.Instance.SimulatorConfig.EnableTelemetryLogging) {
      StopCoroutine(_monitorRoutine);
      DestroyTelemetryLogging();
      ConvertBinaryTelemetryToCsv(_telemetryBinPath,
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

      Vector3 position = agent.transform.position;
      if (position == Vector3.zero) {
        continue;
      }

      Vector3 velocity = agent.Velocity;
      int agentID = agent.gameObject.GetInstanceID();
      byte agentType = (byte)(agent is IThreat ? 0 : 1);

      // Write telemetry data directly to the binary file.
      _telemetryBinaryWriter.Write(time);
      _telemetryBinaryWriter.Write(agentID);
      _telemetryBinaryWriter.Write(position.x);
      _telemetryBinaryWriter.Write(position.y);
      _telemetryBinaryWriter.Write(position.z);
      _telemetryBinaryWriter.Write(velocity.x);
      _telemetryBinaryWriter.Write(velocity.y);
      _telemetryBinaryWriter.Write(velocity.z);
      _telemetryBinaryWriter.Write(agentType);
    }
  }

  private void ConvertBinaryTelemetryToCsv(string binaryFilePath, string csvFilePath) {
    try {
      using FileStream fs =
          new FileStream(binaryFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      using BinaryReader reader = new BinaryReader(fs);
      using StreamWriter writer = new StreamWriter(csvFilePath, false);
      {
        // Write the CSV header.
        writer.WriteLine("Time,AgentID,AgentX,AgentY,AgentZ,AgentVX,AgentVY,AgentVZ,AgentType");

        while (reader.BaseStream.Position != reader.BaseStream.Length) {
          float time = reader.ReadSingle();
          int agentID = reader.ReadInt32();
          float positionX = reader.ReadSingle();
          float positionY = reader.ReadSingle();
          float positionZ = reader.ReadSingle();
          float velocityX = reader.ReadSingle();
          float velocityY = reader.ReadSingle();
          float velocityZ = reader.ReadSingle();
          byte agentTypeByte = reader.ReadByte();
          string agentType = agentTypeByte == 0 ? "T" : "M";

          // Write the data to CSV.
          writer.WriteLine(
              $"{time:F2},{agentID},{positionX:F2},{positionY:F2},{positionZ:F2},{velocityX:F2},{velocityY:F2},{velocityZ:F2},{agentType}");
        }
        Debug.Log($"Telemetry CSV file converted successfully: {csvFilePath}.");
      }
    } catch (IOException e) {
      Debug.LogWarning(
          $"An IO error occurred while converting binary telemetry file to CSV format: {e.Message}.");
    }
  }

  private void WriteEventsToFile() {
    using (StreamWriter writer = new StreamWriter(_eventLogPath, false)) {
      // Write the CSV header.
      writer.WriteLine("Time,PositionX,PositionY,PositionZ,Event,Details");

      foreach (var record in _eventLogCache) {
        writer.WriteLine(
            $"{record.Time:F2},{record.PositionX:F2},{record.PositionY:F2},{record.PositionZ:F2},{record.EventType},{record.Details}");
      }
    }
  }
}
