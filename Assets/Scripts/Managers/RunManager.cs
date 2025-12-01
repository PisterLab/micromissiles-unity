using System;
using System.Collections;
using UnityEngine;

// The run manager handles running the same simulation configuration multiple times.
// It implements the singleton pattern to ensure that only one instance exists.
public class RunManager : MonoBehaviour {
  private const string _configFlag = "--config";

  public static RunManager Instance { get; private set; }

  // Run configuration.
  public Configs.RunConfig RunConfig { get; set; }

  // If true, the simulation is currently running.
  public bool IsRunning { get; private set; } = false;

  public int RunIndex { get; private set; } = 0;

  public int Seed { get; private set; } = 0;

  public string Timestamp { get; private set; } = "";

  public bool HasRunConfig() {
    return RunConfig != null;
  }

  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
  private static void OnBeforeSceneLoad() {
    string runConfigFile = TryGetCommandLineArg(_configFlag);
    if (runConfigFile == null) {
      return;
    }
    Configs.RunConfig runConfig = ConfigLoader.LoadRunConfig(runConfigFile);

    // Create a game object to run coroutines.
    var gameObject = new GameObject("RunManager");
    DontDestroyOnLoad(gameObject);
    var runManager = gameObject.AddComponent<RunManager>();
    runManager.InitializeFromRunConfig(runConfig);
  }

  private void InitializeFromRunConfig(Configs.RunConfig runConfig) {
    RunConfig = runConfig;
    IsRunning = false;
    RunIndex = 0;
    Seed = RunConfig.Seed;
    Timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
  }

  private void Start() {
    if (RunConfig != null) {
      Application.targetFrameRate = -1;

      // Disable automatically restarting at the end of the simulation.
      SimManager.Instance.AutoRestartOnEnd = false;
      SimManager.Instance.OnSimulationStarted += RegisterSimulationStarted;
      SimManager.Instance.OnSimulationEnded += RegisterSimulationEnded;

      StartCoroutine(Run());
    }
  }

  private void Awake() {
    if (Instance != null && Instance != this) {
      Destroy(gameObject);
      return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject);
  }

  private IEnumerator Run() {
    if (RunConfig.NumRuns == 0) {
      yield break;
    }

    // Allow one frame for initialization to finish.
    yield return null;

    // Seed the random number generator.
    UnityEngine.Random.InitState(Seed);

    Debug.Log(
        $"Starting run {RunIndex + 1} with seed {Seed} and simulation config {RunConfig.SimulationConfigFile}.");
    SimManager.Instance.LoadNewSimulationConfig(RunConfig.SimulationConfigFile);
  }

  private IEnumerator Advance() {
    // Allow one frame for cleanup to finish, such as simulation monitoring.
    yield return null;

    ++RunIndex;
    if (RunIndex >= RunConfig.NumRuns) {
      Debug.Log($"Completed run {RunConfig.Name} with {RunConfig.NumRuns} runs.");
      SimManager.Instance.QuitSimulation();
      yield break;
    }
    Seed += RunConfig.SeedStride;

    yield return Run();
  }

  private void RegisterSimulationStarted() {
    IsRunning = true;
  }

  private void RegisterSimulationEnded() {
    if (!IsRunning) {
      return;
    }
    IsRunning = false;
    StartCoroutine(Advance());
  }

  private static string TryGetCommandLineArg(string name) {
    try {
      var args = Environment.GetCommandLineArgs();
      return GetArgValue(args, name);
    } catch (Exception e) {
      Debug.LogWarning($"Failed to parse command line args: {e.Message}");
      return null;
    }
  }

  private static string GetArgValue(string[] args, string name) {
    for (int i = 0; i < args.Length; ++i) {
      if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase)) {
        if (i + 1 < args.Length)
          return args[i + 1];
      }
      if (args[i].StartsWith(name + "=", StringComparison.OrdinalIgnoreCase)) {
        return args[i].Substring(name.Length + 1);
      }
    }
    return null;
  }
}
