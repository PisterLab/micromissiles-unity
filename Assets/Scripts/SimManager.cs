using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages the simulation by handling missiles, targets, and their assignments.
/// Implements the Singleton pattern to ensure only one instance exists.
/// </summary>
public class SimManager : MonoBehaviour {
  public SimulatorConfig simulatorConfig;

  /// <summary>
  /// Singleton instance of SimManager.
  /// </summary>
  public static SimManager Instance { get; set; }

  /// <summary>
  /// Configuration settings for the simulation.
  /// </summary>
  [SerializeField]
  public SimulationConfig simulationConfig;

  public string defaultConfig = "1_salvo_1_hydra_7_drones.json";

  private List<Interceptor> _activeInterceptors = new List<Interceptor>();

  private List<Interceptor> _interceptorObjects = new List<Interceptor>();
  private List<Threat> _threatObjects = new List<Threat>();

  private List<GameObject> _dummyAgentObjects = new List<GameObject>();
  private Dictionary<(Vector3, Vector3), GameObject> _dummyAgentTable =
      new Dictionary<(Vector3, Vector3), GameObject>();

  private List<List<Agent>> _interceptorSwarms = new List<List<Agent>>();
  private List<List<Agent>> _threatSwarms = new List<List<Agent>>();

  private float _elapsedSimulationTime = 0f;
  private float endTime = 100f;  // Set an appropriate end time
  private bool simulationRunning = false;

  private float _costLaunchedInterceptors = 0f;
  private float _costDestroyedThreats = 0f;

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

  /// <summary>
  /// Gets the total cost of launched interceptors.
  /// </summary>
  /// <returns>The total cost of launched interceptors.</returns>
  public double GetCostLaunchedInterceptors() {
    return _costLaunchedInterceptors;
  }

  /// <summary>
  /// Gets the total cost of destroyed threats.
  /// </summary>
  /// <returns>The total cost of destroyed threats.</returns>
  public double GetCostDestroyedThreats() {
    return _costDestroyedThreats;
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

    // Load the simulator config
    simulatorConfig = ConfigLoader.LoadSimulatorConfig();
    Time.fixedDeltaTime = (float)(1.0f / simulatorConfig.physicsUpdateRate);
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
    Time.maximumDeltaTime = Time.timeScale * 0.05f;

    // Time.fixedDeltaTime is set in the simulator.json
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
      List<Agent> swarm = new List<Agent>();
      for (int i = 0; i < swarmConfig.num_agents; i++) {
        Interceptor interceptor = CreateInterceptor(swarmConfig.dynamic_agent_config);
        swarm.Add(interceptor);
      }
      _interceptorSwarms.Add(swarm);
    }
    IADS.Instance.RequestThreatAssignment(_interceptorObjects);

    List<Agent> targets = new List<Agent>();
    // Create targets based on config
    foreach (var swarmConfig in simulationConfig.threat_swarm_configs) {
      List<Agent> swarm = new List<Agent>();
      for (int i = 0; i < swarmConfig.num_agents; i++) {
        Threat threat = CreateThreat(swarmConfig.dynamic_agent_config);
        swarm.Add(threat);
      }
      _threatSwarms.Add(swarm);
    }
  }

  public Vector3 GetAllAgentsCenter() {
    List<Agent> allAgents = _interceptorObjects.ConvertAll(interceptor => interceptor as Agent)
                                .Concat(_threatObjects.ConvertAll(threat => threat as Agent))
                                .ToList();
    return GetSwarmCenter(allAgents);
  }

  public Vector3 GetSwarmCenter(List<Agent> swarm) {
    if (swarm.Count == 0) {
      return Vector3.zero;
    }

    Vector3 sum = Vector3.zero;
    int count = 0;
    int swarmCount = swarm.Count;

    for (int i = 0; i < swarmCount; i++) {
      Agent agent = swarm[i];
      if (!agent.IsHit()) {
        sum += agent.transform.position;
        count++;
      }
    }

    return count > 0 ? sum / count : Vector3.zero;
  }

  public List<List<Agent>> GetInterceptorSwarms() {
    return _interceptorSwarms;
  }

  public List<List<Agent>> GetThreatSwarms() {
    return _threatSwarms;
  }

  public void AssignInterceptorsToThreats() {
    IADS.Instance.AssignInterceptorsToThreats(_interceptorObjects);
  }

  public void RegisterInterceptorHit(Interceptor interceptor, Threat threat) {
    _costDestroyedThreats += threat.staticAgentConfig.unitCost;
    if (interceptor is Interceptor missileComponent) {
      _activeInterceptors.Remove(missileComponent);
    }
  }

  public void RegisterInterceptorMiss(Interceptor interceptor, Threat threat) {
    if (interceptor is Interceptor missileComponent) {
      _activeInterceptors.Remove(missileComponent);
    }
  }

  public void RegisterThreatHit(Threat threat) {
    // Placeholder
  }

  public void RegisterThreatMiss(Threat threat) {
    // Placeholder
  }

  private AttackBehavior LoadAttackBehavior(DynamicAgentConfig config) {
    string threatBehaviorFile = config.attack_behavior;
    AttackBehavior attackBehavior = AttackBehavior.FromJson(threatBehaviorFile);
    switch (attackBehavior.attackBehaviorType) {
      case AttackBehavior.AttackBehaviorType.DIRECT_ATTACK:
        return DirectAttackBehavior.FromJson(threatBehaviorFile);
      default:
        Debug.LogError($"Attack behavior type '{attackBehavior.attackBehaviorType}' not found.");
        return null;
    }
  }

  public Agent CreateDummyAgent(Vector3 position, Vector3 velocity) {
    if (_dummyAgentTable.ContainsKey((position, velocity))) {
      return _dummyAgentTable[(position, velocity)].GetComponent<Agent>();
    }
    GameObject dummyAgentPrefab = Resources.Load<GameObject>($"Prefabs/DummyAgent");
    GameObject dummyAgentObject = Instantiate(dummyAgentPrefab, position, Quaternion.identity);
    if (!dummyAgentObject.TryGetComponent<Agent>(out _)) {
      dummyAgentObject.AddComponent<DummyAgent>();
    }
    Rigidbody dummyRigidbody = dummyAgentObject.GetComponent<Rigidbody>();
    dummyRigidbody.linearVelocity = velocity;
    _dummyAgentObjects.Add(dummyAgentObject);
    _dummyAgentTable[(position, velocity)] = dummyAgentObject;
    return dummyAgentObject.GetComponent<Agent>();
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

    // Add the interceptor's unit cost to the total cost
    _costLaunchedInterceptors += interceptorStaticAgentConfig.unitCost;

    // Let listeners know a new interceptor has been created
    OnNewInterceptor?.Invoke(interceptor);

    return interceptor;
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

    // Set the attack behavior
    AttackBehavior attackBehavior = LoadAttackBehavior(config);
    threat.SetAttackBehavior(attackBehavior);

    // Subscribe events
    threat.OnThreatHit += RegisterThreatHit;
    threat.OnThreatMiss += RegisterThreatMiss;

    // Assign a unique and simple ID
    int threatId = _threatObjects.Count;
    threatObject.name = $"{threatStaticAgentConfig.name}_Threat_{threatId}";

    // Threats always start in midcourse
    threat.SetFlightPhase(Agent.FlightPhase.MIDCOURSE);

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

    GameObject agentObject = Instantiate(prefab, noisyPosition, Quaternion.identity);

    Rigidbody agentRigidbody = agentObject.GetComponent<Rigidbody>();
    Vector3 velocityNoise = Utilities.GenerateRandomNoise(config.standard_deviation.velocity);
    Vector3 noisyVelocity = config.initial_state.velocity + velocityNoise;
    agentRigidbody.linearVelocity = noisyVelocity;
    agentObject.GetComponent<Agent>().SetInitialVelocity(noisyVelocity);
    // Set rotation to face the initial velocity with noise
    Vector3 velocityDirection = noisyVelocity.normalized;
    Quaternion targetRotation = Quaternion.LookRotation(velocityDirection, Vector3.up);
    agentObject.transform.rotation = targetRotation;

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
    _costLaunchedInterceptors = 0f;
    _costDestroyedThreats = 0f;

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

    foreach (var dummyAgent in _dummyAgentObjects) {
      if (dummyAgent != null) {
        Destroy(dummyAgent);
      }
    }

    _interceptorObjects.Clear();
    _activeInterceptors.Clear();
    _threatObjects.Clear();
    _dummyAgentObjects.Clear();
    _dummyAgentTable.Clear();
    _interceptorSwarms.Clear();
    _threatSwarms.Clear();
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
  }

  void FixedUpdate() {
    if (simulationRunning && _elapsedSimulationTime < endTime) {
      _elapsedSimulationTime += Time.deltaTime;
    } else if (_elapsedSimulationTime >= endTime) {
      simulationRunning = false;
      RestartSimulation();
      Debug.Log("Simulation completed.");
    }
  }
}

[System.Serializable]
public class SimulatorConfig {
  public bool enableLogging;
  public bool enableMissileTrailEffect;
  public bool enableExplosionEffect;
  public int physicsUpdateRate;
  public bool persistentFlightTrails;
}
