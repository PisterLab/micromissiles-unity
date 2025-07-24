using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

public class SimMonitor : MonoBehaviour {
  private const float _updateRate = 0.1f;  // 100 Hz
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
    Debug.Log($"Monitoring simulation logs to {_sessionDirectory}");
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
      yield return new WaitForSeconds(_updateRate);
    }
  }

  private void RecordTelemetry() {
    float time = (float)SimManager.Instance.GetElapsedSimulationTime();
    var agents = SimManager.Instance.GetActiveAgents();
    if (_telemetryBinaryWriter == null) {
      Debug.LogWarning("Telemetry binary writer is null");
      return;
    }
    for (int i = 0; i < agents.Count; ++i) {
      var agent = agents[i];

      if (!agent.gameObject.activeInHierarchy)
        continue;

      Vector3 pos = agent.transform.position;

      if (pos == Vector3.zero)
        continue;

      Vector3 vel = agent.GetVelocity();  // Ensure GetVelocity() doesn't allocate

      int agentID = agent.GetInstanceID();
      int flightPhase =
          agent is AirborneAgent airborneAgent ? (int)airborneAgent.GetFlightPhase() : -1;
      byte agentType = (byte)(agent is Threat ? 0 : 1);

      // Write telemetry data directly to the binary file
      _telemetryBinaryWriter.Write(time);
      _telemetryBinaryWriter.Write(agentID);
      _telemetryBinaryWriter.Write(pos.x);
      _telemetryBinaryWriter.Write(pos.y);
      _telemetryBinaryWriter.Write(pos.z);
      _telemetryBinaryWriter.Write(vel.x);
      _telemetryBinaryWriter.Write(vel.y);
      _telemetryBinaryWriter.Write(vel.z);
      _telemetryBinaryWriter.Write(flightPhase);
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
        // Write CSV header
        writer.WriteLine(
            "Time,AgentID,AgentX,AgentY,AgentZ,AgentVX,AgentVY,AgentVZ,AgentState,AgentType");

        while (reader.BaseStream.Position != reader.BaseStream.Length) {
          float time = reader.ReadSingle();
          int agentID = reader.ReadInt32();
          float posX = reader.ReadSingle();
          float posY = reader.ReadSingle();
          float posZ = reader.ReadSingle();
          float velX = reader.ReadSingle();
          float velY = reader.ReadSingle();
          float velZ = reader.ReadSingle();
          int flightPhase = reader.ReadInt32();
          byte agentTypeByte = reader.ReadByte();
          string agentType = agentTypeByte == 0 ? "T" : "M";

          // Write the data to CSV
          writer.WriteLine(
              $"{time:F2},{agentID},{posX:F2},{posY:F2},{posZ:F2},{velX:F2},{velY:F2},{velZ:F2},{flightPhase},{agentType}");
        }
      }
    } catch (IOException e) {
      Debug.LogWarning(
          $"An IO error occurred while converting binary telemetry to CSV: {e.Message}");
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
    if (SimManager.Instance.simulatorConfig.enableTelemetryLogging) {
      InitializeTelemetryLogFiles();
      _monitorRoutine = StartCoroutine(MonitorRoutine());
    }
    if (SimManager.Instance.simulatorConfig.enableEventLogging) {
      InitializeEventLogFiles();
    }
  }

  private void RegisterSimulationEnded() {
    if (SimManager.Instance.simulatorConfig.enableTelemetryLogging) {
      StopCoroutine(_monitorRoutine);
      CloseTelemetryLogFiles();
      StartCoroutine(ConvertBinaryTelemetryToCsvCoroutine(
          _telemetryBinPath, Path.ChangeExtension(_telemetryBinPath, ".csv")));
    }
    if (SimManager.Instance.simulatorConfig.enableEventLogging) {
      WriteEventsToFile();
    }
  }

  private IEnumerator ConvertBinaryTelemetryToCsvCoroutine(string binaryFilePath,
                                                           string csvFilePath) {
    yield return null;  // Wait for the next frame to ensure RecordTelemetry() has finished
    ConvertBinaryTelemetryToCsv(binaryFilePath, csvFilePath);
  }

  public void RegisterNewThreat(Threat threat) {
    if (SimManager.Instance.simulatorConfig.enableEventLogging) {
      RegisterNewAgent(threat, "NEW_THREAT");
    }
  }

  public void RegisterNewInterceptor(Interceptor interceptor) {
    if (SimManager.Instance.simulatorConfig.enableEventLogging) {
      RegisterNewAgent(interceptor, "NEW_INTERCEPTOR");
      interceptor.OnInterceptMiss += RegisterInterceptorMiss;
      interceptor.OnInterceptHit += RegisterInterceptorHit;
    }
  }

  private void RegisterNewAgent(Agent agent, string eventType) {
    float time = (float)SimManager.Instance.GetElapsedSimulationTime();
    Vector3 pos = agent.transform.position;
    var record = new EventRecord { Time = time,       PositionX = pos.x,     PositionY = pos.y,
                                   PositionZ = pos.z, EventType = eventType, Details = agent.name };
    _eventLogCache.Add(record);
  }

  public void RegisterInterceptorHit(Interceptor interceptor, Threat threat) {
    if (SimManager.Instance.simulatorConfig.enableEventLogging) {
      RegisterInterceptorEvent(interceptor, threat, true);
    }
  }

  public void RegisterInterceptorMiss(Interceptor interceptor, Threat threat) {
    if (SimManager.Instance.simulatorConfig.enableEventLogging) {
      RegisterInterceptorEvent(interceptor, threat, false);
    }
  }

  public void RegisterInterceptorEvent(Interceptor interceptor, Threat threat, bool hit) {
    float time = (float)SimManager.Instance.GetElapsedSimulationTime();
    Vector3 pos = interceptor.transform.position;
    string eventType = hit ? "INTERCEPTOR_HIT" : "INTERCEPTOR_MISS";
    var record = new EventRecord {
      Time = time,       PositionX = pos.x,     PositionY = pos.y,
      PositionZ = pos.z, EventType = eventType, Details = $"{interceptor.name} and {threat.name}"
    };
    _eventLogCache.Add(record);
  }

  private void OnDestroy() {
    CloseTelemetryLogFiles();
    WriteEventsToFile();
  }
}
