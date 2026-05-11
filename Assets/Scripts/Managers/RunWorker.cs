using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class RunWorker : MonoBehaviour {
  private const string _simulationConfigFlag = "--simulation_config";
  private const string _seedFlag = "--seed";
  private const string _outputDirFlag = "--output_dir";

  public static RunWorker Instance { get; private set; }

  public static bool IsWorkerMode { get; private set; } = false;

  public static string SimulationConfigFile { get; private set; }

  public static int Seed { get; private set; } = 0;

  public static string OutputDirectory { get; private set; }

  private bool _hasStartedRun = false;
  private bool _hasScheduledQuit = false;

  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
  private static void OnBeforeSceneLoad() {
    if (!TryGetWorkerModeArguments(Environment.GetCommandLineArgs(),
                                   out string simulationConfigFile, out int seed,
                                   out string outputDirectory)) {
      return;
    }

    var gameObject = new GameObject("RunWorker");
    DontDestroyOnLoad(gameObject);
    var runWorker = gameObject.AddComponent<RunWorker>();
    runWorker.Initialize(simulationConfigFile, seed, outputDirectory);
  }

  public static bool TryGetWorkerModeArguments(string[] args, out string simulationConfigFile,
                                               out int seed, out string outputDirectory) {
    simulationConfigFile = GetArgValue(args, _simulationConfigFlag);
    string seedValue = GetArgValue(args, _seedFlag);
    string rawOutputDirectory = GetArgValue(args, _outputDirFlag);
    seed = 0;
    outputDirectory = null;

    if (simulationConfigFile == null && seedValue == null && rawOutputDirectory == null) {
      return false;
    }

    if (string.IsNullOrWhiteSpace(simulationConfigFile) || string.IsNullOrWhiteSpace(seedValue) ||
        string.IsNullOrWhiteSpace(rawOutputDirectory)) {
      throw new ArgumentException("Worker mode requires simulation config, seed, and output dir.");
    }
    if (!int.TryParse(seedValue, out seed)) {
      throw new ArgumentException($"Failed to parse worker seed: {seedValue}.");
    }
    if (!Path.IsPathRooted(rawOutputDirectory)) {
      throw new ArgumentException(
          $"Worker output directory must be absolute: {rawOutputDirectory}.");
    }

    outputDirectory = Path.GetFullPath(rawOutputDirectory);
    return true;
  }

  private void Awake() {
    if (Instance != null && Instance != this) {
      Destroy(gameObject);
      return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject);
  }

  private void Start() {
    if (!IsWorkerMode) {
      Destroy(gameObject);
      return;
    }

    Application.targetFrameRate = -1;
    StartCoroutine(RunWhenReady());
  }

  private void OnDestroy() {
    if (SimManager.Instance != null) {
      SimManager.Instance.OnSimulationEnded -= RegisterSimulationEnded;
    }
    if (Instance == this) {
      Instance = null;
      IsWorkerMode = false;
      SimulationConfigFile = null;
      Seed = 0;
      OutputDirectory = null;
    }
  }

  private void Initialize(string simulationConfigFile, int seed, string outputDirectory) {
    IsWorkerMode = true;
    SimulationConfigFile = simulationConfigFile;
    Seed = seed;
    OutputDirectory = outputDirectory;
  }

  private IEnumerator RunWhenReady() {
    while (SimManager.Instance == null) {
      yield return null;
    }

    SimManager.Instance.AutoRestartOnEnd = false;
    SimManager.Instance.OnSimulationEnded += RegisterSimulationEnded;

    // Allow scene initialization and manager subscriptions to finish first.
    yield return null;

    PrepareOutputDirectory();
    UnityEngine.Random.InitState(Seed);
    _hasStartedRun = true;
    Debug.Log($"Starting run with simulation config {SimulationConfigFile} and seed {Seed}.");
    SimManager.Instance.LoadNewSimulationConfig(SimulationConfigFile);
  }

  private void PrepareOutputDirectory() {
    if (Directory.Exists(OutputDirectory)) {
      throw new IOException(
          $"Output directory already exists: {OutputDirectory}. Refusing to overwrite.");
    }
    Directory.CreateDirectory(OutputDirectory);
  }

  private void RegisterSimulationEnded() {
    if (!_hasStartedRun || _hasScheduledQuit) {
      return;
    }

    _hasScheduledQuit = true;
    StartCoroutine(QuitAfterCleanup());
  }

  private IEnumerator QuitAfterCleanup() {
    // Allow end-of-run cleanup, such as log flushing, to complete first.
    yield return null;
    SimManager.Instance.QuitSimulation();
  }

  private static string GetArgValue(string[] args, string name) {
    for (int i = 0; i < args.Length; ++i) {
      if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) {
        return args[i + 1];
      }
      if (args[i].StartsWith(name + "=", StringComparison.OrdinalIgnoreCase)) {
        return args[i].Substring(name.Length + 1);
      }
    }
    return null;
  }
}
