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
  public SimulationConfig SimulationConfig;

  private string _defaultConfig = "7_quadcopters.json";

  private List<Interceptor> _activeInterceptors = new List<Interceptor>();

  private List<Interceptor> _interceptorObjects = new List<Interceptor>();
  private List<Threat> _threatObjects = new List<Threat>();

  private List<GameObject> _dummyAgentObjects = new List<GameObject>();
  private Dictionary<(Vector3, Vector3), GameObject> _dummyAgentTable =
      new Dictionary<(Vector3, Vector3), GameObject>();

  // Inclusive of all, including submunitions swarms.
  // The boolean indicates whether the agent is active (true) or inactive (false).
  private List<List<(Agent, bool)>> _interceptorSwarms = new List<List<(Agent, bool)>>();
  private List<List<(Agent, bool)>> _submunitionsSwarms = new List<List<(Agent, bool)>>();
  private List<List<(Agent, bool)>> _threatSwarms = new List<List<(Agent, bool)>>();

  private Dictionary<Agent, List<(Agent, bool)>> _interceptorSwarmMap =
      new Dictionary<Agent, List<(Agent, bool)>>();

  private Dictionary<Agent, List<(Agent, bool)>> _submunitionsSwarmMap =
      new Dictionary<Agent, List<(Agent, bool)>>();
  private Dictionary<Agent, List<(Agent, bool)>> _threatSwarmMap =
      new Dictionary<Agent, List<(Agent, bool)>>();

  // Maps a submunition swarm to its corresponding interceptor swarm.
  private Dictionary<List<(Agent, bool)>, List<(Agent, bool)>> _submunitionInterceptorSwarmMap =
      new Dictionary<List<(Agent, bool)>, List<(Agent, bool)>>();

  // Events to subscribe to for changes in each of the swarm tables.
  public delegate void SwarmEventHandler(List<List<(Agent, bool)>> swarm);
  public event SwarmEventHandler OnInterceptorSwarmChanged;
  public event SwarmEventHandler OnSubmunitionsSwarmChanged;
  public event SwarmEventHandler OnThreatSwarmChanged;
  //////////////////////////////////////////////////////////////////////

  private float _elapsedSimulationTime = 0f;
  private bool _isSimulationPaused = false;

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
    return _threatObjects.Where(threat => !threat.IsTerminated()).ToList();
  }

  public List<Agent> GetActiveAgents() {
    return _activeInterceptors.ConvertAll(interceptor => interceptor as Agent)
        .Concat(GetActiveThreats().ConvertAll(threat => threat as Agent))
        .ToList();
  }

  public string GenerateSwarmTitle(List<(Agent, bool)> swarm, int index) {
    string swarmTitle = swarm[0].Item1.name;
    swarmTitle = swarmTitle.Split('_')[0];
    swarmTitle += $"_{index}";
    return swarmTitle;
  }

  public string GenerateInterceptorSwarmTitle(List<(Agent, bool)> swarm) {
    return GenerateSwarmTitle(swarm, _interceptorSwarms.IndexOf(swarm));
  }

  public string GenerateSubmunitionsSwarmTitle(List<(Agent, bool)> swarm) {
    return GenerateSwarmTitle(swarm, LookupSubmunitionSwarmIndexInInterceptorSwarm(swarm));
  }

  public string GenerateThreatSwarmTitle(List<(Agent, bool)> swarm) {
    return GenerateSwarmTitle(swarm, _threatSwarms.IndexOf(swarm));
  }

  void Awake() {
    // Ensure only one instance of SimManager exists
    if (Instance == null) {
      Instance = this;
      DontDestroyOnLoad(gameObject);
    } else {
      Destroy(gameObject);
    }
    SimulationConfig = ConfigLoader.LoadSimulationConfig(_defaultConfig);
    simulatorConfig = ConfigLoader.LoadSimulatorConfig();
    Debug.Log(SimulationConfig);
  }

  void Start() {
    if (Instance == this) {
      _isSimulationPaused = false;
      StartSimulation();
      ResumeSimulation();
    }
  }

  void Update() {}

  public void SetTimeScale(float timeScale) {
    Time.timeScale = timeScale;
    // Time.fixedDeltaTime is derived from simulator.json.
    Time.maximumDeltaTime = Time.fixedDeltaTime * 3;
  }

  public void StartSimulation() {
    InitializeSimulation();

    // Invoke the simulation started event to let listeners know to invoke their own handler
    // behavior.
    UIManager.Instance.LogActionMessage("[SIM] Simulation started.");
    OnSimulationStarted?.Invoke();
  }

  public void PauseSimulation() {
    SetTimeScale(0);
    Time.fixedDeltaTime = 0;
    _isSimulationPaused = true;
  }

  public void ResumeSimulation() {
    Time.fixedDeltaTime = (float)(1.0f / simulatorConfig.physicsUpdateRate);
    SetTimeScale(SimulationConfig.timeScale);
    _isSimulationPaused = false;
  }

  public bool IsSimulationPaused() {
    return _isSimulationPaused;
  }

  private void InitializeSimulation() {
    if (!IsSimulationPaused()) {
      // If the simulation was not paused, we need to update the time scale.
      SetTimeScale(SimulationConfig.timeScale);
      Time.fixedDeltaTime = (float)(1.0f / simulatorConfig.physicsUpdateRate);
      // If the simulation WAS paused, then ResumeSimulation will handle updating the time scale and
      // fixed delta time from the newly loaded config files.
    }
    // Create targets based on the configuration.
    List<Agent> targets = new List<Agent>();
    foreach (var swarmConfig in SimulationConfig.threat_swarm_configs) {
      List<Agent> swarm = new List<Agent>();
      for (int i = 0; i < swarmConfig.num_agents; ++i) {
        Threat threat = CreateThreat(swarmConfig.dynamic_agent_config);
        swarm.Add(threat);
      }
      AddThreatSwarm(swarm);
    }
  }

  public void AddInterceptorSwarm(List<Agent> swarm) {
    List<(Agent, bool)> swarmTuple = swarm.ConvertAll(agent => (agent, true));
    _interceptorSwarms.Add(swarmTuple);
    foreach (var interceptor in swarm) {
      _interceptorSwarmMap[interceptor] = swarmTuple;
    }
    OnInterceptorSwarmChanged?.Invoke(_interceptorSwarms);
  }

  public void AddSubmunitionsSwarm(List<Agent> swarm) {
    List<(Agent, bool)> swarmTuple = swarm.ConvertAll(agent => (agent, true));
    _submunitionsSwarms.Add(swarmTuple);
    foreach (var submunition in swarm) {
      _submunitionsSwarmMap[submunition] = swarmTuple;
    }
    AddInterceptorSwarm(swarm);
    _submunitionInterceptorSwarmMap[swarmTuple] = _interceptorSwarms[_interceptorSwarms.Count - 1];
    OnSubmunitionsSwarmChanged?.Invoke(_submunitionsSwarms);
  }

  public int LookupSubmunitionSwarmIndexInInterceptorSwarm(List<(Agent, bool)> swarm) {
    if (_submunitionInterceptorSwarmMap.TryGetValue(swarm, out var interceptorSwarm)) {
      return _interceptorSwarms.IndexOf(interceptorSwarm);
    }
    // Return -1 if the swarm is not found.
    return -1;
  }

  public void AddThreatSwarm(List<Agent> swarm) {
    List<(Agent, bool)> swarmTuple = swarm.ConvertAll(agent => (agent, true));
    _threatSwarms.Add(swarmTuple);
    foreach (var threat in swarm) {
      _threatSwarmMap[threat] = swarmTuple;
    }
    OnThreatSwarmChanged?.Invoke(_threatSwarms);
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

    for (int i = 0; i < swarmCount; ++i) {
      Agent agent = swarm[i];
      if (!agent.IsTerminated()) {
        sum += agent.transform.position;
        ++count;
      }
    }

    return count > 0 ? sum / count : Vector3.zero;
  }

  public List<List<(Agent, bool)>> GetInterceptorSwarms() {
    return _interceptorSwarms;
  }

  public List<List<(Agent, bool)>> GetSubmunitionsSwarms() {
    return _submunitionsSwarms;
  }

  public List<List<(Agent, bool)>> GetThreatSwarms() {
    return _threatSwarms;
  }

  public void DestroyInterceptorInSwarm(Interceptor interceptor) {
    var swarm = _interceptorSwarmMap[interceptor];
    int index = swarm.FindIndex(tuple => tuple.Item1 == interceptor);
    if (index != -1) {
      swarm[index] = (swarm[index].Item1, false);
      OnInterceptorSwarmChanged?.Invoke(_interceptorSwarms);
    } else {
      Debug.LogError("Interceptor not found in swarm.");
    }
    if (swarm.All(tuple => !tuple.Item2)) {
      // Need to give the camera controller a way to update to the next swarm if it exists.
      if (CameraController.Instance.cameraMode == CameraMode.FOLLOW_INTERCEPTOR_SWARM) {
        CameraController.Instance.FollowNextInterceptorSwarm();
      }
    }

    // If this also happens to be a submunition, destroy it in the submunition swarm.
    if (_submunitionsSwarmMap.ContainsKey(interceptor)) {
      DestroySubmunitionInSwarm(interceptor);
    }
  }

  public void DestroySubmunitionInSwarm(Interceptor submunition) {
    var swarm = _submunitionsSwarmMap[submunition];
    int index = swarm.FindIndex(tuple => tuple.Item1 == submunition);
    if (index != -1) {
      swarm[index] = (swarm[index].Item1, false);
      OnSubmunitionsSwarmChanged?.Invoke(_submunitionsSwarms);
    }
  }

  public void DestroyThreatInSwarm(Threat threat) {
    var swarm = _threatSwarmMap[threat];
    int index = swarm.FindIndex(tuple => tuple.Item1 == threat);
    if (index != -1) {
      swarm[index] = (swarm[index].Item1, false);
      OnThreatSwarmChanged?.Invoke(_threatSwarms);
    }
    if (swarm.All(tuple => !tuple.Item2)) {
      _threatSwarms.Remove(swarm);
      if (CameraController.Instance.cameraMode == CameraMode.FOLLOW_THREAT_SWARM) {
        CameraController.Instance.FollowNextThreatSwarm();
      }
    }
  }

  public void RegisterInterceptorHit(Interceptor interceptor, Threat threat) {
    _costDestroyedThreats += threat.staticAgentConfig.unitCost;
    if (interceptor is Interceptor missileComponent) {
      _activeInterceptors.Remove(missileComponent);
    }
    DestroyInterceptorInSwarm(interceptor);
    DestroyThreatInSwarm(threat);
  }

  public void RegisterInterceptorMiss(Interceptor interceptor, Threat threat) {
    if (interceptor is Interceptor missileComponent) {
      _activeInterceptors.Remove(missileComponent);
    }
    DestroyInterceptorInSwarm(interceptor);
  }

  public void RegisterThreatHit(Threat threat) {
    DestroyThreatInSwarm(threat);
  }

  public void RegisterThreatMiss(Threat threat) {
    DestroyThreatInSwarm(threat);
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
  /// <param name="initialState">Initial state of the interceptor.</param>
  /// <returns>The created Interceptor instance, or null if creation failed.</returns>
  public Interceptor CreateInterceptor(DynamicAgentConfig config, InitialState initialState) {
    string interceptorModelFile = config.agent_model;
    interceptorModelFile = "Interceptors/" + interceptorModelFile;
    StaticAgentConfig interceptorStaticAgentConfig =
        ConfigLoader.LoadStaticAgentConfig(interceptorModelFile);
    string agentClass = interceptorStaticAgentConfig.agentClass;
    // The interceptor class corresponds to the Prefab that must exist in the Resources/Prefabs
    // folder.
    GameObject interceptorObject = CreateAgent(config, initialState, agentClass);

    if (interceptorObject == null)
      return null;

    // Interceptor-specific logic.
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

    // Set the static agent config.
    interceptor.SetStaticAgentConfig(interceptorStaticAgentConfig);

    // Subscribe events.
    interceptor.OnInterceptHit += RegisterInterceptorHit;
    interceptor.OnInterceptMiss += RegisterInterceptorMiss;

    // Assign a unique and simple ID.
    int interceptorId = _interceptorObjects.Count;
    interceptorObject.name = $"{interceptorStaticAgentConfig.name}_Interceptor_{interceptorId}";

    // Add the interceptor's unit cost to the total cost.
    _costLaunchedInterceptors += interceptorStaticAgentConfig.unitCost;

    // Let listeners know a new interceptor has been created.
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
    // The threat class corresponds to the Prefab that must exist in the Resources/Prefabs folder.
    GameObject threatObject = CreateRandomAgent(config, agentClass);

    if (threatObject == null)
      return null;

    Threat threat = threatObject.GetComponent<Threat>();
    _threatObjects.Add(threat);

    // Set the static agent config.
    threat.SetStaticAgentConfig(threatStaticAgentConfig);

    // Set the attack behavior.
    AttackBehavior attackBehavior = LoadAttackBehavior(config);
    threat.SetAttackBehavior(attackBehavior);

    // Subscribe events.
    threat.OnThreatHit += RegisterThreatHit;
    threat.OnThreatMiss += RegisterThreatMiss;

    // Assign a unique and simple ID.
    int threatId = _threatObjects.Count;
    threatObject.name = $"{threatStaticAgentConfig.name}_Threat_{threatId}";

    // Let listeners know that a new threat has been created.
    OnNewThreat?.Invoke(threat);

    return threatObject.GetComponent<Threat>();
  }

  /// <summary>
  /// Creates a agent based on the provided configuration and prefab name.
  /// </summary>
  /// <param name="config">Configuration settings for the agent.</param>
  /// <param name="initialState">Initial state of the agent.</param>
  /// <param name="prefabName">Name of the prefab to instantiate.</param>
  /// <returns>The created GameObject instance, or null if creation failed.</returns>
  public GameObject CreateAgent(DynamicAgentConfig config, InitialState initialState,
                                string prefabName) {
    GameObject prefab = Resources.Load<GameObject>($"Prefabs/{prefabName}");
    if (prefab == null) {
      Debug.LogError($"Prefab '{prefabName}' not found in Resources/Prefabs folder.");
      return null;
    }

    // Set the position.
    GameObject agentObject = Instantiate(prefab, initialState.position, Quaternion.identity);

    // Set the velocity. The rigid body is frozen while the agent is in the initialized phase.
    agentObject.GetComponent<Agent>().SetInitialVelocity(initialState.velocity);

    // Set the rotation to face the initial velocity.
    Vector3 velocityDirection = initialState.velocity.normalized;
    Quaternion targetRotation = Quaternion.LookRotation(velocityDirection, Vector3.up);
    agentObject.transform.rotation = targetRotation;

    agentObject.GetComponent<Agent>().SetDynamicAgentConfig(config);
    return agentObject;
  }

  /// <summary>
  /// Creates a random agent based on the provided configuration and prefab name.
  /// </summary>
  /// <param name="config">Configuration settings for the agent.</param>
  /// <param name="prefabName">Name of the prefab to instantiate.</param>
  /// <returns>The created GameObject instance, or null if creation failed.</returns>
  public GameObject CreateRandomAgent(DynamicAgentConfig config, string prefabName) {
    GameObject prefab = Resources.Load<GameObject>($"Prefabs/{prefabName}");
    if (prefab == null) {
      Debug.LogError($"Prefab '{prefabName}' not found in Resources/Prefabs folder.");
      return null;
    }

    // Randomize the initial state.
    InitialState initialState = new InitialState();

    // Randomize the position.
    Vector3 positionNoise = Utilities.GenerateRandomNoise(config.standard_deviation.position);
    initialState.position = config.initial_state.position + positionNoise;

    // Randomize the velocity.
    Vector3 velocityNoise = Utilities.GenerateRandomNoise(config.standard_deviation.velocity);
    initialState.velocity = config.initial_state.velocity + velocityNoise;
    return CreateAgent(config, initialState, prefabName);
  }

  public void LoadNewConfig(string configFileName) {
    this.SimulationConfig = ConfigLoader.LoadSimulationConfig(configFileName);
    // Reload the simulator config
    this.simulatorConfig = ConfigLoader.LoadSimulatorConfig();
    if (SimulationConfig != null) {
      Debug.Log($"Loaded new configuration: {configFileName}.");
      RestartSimulation();
    } else {
      Debug.LogError($"Failed to load configuration: {configFileName}.");
    }
  }

  public void RestartSimulation() {
    OnSimulationEnded?.Invoke();
    Debug.Log("Simulation ended.");
    UIManager.Instance.LogActionMessage("[SIM] Simulation restarted.");
    // Reset the simulation time.
    _elapsedSimulationTime = 0f;
    _isSimulationPaused = IsSimulationPaused();
    _costLaunchedInterceptors = 0f;
    _costDestroyedThreats = 0f;

    // Clear existing interceptors and threats.
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
    _submunitionsSwarms.Clear();
    _threatSwarms.Clear();
    OnInterceptorSwarmChanged?.Invoke(_interceptorSwarms);
    OnSubmunitionsSwarmChanged?.Invoke(_submunitionsSwarms);
    OnThreatSwarmChanged?.Invoke(_threatSwarms);
    StartSimulation();
  }

  void FixedUpdate() {
    if (!_isSimulationPaused && _elapsedSimulationTime < SimulationConfig.endTime) {
      _elapsedSimulationTime += Time.deltaTime;
    } else if (_elapsedSimulationTime >= SimulationConfig.endTime) {
      RestartSimulation();
      Debug.Log("Simulation completed.");
    }
  }

  public void QuitSimulation() {
    Application.Quit();
  }
}

[System.Serializable]
public class SimulatorConfig {
  public bool enableTelemetryLogging;
  public bool enableEventLogging;
  public bool enableMissileTrailEffect;
  public bool enableExplosionEffect;
  public int physicsUpdateRate;
  public bool persistentFlightTrails;
}
