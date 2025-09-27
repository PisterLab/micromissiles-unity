using System;
using System.Collections.Generic;

public static class RunContext {
  private static readonly Dictionary<string, string> _labels = new Dictionary<string, string>();
  private static System.Random _systemRandom;

  public static bool IsActive { get; private set; }
  public static string BatchId { get; private set; }
  public static string RunId { get; private set; }
  public static int RunIndex { get; private set; }
  public static int Seed { get; private set; }
  public static string ConfigName { get; private set; }
  public static string OverridesFingerprint { get; private set; }
  public static IReadOnlyDictionary<string, string> Labels => _labels;
  public static DateTime StartedUtc { get; private set; }
  public static DateTime? EndedUtc { get; private set; }
  public static string Version { get; private set; }
  public static string Platform { get; private set; }
  public static System.Random SystemRandom => _systemRandom;
  private static bool _suppressNextEndEvent;

  public static void SetForRun(string batchId, string runId, int runIndex, int seed,
                               string configName, string overridesFingerprint,
                               IDictionary<string, string> labels, string version,
                               string platform) {
    BatchId = batchId;
    RunId = runId;
    RunIndex = runIndex;
    Seed = seed;
    ConfigName = configName;
    OverridesFingerprint = overridesFingerprint;

    _labels.Clear();
    if (labels != null) {
      foreach (var kvp in labels) {
        _labels[kvp.Key] = kvp.Value;
      }
    }

    Version = version;
    Platform = platform;
    StartedUtc = DateTime.UtcNow;
    EndedUtc = null;
    IsActive = true;

    _systemRandom = new System.Random(seed);
  }

  public static void MarkRunEnded() {
    if (!IsActive || EndedUtc.HasValue) {
      return;
    }

    EndedUtc = DateTime.UtcNow;
  }

  public static string GetLabel(string key) {
    return _labels.TryGetValue(key, out var value) ? value : null;
  }

  public static void Clear() {
    IsActive = false;
    BatchId = null;
    RunId = null;
    RunIndex = -1;
    Seed = 0;
    ConfigName = null;
    OverridesFingerprint = null;
    Version = null;
    Platform = null;
    StartedUtc = default;
    EndedUtc = null;
    _labels.Clear();
    _systemRandom = null;
    _suppressNextEndEvent = false;
  }

  public static void SuppressNextEndEventOnce() {
    _suppressNextEndEvent = true;
  }

  public static bool ConsumeEndEventSuppressionIfNeeded() {
    if (!_suppressNextEndEvent) {
      return false;
    }
    _suppressNextEndEvent = false;
    return true;
  }
}
