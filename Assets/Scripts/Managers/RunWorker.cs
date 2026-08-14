using System;
using System.Collections;
using System.IO;
using UnityEngine;

// The run worker executes exactly one seeded simulation run launched externally, e.g., through the
// Python batch run launcher. It applies the seed, starts the requested simulation configuration,
// writes logs to the assigned output directory, and quits after the simulation finishes.

public class RunWorker : MonoBehaviour {
  private const string _simulationConfigFlag = "--simulation_config";
  private const string _seedFlag = "--seed";
  private const string _outputDirFlag = "--output_dir";
  private const string _communicationConfigOverrideFlag = "--communication_config_override";

  public static RunWorker Instance { get; private set; }

  // True if the run worker was launched from the CLI, e.g., as part of a batch run, rather than
  // from normal interactive mode.
  public static bool IsWorkerMode { get; private set; } = false;

  // Simulation configuration to execute.
  public static string SimulationConfigFile { get; private set; }

  // Simulation run seed.
  public static int Seed { get; private set; } = 0;

  // Output directory of the simulation run.
  public static string OutputDirectory { get; private set; }

  // Optional serialized communication configuration that replaces the simulation configuration.
  public static string CommunicationConfigOverridePath { get; private set; }

  private bool _hasStartedRun = false;
  private bool _hasScheduledQuit = false;

  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
  private static void OnBeforeSceneLoad() {
    if (!TryGetWorkerModeArguments(
            Environment.GetCommandLineArgs(), out string simulationConfigFile, out int seed,
            out string outputDirectory, out string communicationConfigOverridePath)) {
      return;
    }

    var gameObject = new GameObject("RunWorker");
    DontDestroyOnLoad(gameObject);
    var runWorker = gameObject.AddComponent<RunWorker>();
    runWorker.Initialize(simulationConfigFile, seed, outputDirectory,
                         communicationConfigOverridePath);
  }

  public static bool TryGetWorkerModeArguments(string[] args, out string simulationConfigFile,
                                               out int seed, out string outputDirectory,
                                               out string communicationConfigOverridePath) {
    simulationConfigFile = GetArgValue(args, _simulationConfigFlag);
    string seedValue = GetArgValue(args, _seedFlag);
    string rawOutputDirectory = GetArgValue(args, _outputDirFlag);
    string rawCommunicationConfigOverride = GetArgValue(args, _communicationConfigOverrideFlag);
    seed = 0;
    outputDirectory = null;
    communicationConfigOverridePath = null;

    if (simulationConfigFile == null && seedValue == null && rawOutputDirectory == null &&
        rawCommunicationConfigOverride == null) {
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
    if (rawCommunicationConfigOverride != null) {
      if (string.IsNullOrWhiteSpace(rawCommunicationConfigOverride) ||
          !Path.IsPathRooted(rawCommunicationConfigOverride)) {
        throw new ArgumentException(
            "Worker communication configuration override path must be absolute.");
      }
      communicationConfigOverridePath = Path.GetFullPath(rawCommunicationConfigOverride);
    }
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

  private void Initialize(string simulationConfigFile, int seed, string outputDirectory,
                          string communicationConfigOverridePath) {
    IsWorkerMode = true;
    SimulationConfigFile = simulationConfigFile;
    Seed = seed;
    OutputDirectory = outputDirectory;
    CommunicationConfigOverridePath = communicationConfigOverridePath;
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
    SimManager.Instance.LoadNewSimulationConfig(
        SimulationConfigFile, LoadCommunicationConfigOverride(CommunicationConfigOverridePath));
  }

  // Deserializes the exact scenario configuration written by the Python batch runner. Returning
  // null preserves the communication configuration embedded in the simulation file.
  private static Configs.CommunicationConfig LoadCommunicationConfigOverride(string path) {
    if (path == null) {
      return null;
    }

    byte[] serializedConfig = File.ReadAllBytes(path);
    return Configs.CommunicationConfig.Parser.ParseFrom(serializedConfig);
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
