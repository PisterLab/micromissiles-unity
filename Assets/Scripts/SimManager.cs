using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages the simulation by handling missiles, targets, and their assignments.
/// Implements the Singleton pattern to ensure only one instance exists.
/// </summary>
public class SimManager : MonoBehaviour {
  /// <summary>
  /// Singleton instance of SimManager.
  /// </summary>
  public static SimManager Instance { get; private set; }

  /// <summary>
  /// Configuration settings for the simulation.
  /// </summary>
  [SerializeField]
  public SimulationConfig simulationConfig;

  private List<Interceptor> _activeInterceptors = new List<Interceptor>();

  private List<Interceptor> _interceptorObjects = new List<Interceptor>();
  private List<Threat> _threatObjects = new List<Threat>();

  private float _elapsedSimulationTime = 0f;
  private float endTime = 100f;  // Set an appropriate end time
  private bool simulationRunning = false;

  public delegate void SimulationEventHandler();
  public event SimulationEventHandler OnSimulationEnded;
  public event SimulationEventHandler OnSimulationStarted;

  public delegate void NewThreatEventHandler(Threat threat);
  public event NewThreatEventHandler OnNewThreat;

  public delegate void NewInterceptorEventHandler(Interceptor interceptor);
  public event NewInterceptorEventHandler OnNewInterceptor;

  /// <summary>
  /// Gets the elapsed simulation time.
  /// </summary>
  /// <returns>The elapsed time in seconds.</returns>
  public double GetElapsedSimulationTime() {
    return _elapsedSimulationTime;
  }

  public List<Interceptor> GetActiveInterceptors() {
    return _activeInterceptors;
  }

  public List<Threat> GetActiveThreats() {
    return _threatObjects.Where(threat => !threat.IsHit()).ToList();
  }

  public List<Agent> GetActiveAgents() {
    return _activeInterceptors.ConvertAll(interceptor => interceptor as Agent)
        .Concat(GetActiveThreats().ConvertAll(threat => threat as Agent))
        .ToList();
  }

  void Awake() {
    // Ensure only one instance of SimManager exists
    if (Instance == null) {
      Instance = this;
      DontDestroyOnLoad(gameObject);
    } else {
      Destroy(gameObject);
    }
    simulationConfig = ConfigLoader.LoadSimulationConfig("1_salvo_1_hydra_7_drones.json");
    Debug.Log(simulationConfig);
  }

  void Start() {
    // Slow down time by simulationConfig.timeScale
    if (Instance == this) {
      StartSimulation();
      ResumeSimulation();
    }
  }

  public void SetTimeScale(float timeScale) {
    Time.timeScale = timeScale;
    Time.fixedDeltaTime = Time.timeScale * 0.02f;
    Time.maximumDeltaTime = Time.timeScale * 0.15f;
  }

  public void StartSimulation() {
    OnSimulationStarted?.Invoke();
    InitializeSimulation();
  }

  public void PauseSimulation() {
    SetTimeScale(0);
    simulationRunning = false;
  }

  public void ResumeSimulation() {
    SetTimeScale(simulationConfig.timeScale);
    simulationRunning = true;
  }

  public bool IsSimulationRunning() {
    return simulationRunning;
  }

  private void InitializeSimulation() {
    // Invoke the simulation started event to let listeners
    // know to invoke their own handler behavior
    OnSimulationStarted?.Invoke();
    List<Interceptor> missiles = new List<Interceptor>();
    // Create missiles based on config
    foreach (var swarmConfig in simulationConfig.interceptor_swarm_configs) {
      for (int i = 0; i < swarmConfig.num_agents; i++) {
        CreateInterceptor(swarmConfig.dynamic_agent_config);
      }
    }

    List<Threat> targets = new List<Threat>();
    // Create targets based on config
    foreach (var swarmConfig in simulationConfig.threat_swarm_configs) {
      for (int i = 0; i < swarmConfig.num_agents; i++) {
        CreateThreat(swarmConfig.dynamic_agent_config);
      }
    }
  }

  public void AssignInterceptorsToThreats() {
    IADS.Instance.AssignInterceptorsToThreats(_interceptorObjects);
  }

  public void RegisterInterceptorHit(Interceptor interceptor, Threat threat) {
    if (interceptor is Interceptor missileComponent) {
      _activeInterceptors.Remove(missileComponent);
    }
  }

  public void RegisterInterceptorMiss(Interceptor interceptor, Threat threat) {
    if (interceptor is Interceptor missileComponent) {
      _activeInterceptors.Remove(missileComponent);
    }
  }

  public void RegisterThreatHit(Interceptor interceptor, Threat threat) {
    // Placeholder
  }

  public void RegisterThreatMiss(Interceptor interceptor, Threat threat) {
    Debug.Log($"RegisterThreatMiss: Interceptor {interceptor.name} missed threat {threat.name}");
    // Placeholder
  }

  /// <summary>
  /// Creates a interceptor based on the provided configuration.
  /// </summary>
  /// <param name="config">Configuration settings for the interceptor.</param>
  /// <returns>The created Interceptor instance, or null if creation failed.</returns>
  public Interceptor CreateInterceptor(DynamicAgentConfig config) {
    string interceptorModelFile = config.agent_model;
    interceptorModelFile = "Interceptors/" + interceptorModelFile;
    StaticAgentConfig interceptorStaticAgentConfig =
        ConfigLoader.LoadStaticAgentConfig(interceptorModelFile);
    string agentClass = interceptorStaticAgentConfig.agentClass;
    // The interceptor class corresponds to the Prefab that must
    // exist in the Resources/Prefabs folder
    GameObject interceptorObject = CreateAgent(config, agentClass);

    if (interceptorObject == null)
      return null;

    // Interceptor-specific logic
    switch (config.dynamic_config.sensor_config.type) {
      case SensorType.IDEAL:
        interceptorObject.AddComponent<IdealSensor>();
        break;
      default:
        Debug.LogError($"Sensor type '{config.dynamic_config.sensor_config.type}' not found.");
        break;
    }

    Interceptor interceptor = interceptorObject.GetComponent<Interceptor>();
    _interceptorObjects.Add(interceptor);
    _activeInterceptors.Add(interceptor);

    // Set the static agent config
    interceptor.SetStaticAgentConfig(interceptorStaticAgentConfig);

    // Subscribe events
    interceptor.OnInterceptHit += RegisterInterceptorHit;
    interceptor.OnInterceptMiss += RegisterInterceptorMiss;

    // Assign a unique and simple ID
    int interceptorId = _interceptorObjects.Count;
    interceptorObject.name = $"{interceptorStaticAgentConfig.name}_Interceptor_{interceptorId}";

    // Let listeners know a new interceptor has been created
    OnNewInterceptor?.Invoke(interceptor);

    return interceptorObject.GetComponent<Interceptor>();
  }

  /// <summary>
  /// Creates a threat based on the provided configuration.
  /// </summary>
  /// <param name="config">Configuration settings for the threat.</param>
  /// <returns>The created Threat instance, or null if creation failed.</returns>
  private Threat CreateThreat(DynamicAgentConfig config) {
    string threatModelFile = config.agent_model;
    threatModelFile = "Threats/" + threatModelFile;
    StaticAgentConfig threatStaticAgentConfig = ConfigLoader.LoadStaticAgentConfig(threatModelFile);
    string agentClass = threatStaticAgentConfig.agentClass;
    // The threat class corresponds to the Prefab that must
    // exist in the Resources/Prefabs folder
    GameObject threatObject = CreateAgent(config, agentClass);

    if (threatObject == null)
      return null;

    Threat threat = threatObject.GetComponent<Threat>();
    _threatObjects.Add(threat);

    // Set the static agent config
    threat.SetStaticAgentConfig(threatStaticAgentConfig);

    // Subscribe events
    threat.OnInterceptHit += RegisterThreatHit;
    threat.OnInterceptMiss += RegisterThreatMiss;

    // Assign a unique and simple ID
    int threatId = _threatObjects.Count;
    threatObject.name = $"{threatStaticAgentConfig.name}_Threat_{threatId}";

    // Let listeners know a new threat has been created
    OnNewThreat?.Invoke(threat);

    return threatObject.GetComponent<Threat>();
  }

  /// <summary>
  /// Creates an agent (interceptor or threat) based on the provided configuration and prefab name.
  /// </summary>
  /// <param name="config">Configuration settings for the agent.</param>
  /// <param name="prefabName">Name of the prefab to instantiate.</param>
  /// <returns>The created GameObject instance, or null if creation failed.</returns>
  public GameObject CreateAgent(DynamicAgentConfig config, string prefabName) {
    GameObject prefab = Resources.Load<GameObject>($"Prefabs/{prefabName}");
    if (prefab == null) {
      Debug.LogError($"Prefab '{prefabName}' not found in Resources/Prefabs folder.");
      return null;
    }

    Vector3 noiseOffset = Utilities.GenerateRandomNoise(config.standard_deviation.position);
    Vector3 noisyPosition = config.initial_state.position + noiseOffset;

    GameObject agentObject =
        Instantiate(prefab, noisyPosition, Quaternion.Euler(config.initial_state.rotation));

    Rigidbody agentRigidbody = agentObject.GetComponent<Rigidbody>();
    Vector3 velocityNoise = Utilities.GenerateRandomNoise(config.standard_deviation.velocity);
    Vector3 noisyVelocity = config.initial_state.velocity + velocityNoise;
    agentRigidbody.linearVelocity = noisyVelocity;

    agentObject.GetComponent<Agent>().SetDynamicAgentConfig(config);

    return agentObject;
  }

  public void LoadNewConfig(string configFileName) {
    simulationConfig = ConfigLoader.LoadSimulationConfig(configFileName);
    if (simulationConfig != null) {
      Debug.Log($"Loaded new configuration: {configFileName}");
      RestartSimulation();
    } else {
      Debug.LogError($"Failed to load configuration: {configFileName}");
    }
  }

  public void RestartSimulation() {
    OnSimulationEnded?.Invoke();
    Debug.Log("Simulation ended");
    // Reset simulation time
    _elapsedSimulationTime = 0f;
    simulationRunning = IsSimulationRunning();

    // Clear existing interceptors and threats
    foreach (var interceptor in _interceptorObjects) {
      if (interceptor != null) {
        Destroy(interceptor.gameObject);
      }
    }

    foreach (var threat in _threatObjects) {
      if (threat != null) {
        Destroy(threat.gameObject);
      }
    }

    _interceptorObjects.Clear();
    _activeInterceptors.Clear();
    _threatObjects.Clear();

    StartSimulation();
  }

  void Update() {
    // Check if all missiles have terminated
    bool allInterceptorsTerminated = true;
    foreach (var interceptor in _interceptorObjects) {
      if (interceptor != null && interceptor.GetFlightPhase() != Agent.FlightPhase.TERMINATED) {
        allInterceptorsTerminated = false;
        break;
      }
    }
    // If all missiles have terminated, restart the simulation
    if (allInterceptorsTerminated) {
      RestartSimulation();
    }

    if (simulationRunning && _elapsedSimulationTime < endTime) {
      _elapsedSimulationTime += Time.deltaTime;
    } else if (_elapsedSimulationTime >= endTime) {
      simulationRunning = false;
      Debug.Log("Simulation completed.");
    }
  }
}
