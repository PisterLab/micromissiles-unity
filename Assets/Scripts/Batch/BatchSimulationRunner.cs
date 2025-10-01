using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using UnityEngine;

// Orchestrates sequential batch runs driven by CLI args or a batch JSON file.
public class BatchSimulationRunner : MonoBehaviour {
  private readonly List<RunSpec> _runs = new List<RunSpec>();
  private int _currentIndex = -1;
  private bool _waitingForEnd = false;
  private string _batchId;
  public static bool IsBatchRequested { get; private set; }

  private static readonly JsonSerializer _jsonSerializer =
      new JsonSerializer { Converters = { new StringEnumConverter() } };

  // Represents one run entry in a batch.
  private class RunSpec {
    public string ConfigPath;
    public string FriendlyName;
    public int Seed;
    public int RunIndex;
    public IDictionary<string, string> Labels = new Dictionary<string, string>();
    // Overrides via SelectToken path => value JToken
    public IDictionary<string, JToken> Overrides = new Dictionary<string, JToken>();
  }

  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
  private static void Preflight() {
    try {
      var args = Environment.GetCommandLineArgs();
      IsBatchRequested = IsBatchModeRequested(args);
    } catch (Exception ex) {
      Debug.LogWarning($"[BatchRunner] Preflight failed: {ex.Message}");
      IsBatchRequested = false;
    }
  }

  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  private static void OnAfterSceneLoad() {
    try {
      var args = Environment.GetCommandLineArgs();
      if (!IsBatchModeRequested(args)) {
        return;  // Normal interactive run.
      }

      // Create a host object to run coroutines and own subscriptions.
      var host = new GameObject("BatchSimulationRunner");
      DontDestroyOnLoad(host);
      var runner = host.AddComponent<BatchSimulationRunner>();
      runner.InitializeFromArgs(args);
    } catch (Exception ex) {
      Debug.LogError($"[BatchRunner] Failed to bootstrap: {ex}");
    }
  }

  private static bool IsBatchModeRequested(string[] args) {
    // Trigger batch mode if any of these flags are present.
    return HasArg(args, "--batchConfig") || HasArg(args, "--config") || HasArg(args, "--runs");
  }

  private static bool HasArg(string[] args, string name) {
    for (int i = 0; i < args.Length; i++) {
      if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
        return true;
      if (args[i].StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
        return true;
    }
    return false;
  }

  private static string GetArgValue(string[] args, string name) {
    for (int i = 0; i < args.Length; i++) {
      var a = args[i];
      if (a.Equals(name, StringComparison.OrdinalIgnoreCase)) {
        if (i + 1 < args.Length)
          return args[i + 1];
      }
      if (a.StartsWith(name + "=", StringComparison.OrdinalIgnoreCase)) {
        return a.Substring(name.Length + 1);
      }
    }
    return null;
  }

  private void InitializeFromArgs(string[] args) {
    try {
      Application.targetFrameRate = -1;  // Avoid throttling in batch mode

      // Prevent SimManager from auto-restarting; we will sequence runs.
      if (SimManager.Instance != null) {
        SimManager.Instance.autoRestartOnEnd = false;
      }

      // Labels from CLI (flat JSON object string)
      var labels = ParseLabelsJson(GetArgValue(args, "--labels"));

      // Determine batchId
      _batchId = GetArgValue(args, "--batchId");

      // Primary input mode: batch config file
      string batchConfigPath = GetArgValue(args, "--batchConfig");
      // Convenience: if user passed a lone .json path without a flag, treat as --batchConfig
      if (string.IsNullOrEmpty(batchConfigPath)) {
        var implicitJsonArg =
            args.FirstOrDefault(a => !string.IsNullOrEmpty(a) && !a.StartsWith("-") &&
                                     a.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(implicitJsonArg)) {
          batchConfigPath = implicitJsonArg;
        }
      }
      if (!string.IsNullOrEmpty(batchConfigPath)) {
        BuildRunsFromBatchFile(batchConfigPath, labels);
      } else {
        // Secondary mode: single config + count + seed pattern
        string configArg = GetArgValue(args, "--config");
        if (string.IsNullOrEmpty(configArg)) {
          Debug.LogError("[BatchRunner] --config must be provided when --batchConfig is not set.");
          return;
        }
        int runs = ParseIntOrDefault(GetArgValue(args, "--runs"), 1);
        int baseSeed = ParseIntOrDefault(GetArgValue(args, "--seed"), 1);
        int seedStride = ParseIntOrDefault(GetArgValue(args, "--seedStride"), 1);
        var overrides = ParseOverridesJson(GetArgValue(args, "--overrides"));
        var batchIdDefault = !string.IsNullOrEmpty(_batchId) ? _batchId : BuildDefaultBatchId();
        _batchId = batchIdDefault;

        for (int i = 0; i < runs; i++) {
          var spec = CreateRunSpec(configArg, i, baseSeed + i * seedStride, labels, overrides,
                                   Directory.GetCurrentDirectory());
          if (spec != null) {
            _runs.Add(spec);
          }
        }
      }

      if (string.IsNullOrEmpty(_batchId)) {
        _batchId = BuildDefaultBatchId();
      }

      // Subscriptions to observe run boundaries
      if (SimManager.Instance != null) {
        SimManager.Instance.OnSimulationEnded += OnSimulationEnded;
        SimManager.Instance.OnSimulationStarted += OnSimulationStarted;
      }

      // Kick off sequencing
      StartCoroutine(RunSequence());
    } catch (Exception ex) {
      Debug.LogError($"[BatchRunner] Initialization error: {ex}");
    }
  }

  private static IDictionary<string, string> ParseLabelsJson(string json) {
    var result = new Dictionary<string, string>();
    if (string.IsNullOrWhiteSpace(json))
      return result;
    try {
      var jobj = JObject.Parse(json);
      foreach (var prop in jobj.Properties()) {
        result[prop.Name] = prop.Value.Type == JTokenType.String
                                ? (string)prop.Value
                                : prop.Value.ToString(Formatting.None);
      }
    } catch (Exception ex) {
      Debug.LogWarning($"[BatchRunner] Failed to parse --labels JSON: {ex.Message}");
    }
    return result;
  }

  private static IDictionary<string, JToken> ParseOverridesJson(string json) {
    var result = new Dictionary<string, JToken>();
    if (string.IsNullOrWhiteSpace(json))
      return result;
    try {
      var jobj = JObject.Parse(json);
      foreach (var prop in jobj.Properties()) {
        result[prop.Name] = prop.Value;
      }
    } catch (Exception ex) {
      Debug.LogWarning($"[BatchRunner] Failed to parse --overrides JSON: {ex.Message}");
    }
    return result;
  }

  private static int ParseIntOrDefault(string s, int dflt) {
    if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
      return v;
    return dflt;
  }

  private void BuildRunsFromBatchFile(string path, IDictionary<string, string> parentLabels) {
    try {
      string resolvedBatchPath = ResolveConfigPath(path, Directory.GetCurrentDirectory());
      if (string.IsNullOrEmpty(resolvedBatchPath)) {
        Debug.LogError($"[BatchRunner] Batch file not found: {path}");
        return;
      }

      string json = File.ReadAllText(resolvedBatchPath);

      var root = JObject.Parse(json);
      _batchId = _batchId ?? (string)root["batchId"];

      string batchDirectory = Path.GetDirectoryName(resolvedBatchPath);

      // Global fields
      string globalConfig = (string)root["config"];
      int baseSeed = root["seed"] != null ? (int)root["seed"] : 1;
      int seedStride = root["seedStride"] != null ? (int)root["seedStride"] : 1;
      var globalLabels = ToStringDict((JObject)root["labels"]) ?? new Dictionary<string, string>();
      foreach (var kv in parentLabels) globalLabels[kv.Key] = kv.Value;
      var globalOverrides =
          ToTokenDict((JObject)root["overrides"]) ?? new Dictionary<string, JToken>();

      // Either "runs": <int> replicate, or array of run specs
      if (root["runs"] != null && root["runs"].Type == JTokenType.Integer &&
          !string.IsNullOrEmpty(globalConfig)) {
        int runs = (int)root["runs"];
        for (int i = 0; i < runs; i++) {
          var spec = CreateRunSpec(globalConfig, i, baseSeed + i * seedStride, globalLabels,
                                   globalOverrides, batchDirectory);
          if (spec != null) {
            _runs.Add(spec);
          }
        }
        return;
      }

      // Array of detailed run entries
      var runArray = (JArray)root["runs"];
      if (runArray != null) {
        int idx = 0;
        foreach (var t in runArray) {
          var o = (JObject)t;
          string configForRun = (string)(o["config"] ?? globalConfig);
          if (string.IsNullOrEmpty(configForRun)) {
            Debug.LogError($"[BatchRunner] Run {idx} missing 'config' path.");
            idx++;
            continue;
          }

          var mergedLabels = new Dictionary<string, string>(globalLabels);
          var localLabels = ToStringDict((JObject)o["labels"]);
          if (localLabels != null)
            foreach (var kv in localLabels) mergedLabels[kv.Key] = kv.Value;

          var mergedOverrides = new Dictionary<string, JToken>(globalOverrides);
          var localOverrides = ToTokenDict((JObject)o["overrides"]);
          if (localOverrides != null)
            foreach (var kv in localOverrides) mergedOverrides[kv.Key] = kv.Value;

          var spec = CreateRunSpec(configForRun,
                                   idx,
                                   (int?)(o["seed"]) ?? (baseSeed + idx * seedStride),
                                   mergedLabels,
                                   mergedOverrides,
                                   batchDirectory);
          if (spec != null) {
            _runs.Add(spec);
          }
          idx++;
        }
      }

      if (_runs.Count == 0) {
        Debug.LogError("[BatchRunner] Batch file parsed but defined 0 runs.");
      }
    } catch (Exception ex) {
      Debug.LogError($"[BatchRunner] Failed to parse batch file '{path}': {ex}");
    }
  }

  private static Dictionary<string, string> ToStringDict(JObject obj) {
    if (obj == null)
      return null;
    var d = new Dictionary<string, string>();
    foreach (var p in obj.Properties()) {
      d[p.Name] =
          p.Value.Type == JTokenType.String ? (string)p.Value : p.Value.ToString(Formatting.None);
    }
    return d;
  }

  private static Dictionary<string, JToken> ToTokenDict(JObject obj) {
    if (obj == null)
      return null;
    var d = new Dictionary<string, JToken>();
    foreach (var p in obj.Properties()) {
      d[p.Name] = p.Value;
    }
    return d;
  }

  private IEnumerator RunSequence() {
    if (_runs.Count == 0)
      yield break;
    // Give time for scene objects to finish initializing in case we're very early.
    yield return null;

    _currentIndex = -1;
    AdvanceToNextRun();
  }

  private void AdvanceToNextRun() {
    _currentIndex++;
    if (_currentIndex >= _runs.Count) {
      Debug.Log($"[BatchRunner] Completed batch '{_batchId}' with {_runs.Count} runs.");
      Application.Quit();
      return;
    }

    var spec = _runs[_currentIndex];
    StartOneRun(spec);
  }

  private void StartOneRun(RunSpec spec) {
    try {
      if (string.IsNullOrEmpty(spec.ConfigPath) || !File.Exists(spec.ConfigPath)) {
        Debug.LogError($"[BatchRunner] Config file missing: {spec.ConfigPath}");
        AdvanceToNextRun();
        return;
      }

      string configJson = File.ReadAllText(spec.ConfigPath);
      string friendlyName = spec.FriendlyName ?? Path.GetFileName(spec.ConfigPath);
      Debug.Log($"[BatchRunner] Starting run {spec.RunIndex} seed={spec.Seed} config={friendlyName}");

      var patched = ApplyOverrides(configJson, spec.Overrides, out var fingerprint);
      var cfg = DeserializeSimulationConfig(patched);
      if (cfg == null) {
        Debug.LogError("[BatchRunner] Config deserialization returned null; skipping run.");
        AdvanceToNextRun();
        return;
      }

      // Seed Unity and shared System.Random
      UnityEngine.Random.InitState(spec.Seed);

      // Prepare run IDs
      string runId = BuildRunId(spec.RunIndex, spec.Seed);
      var version = Application.version;
      var platform = Application.platform.ToString();

      RunContext.SetForRun(_batchId, runId, spec.RunIndex, spec.Seed, friendlyName, fingerprint,
                           spec.Labels, version, platform);

      // Wait for the new run's OnSimulationStarted before considering end events.
      _waitingForEnd = false;

      if (SimManager.Instance != null) {
        SimManager.Instance.autoRestartOnEnd = false;
        SimManager.Instance.LoadNewConfig(cfg, friendlyName);
      } else {
        Debug.LogError("[BatchRunner] SimManager.Instance is null");
      }

      Debug.Log(
          $"[BatchRunner] Started run {spec.RunIndex} seed={spec.Seed} config={friendlyName}");
    } catch (Exception ex) {
      Debug.LogError($"[BatchRunner] Failed to start run {_currentIndex}: {ex}");
      AdvanceToNextRun();
    }
  }

  private void OnSimulationStarted() {
    // Now the run is live; the next end event is the real one.
    _waitingForEnd = true;
  }

  private void OnSimulationEnded() {
    if (!_waitingForEnd)
      return;
    _waitingForEnd = false;
    // Allow one frame for CSV conversion and any late writes to finish.
    StartCoroutine(EndAndAdvanceNextFrame());
  }

  private IEnumerator EndAndAdvanceNextFrame() {
    try {
      // Mark end in context (SimMonitor will also finalize run_meta.json)
      RunContext.MarkRunEnded();
      // Use realtime wait so we do not depend on Time.timeScale.
      yield return new WaitForSecondsRealtime(0.01f);
    } finally {
      Debug.Log($"[BatchRunner] Run {_currentIndex} ended; advancing to next.");
      AdvanceToNextRun();
    }
  }

  private static string ApplyOverrides(string baseJson, IDictionary<string, JToken> overrides,
                                       out string fingerprint) {
    fingerprint = string.Empty;
    if (string.IsNullOrEmpty(baseJson) || overrides == null || overrides.Count == 0)
      return baseJson;

    var root = JObject.Parse(baseJson);
    int applied = 0;
    foreach (var kv in overrides.OrderBy(k => k.Key)) {
      try {
        var token = root.SelectToken(kv.Key, errorWhenNoMatch: false);
        if (token == null) {
          Debug.LogWarning($"[BatchRunner] Override path not found: {kv.Key}");
          continue;
        }
        token.Replace(kv.Value);
        applied++;
      } catch (Exception ex) {
        Debug.LogWarning($"[BatchRunner] Failed override '{kv.Key}': {ex.Message}");
      }
    }

    fingerprint = ComputeFingerprint(overrides);
    if (applied == 0)
      return baseJson;  // Nothing successfully applied
    return root.ToString(Formatting.None);
  }

  private static string ComputeFingerprint(IDictionary<string, JToken> overrides) {
    if (overrides == null || overrides.Count == 0)
      return string.Empty;
    var sb = new StringBuilder();
    foreach (var kv in overrides.OrderBy(k => k.Key)) {
      sb.Append(kv.Key).Append('=');
      sb.Append(kv.Value?.ToString(Formatting.None) ?? "null");
      sb.Append(';');
    }
    using var sha1 = SHA1.Create();
    var bytes = Encoding.UTF8.GetBytes(sb.ToString());
    var hash = sha1.ComputeHash(bytes);
    return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
  }

  private static SimulationConfig DeserializeSimulationConfig(string json) {
    try {
      using var reader = new JsonTextReader(new StringReader(json));
      return _jsonSerializer.Deserialize<SimulationConfig>(reader);
    } catch (Exception ex) {
      Debug.LogError($"[BatchRunner] JSON -> SimulationConfig failed: {ex.Message}");
      return null;
    }
  }

  private string BuildDefaultBatchId() {
    // yyyyMMdd_HHmmss + 6-char suffix
    string ts = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
    string suffix = Guid.NewGuid().ToString("N").Substring(0, 6);
    return $"batch_{ts}_{suffix}";
  }

  private string BuildRunId(int runIndex, int seed) {
    return $"run_{runIndex:D4}_seed_{seed}";
  }

  private RunSpec CreateRunSpec(string configInput, int runIndex, int seed,
                                IDictionary<string, string> labels,
                                IDictionary<string, JToken> overrides, string baseDirectory) {
    string resolved = ResolveConfigPath(configInput, baseDirectory);
    if (string.IsNullOrEmpty(resolved)) {
      Debug.LogError(
          $"[BatchRunner] Config path '{configInput}' could not be resolved (base '{baseDirectory}').");
      return null;
    }

    var spec =
        new RunSpec { ConfigPath = resolved,
                      FriendlyName = Path.GetFileName(resolved),
                      Seed = seed,
                      RunIndex = runIndex,
                      Labels = labels != null ? new Dictionary<string, string>(labels)
                                              : new Dictionary<string, string>(),
                      Overrides = overrides != null ? new Dictionary<string, JToken>(overrides)
                                                    : new Dictionary<string, JToken>() };
    return spec;
  }

  private static string ResolveConfigPath(string userPath, string baseDirectory) {
    if (string.IsNullOrWhiteSpace(userPath))
      return null;

    string candidate;
    if (Path.IsPathRooted(userPath)) {
      candidate = userPath;
    } else {
      string baseDir =
          !string.IsNullOrEmpty(baseDirectory) ? baseDirectory : Directory.GetCurrentDirectory();
      candidate = Path.GetFullPath(Path.Combine(baseDir, userPath));
    }

    if (File.Exists(candidate)) {
      return candidate;
    }

    // Fallback to StreamingAssets/Configs for callers that only pass config filenames.
    try {
      var streamingConfigs = Path.Combine(Application.streamingAssetsPath ?? string.Empty, "Configs");
      if (!string.IsNullOrEmpty(streamingConfigs)) {
        var streamingCandidate = Path.GetFullPath(Path.Combine(streamingConfigs, userPath));
        if (File.Exists(streamingCandidate)) {
          return streamingCandidate;
        }
      }
    } catch (Exception ex) {
      Debug.LogWarning($"[BatchRunner] Failed StreamingAssets fallback for '{userPath}': {ex.Message}");
    }

    return null;
  }
}
