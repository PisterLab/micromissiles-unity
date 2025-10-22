using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

public class SimMonitor : MonoBehaviour {
  private const float _updatePeriod = 0.1f;  // 100 Hz
  private string _telemetryBinPath;
  private string _eventLogPath;
  private Coroutine _monitorRoutine;

  private string _sessionDirectory;

  private FileStream _telemetryFileStream;
  private BinaryWriter _telemetryBinaryWriter;

  [SerializeField]
  private List<EventRecord> _eventLogCache;

  [System.Serializable]
  private class EventRecord {
    public float Time;
    public float PositionX;
    public float PositionY;
    public float PositionZ;
    public string EventType;
    public string Details;
  }

  private void Awake() {
    InitializeSessionDirectory();
  }

  private void Start() {
    SimManager.Instance.OnSimulationStarted += RegisterSimulationStarted;
    SimManager.Instance.OnSimulationEnded += RegisterSimulationEnded;
    SimManager.Instance.OnNewThreat += RegisterNewThreat;
    SimManager.Instance.OnNewInterceptor += RegisterNewInterceptor;
  }

  private void InitializeSessionDirectory() {
    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    _sessionDirectory = Application.persistentDataPath + $"\\Telemetry\\Logs\\{timestamp}";
    Directory.CreateDirectory(_sessionDirectory);
    Debug.Log($"Monitoring simulation logs to {_sessionDirectory}.");
  }

  private void InitializeTelemetryLogFiles() {
    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    _telemetryBinPath = Path.Combine(_sessionDirectory, $"sim_telemetry_{timestamp}.bin");

    // Open the file stream and binary writer for telemetry data
    _telemetryFileStream =
        new FileStream(_telemetryBinPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
    _telemetryBinaryWriter = new BinaryWriter(_telemetryFileStream);

    Debug.Log("Telemetry log file initialized successfully.");
  }

  private void InitializeEventLogFiles() {
    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    _eventLogPath = Path.Combine(_sessionDirectory, $"sim_events_{timestamp}.csv");

    // Initialize the event log cache
    _eventLogCache = new List<EventRecord>();

    Debug.Log("Event log file initialized successfully.");
  }

  private void CloseTelemetryLogFiles() {
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
    float time = SimManager.Instance.ElapsedSimulationTime;
    var agents = SimManager.Instance.Agents.Where(agent => !agent.IsTerminated).ToList();
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

      // Write telemetry data directly to the binary file
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

  public void ConvertBinaryTelemetryToCsv(string binaryFilePath, string csvFilePath) {
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
      }
    } catch (IOException e) {
      Debug.LogWarning(
          $"An IO error occurred while converting binary telemetry to CSV: {e.Message}.");
    }
  }

  private void WriteEventsToFile() {
    using (StreamWriter writer = new StreamWriter(_eventLogPath, false)) {
      // Write CSV header
      writer.WriteLine("Time,PositionX,PositionY,PositionZ,Event,Details");

      foreach (var record in _eventLogCache) {
        writer.WriteLine(
            $"{record.Time:F2},{record.PositionX:F2},{record.PositionY:F2},{record.PositionZ:F2},{record.EventType},{record.Details}");
      }
    }
  }

  private void RegisterSimulationStarted() {
    if (SimManager.Instance.SimulatorConfig.EnableTelemetryLogging) {
      InitializeTelemetryLogFiles();
      _monitorRoutine = StartCoroutine(MonitorRoutine());
    }
    if (SimManager.Instance.SimulatorConfig.EnableEventLogging) {
      InitializeEventLogFiles();
    }
  }

  private void RegisterSimulationEnded() {
    if (SimManager.Instance.SimulatorConfig.EnableTelemetryLogging) {
      StopCoroutine(_monitorRoutine);
      CloseTelemetryLogFiles();
      StartCoroutine(ConvertBinaryTelemetryToCsvCoroutine(
          _telemetryBinPath, Path.ChangeExtension(_telemetryBinPath, ".csv")));
    }
    if (SimManager.Instance.SimulatorConfig.EnableEventLogging) {
      WriteEventsToFile();
    }
  }

  private IEnumerator ConvertBinaryTelemetryToCsvCoroutine(string binaryFilePath,
                                                           string csvFilePath) {
    yield return null;  // Wait for the next frame to ensure RecordTelemetry() has finished
    ConvertBinaryTelemetryToCsv(binaryFilePath, csvFilePath);
  }

  public void RegisterNewThreat(IThreat threat) {
    if (SimManager.Instance.SimulatorConfig.EnableEventLogging) {
      RegisterNewAgent(threat, "NEW_THREAT");
    }
  }

  public void RegisterNewInterceptor(IInterceptor interceptor) {
    if (SimManager.Instance.SimulatorConfig.EnableEventLogging) {
      RegisterNewAgent(interceptor, "NEW_INTERCEPTOR");
      interceptor.OnHit += RegisterInterceptorHit;
      interceptor.OnMiss += RegisterInterceptorMiss;
    }
  }

  private void RegisterNewAgent(IAgent agent, string eventType) {
    float time = SimManager.Instance.ElapsedSimulationTime;
    Vector3 pos = agent.transform.position;
    var record = new EventRecord {
      Time = time,       PositionX = pos.x,     PositionY = pos.y,
      PositionZ = pos.z, EventType = eventType, Details = agent.gameObject.name,
    };
    _eventLogCache.Add(record);
  }

  public void RegisterInterceptorHit(IInterceptor interceptor) {
    if (SimManager.Instance.SimulatorConfig.EnableEventLogging) {
      RegisterInterceptorEvent(interceptor, true);
    }
  }

  public void RegisterInterceptorMiss(IInterceptor interceptor) {
    if (SimManager.Instance.SimulatorConfig.EnableEventLogging) {
      RegisterInterceptorEvent(interceptor, false);
    }
  }

  public void RegisterInterceptorEvent(IInterceptor interceptor, bool hit) {
    float time = SimManager.Instance.ElapsedSimulationTime;
    Vector3 pos = interceptor.transform.position;
    string eventType = hit ? "INTERCEPTOR_HIT" : "INTERCEPTOR_MISS";
    var record = new EventRecord {
      Time = time,       PositionX = pos.x,     PositionY = pos.y,
      PositionZ = pos.z, EventType = eventType, Details = $"{interceptor.gameObject.name}",
    };
    _eventLogCache.Add(record);
  }

  private void OnDestroy() {
    CloseTelemetryLogFiles();
    WriteEventsToFile();
  }
}
