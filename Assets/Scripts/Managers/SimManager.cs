using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// The simulation manager handles the creation of all agents.
// It implements the singleton pattern to ensure that only one instance exists.
public class SimManager : MonoBehaviour {
  // Simulation events.
  public delegate void SimulationEventHandler();
  public event SimulationEventHandler OnSimulationStarted;
  public event SimulationEventHandler OnSimulationEnded;

  // Interceptor events.
  public delegate void NewInterceptorEventHandler(IInterceptor interceptor);
  public event NewInterceptorEventHandler OnNewInterceptor;
  public delegate void NewLauncherEventHandler(IInterceptor launcher);
  public event NewLauncherEventHandler OnNewLauncher;

  // Threat events.
  public delegate void NewThreatEventHandler(IThreat threat);
  public event NewThreatEventHandler OnNewThreat;

  // Default simulation configuration.
  private const string _defaultSimulationConfig = "7_quadcopters.pbtxt";

  // Default simulator configuration.
  private const string _defaultSimulatorConfig = "simulator.pbtxt";

  // Map from the agent type to the prefab class name.
  // The prefab class must exist in the Resources/Prefabs directory.
  private static readonly Dictionary<Configs.AgentType, string> _agentTypePrefabMap = new() {
    { Configs.AgentType.Vessel, "Vessel" },
    { Configs.AgentType.ShoreBattery, "ShoreBattery" },
    { Configs.AgentType.CarrierInterceptor, "CarrierInterceptor" },
    { Configs.AgentType.MissileInterceptor, "MissileInterceptor" },
    { Configs.AgentType.FixedWingThreat, "FixedWingThreat" },
    { Configs.AgentType.RotaryWingThreat, "RotaryWingThreat" },
  };

  public static SimManager Instance { get; private set; }

  // Simulation configuration.
  public Configs.SimulationConfig SimulationConfig { get; set; }

  // Simulator configuration.
  public Configs.SimulatorConfig SimulatorConfig { get; set; }

  // Simulation time.
  public float ElapsedTime { get; private set; } = 0f;
  public bool IsPaused { get; private set; } = false;

  // If true, the simulation is currently running.
  public bool IsRunning { get; private set; } = false;

  // If true, automatically restart the simulation.
  public bool AutoRestartOnEnd { get; set; } = true;

  public string Timestamp { get; private set; } = "";

  // Lists of all agents in the simulation.
  private List<IAgent> _interceptors = new List<IAgent>();
  private List<IAgent> _threats = new List<IAgent>();
  private List<IAgent> _dummyAgents = new List<IAgent>();

  public IReadOnlyList<IAgent> Interceptors => _interceptors.AsReadOnly();
  public IReadOnlyList<IAgent> Threats => _threats.AsReadOnly();
  public IReadOnlyList<IAgent> Agents => Interceptors.Concat(Threats).ToList().AsReadOnly();

  // Interceptor and threat costs.
  public float CostLaunchedInterceptors { get; private set; } = 0f;
  public float CostDestroyedThreats { get; private set; } = 0f;

  // Track the number of interceptors and threats spawned and terminated.
  private int _numInterceptorsSpawned = 0;
  private int _numThreatsSpawned = 0;
  private int _numThreatsTerminated = 0;

  public void StartSimulation() {
    IsRunning = true;
    OnSimulationStarted?.Invoke();
    Debug.Log("Simulation started.");
    UIManager.Instance.LogActionMessage("[SIM] Simulation started.");

    InitializeLaunchers();
    InitializeThreats();
  }

  public void EndSimulation() {
    IsRunning = false;
    OnSimulationEnded?.Invoke();
    Debug.Log("Simulation ended.");
    UIManager.Instance.LogActionMessage("[SIM] Simulation ended.");

    // Clear existing interceptors and threats.
    foreach (var interceptor in _interceptors) {
      if (interceptor as MonoBehaviour != null) {
        Destroy(interceptor.gameObject);
      }
    }
    foreach (var threat in _threats) {
      if (threat as MonoBehaviour != null) {
        Destroy(threat.gameObject);
      }
    }
    foreach (var dummyAgent in _dummyAgents) {
      if (dummyAgent as MonoBehaviour != null) {
        Destroy(dummyAgent.gameObject);
      }
    }

    _interceptors.Clear();
    _threats.Clear();
    _dummyAgents.Clear();
  }

  public void PostSimulation() {
    if (AutoRestartOnEnd) {
      RestartSimulation();
    }
  }

  public void PauseSimulation() {
    IsPaused = true;
    SetGameSpeed();
  }

  public void ResumeSimulation() {
    IsPaused = false;
    SetGameSpeed();
  }

  public void QuitSimulation() {
    Application.Quit();
  }

  public void RestartSimulation() {
    ElapsedTime = 0f;
    CostLaunchedInterceptors = 0f;
    CostDestroyedThreats = 0f;

    StartSimulation();
  }

  public void LoadNewSimulationConfig(string configFile) {
    // Reload the simulator configuration.
    SimulatorConfig = ConfigLoader.LoadSimulatorConfig(_defaultSimulatorConfig);
    SimulationConfig = ConfigLoader.LoadSimulationConfig(configFile);
    SetGameSpeed();

    if (SimulationConfig != null) {
      Debug.Log($"Loaded new simulation configuration: {configFile}.");
      RestartSimulation();
    } else {
      Debug.LogError($"Failed to load simulation configuration: {configFile}.");
    }
  }

  // Create an interceptor based on the provided configuration.
  public IInterceptor CreateInterceptor(Configs.AgentConfig config, Simulation.State initialState) {
    if (config == null) {
      return null;
    }

    // Load the static configuration.
    Configs.StaticConfig staticConfig = ConfigLoader.LoadStaticConfig(config.ConfigFile);
    if (staticConfig == null) {
      return null;
    }

    GameObject interceptorObject = null;
    if (_agentTypePrefabMap.TryGetValue(staticConfig.AgentType, out var prefab)) {
      interceptorObject = CreateAgent(config, initialState, prefab);
    }
    if (interceptorObject == null) {
      return null;
    }

    IInterceptor interceptor = interceptorObject.GetComponent<IInterceptor>();
    interceptor.HierarchicalAgent = new HierarchicalAgent(interceptor);
    interceptor.StaticConfig = staticConfig;
    interceptor.OnTerminated += (interceptor) => _interceptors.Remove(interceptor);
    _interceptors.Add(interceptor);
    ++_numInterceptorsSpawned;

    // Assign a unique and simple ID.
    int interceptorId = _numInterceptorsSpawned;
    interceptorObject.name = $"{staticConfig.Name}_Interceptor_{interceptorId}";

    // Add the interceptor's unit cost to the total cost.
    CostLaunchedInterceptors += staticConfig.Cost;

    OnNewInterceptor?.Invoke(interceptor);
    return interceptor;
  }

  // Create a threat based on the provided configuration.
  // Returns the created threat instance, or null if creation failed.
  public IThreat CreateThreat(Configs.AgentConfig config) {
    if (config == null) {
      return null;
    }

    // Load the static configuration.
    Configs.StaticConfig staticConfig = ConfigLoader.LoadStaticConfig(config.ConfigFile);
    if (staticConfig == null) {
      return null;
    }

    GameObject threatObject = null;
    if (_agentTypePrefabMap.TryGetValue(staticConfig.AgentType, out var prefab)) {
      threatObject = CreateRandomAgent(config, prefab);
    }
    if (threatObject == null) {
      return null;
    }

    IThreat threat = threatObject.GetComponent<IThreat>();
    threat.HierarchicalAgent = new HierarchicalAgent(threat);
    threat.StaticConfig = staticConfig;
    threat.OnMiss += RegisterThreatMiss;
    threat.OnTerminated += RegisterThreatTerminated;
    _threats.Add(threat);
    ++_numThreatsSpawned;

    // Assign a unique and simple ID.
    int threatId = _threats.Count;
    threatObject.name = $"{staticConfig.Name}_Threat_{threatId}";

    OnNewThreat?.Invoke(threat);
    return threat;
  }

  public IAgent CreateDummyAgent(in Vector3 position, in Vector3 velocity) {
    GameObject dummyAgentPrefab = Resources.Load<GameObject>($"Prefabs/DummyAgent");
    GameObject dummyAgentObject = Instantiate(dummyAgentPrefab, position, Quaternion.identity);
    var dummyAgent = dummyAgentObject.GetComponent<IAgent>();
    dummyAgent.Velocity = velocity;
    _dummyAgents.Add(dummyAgent);
    dummyAgent.OnTerminated += (agent) => _dummyAgents.Remove(agent);
    return dummyAgent;
  }

  public void DestroyDummyAgent(IAgent dummyAgent) {
    Destroy(dummyAgent.gameObject);
    _dummyAgents.Remove(dummyAgent);
  }

  // Create an agent based on the provided configuration and prefab name.
  private GameObject CreateAgent(Configs.AgentConfig config, Simulation.State initialState,
                                 string prefabName) {
    GameObject prefab = Resources.Load<GameObject>($"Prefabs/{prefabName}");
    if (prefab == null) {
      Debug.LogError($"Prefab {prefabName} not found in Resources/Prefabs directory.");
      return null;
    }

    GameObject agentObject =
        Instantiate(prefab, Coordinates3.FromProto(initialState.Position), Quaternion.identity);
    IAgent agent = agentObject.GetComponent<IAgent>();
    agent.AgentConfig = config;
    Vector3 velocity = Coordinates3.FromProto(initialState.Velocity);
    agent.Velocity = velocity;

    // Set the rotation to face the initial velocity.
    Quaternion targetRotation = Quaternion.LookRotation(velocity, Vector3.up);
    agentObject.transform.rotation = targetRotation;
    return agentObject;
  }

  // Create a random agent based on the provided configuration and prefab name.
  private GameObject CreateRandomAgent(Configs.AgentConfig config, string prefabName) {
    // Randomize the position and the velocity.
    Vector3 positionNoise = Utilities.GenerateRandomNoise(config.StandardDeviation.Position);
    Vector3 velocityNoise = Utilities.GenerateRandomNoise(config.StandardDeviation.Velocity);
    var initialState = new Simulation.State() {
      Position = Coordinates3.ToProto(Coordinates3.FromProto(config.InitialState.Position) +
                                      positionNoise),
      Velocity = Coordinates3.ToProto(Coordinates3.FromProto(config.InitialState.Velocity) +
                                      velocityNoise),
    };
    return CreateAgent(config, initialState, prefabName);
  }

  private void Awake() {
    if (Instance != null && Instance != this) {
      Destroy(gameObject);
      return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject);

    SimulatorConfig = ConfigLoader.LoadSimulatorConfig(_defaultSimulatorConfig);
    SimulationConfig = ConfigLoader.LoadSimulationConfig(_defaultSimulationConfig);
    Timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
  }

  private void Start() {
    IsPaused = false;
    if (!RunManager.Instance.HasRunConfig()) {
      StartSimulation();
      ResumeSimulation();
    }
  }

  private void FixedUpdate() {
    if (IsRunning) {
      if (!IsPaused && ElapsedTime < SimulationConfig.EndTime) {
        ElapsedTime += Time.deltaTime;
      } else if (ElapsedTime >= SimulationConfig.EndTime) {
        EndSimulation();
        PostSimulation();
      }
    }
  }

  private void InitializeLaunchers() {
    foreach (var swarmConfig in SimulationConfig.InterceptorSwarmConfigs) {
      IInterceptor launcher =
          CreateInterceptor(swarmConfig.AgentConfig, swarmConfig.AgentConfig.InitialState);
      OnNewLauncher?.Invoke(launcher);
    }
  }

  private void InitializeThreats() {
    foreach (var swarmConfig in SimulationConfig.ThreatSwarmConfigs) {
      for (int i = 0; i < swarmConfig.NumAgents; ++i) {
        CreateThreat(swarmConfig.AgentConfig);
      }
    }
  }

  private void SetGameSpeed() {
    if (IsPaused) {
      Time.fixedDeltaTime = 0;
      SetTimeScale(timeScale: 0);
    } else {
      Time.fixedDeltaTime = 1.0f / SimulatorConfig.PhysicsUpdateRate;
      SetTimeScale(SimulationConfig.TimeScale);
    }
  }

  private void SetTimeScale(float timeScale) {
    Time.timeScale = timeScale;
    // Time.fixedDeltaTime is derived from the simulator configuration.
    Time.maximumDeltaTime = Time.fixedDeltaTime * 3;
  }

  private void RegisterThreatMiss(IThreat threat) {
    CostDestroyedThreats += threat.StaticConfig.Cost;
  }

  private void RegisterThreatTerminated(IAgent threat) {
    _threats.Remove(threat);
    ++_numThreatsTerminated;

    // The simulation can be ended before the actual end time if there is a run configuration and
    // all spawned threats have been terminated.
    if (RunManager.Instance.HasRunConfig() && _numThreatsSpawned > 0 &&
        _numThreatsTerminated >= _numThreatsTerminated) {
      EndSimulation();
      PostSimulation();
    }
  }
}
