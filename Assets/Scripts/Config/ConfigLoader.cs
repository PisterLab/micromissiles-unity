using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public static class ConfigLoader {
  // Maximum serialized length of a Protobuf message.
  // This value is estimated and can be increased if necessary.
  private const int MaxProtobufSerializedLength = 1024;

  // Relative path to the default simulator configuration.
  private const string SimulatorConfigRelativePath = "simulator.pbtxt";

  // Map from the interceptor type to the static configuration file.
  private static readonly Dictionary<Micromissiles.InterceptorType, string>
      InterceptorStaticConfigMap = new() {
        { Micromissiles.InterceptorType.Hydra70, "hydra70.pbtxt" },
        { Micromissiles.InterceptorType.Micromissile, "micromissiles.pbtxt" },
      };

  // Map from the threat type to the static configuration file.
  private static readonly Dictionary<Micromissiles.ThreatType, string> ThreatStaticConfigMap =
      new() {
        { Micromissiles.ThreatType.Quadcopter, "quadcopter.pbtxt" },
        { Micromissiles.ThreatType.Ucav, "ucav.pbtxt" },
        { Micromissiles.ThreatType.Brahmos, "brahmos.pbtxt" },
        { Micromissiles.ThreatType.Ascm, "ascm.pbtxt" },
        { Micromissiles.ThreatType.Fateh110B, "fateh_110b.pbtxt" },
      };

  public static string GetStreamingAssetsFilePath(string relativePath) {
    return Path.Combine(Application.streamingAssetsPath, relativePath);
  }

  public static string LoadFromStreamingAssets(string relativePath) {
    string filePath = GetStreamingAssetsFilePath(relativePath);

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX || UNITY_IOS
    if (!filePath.StartsWith("file://")) {
      filePath = "file://" + filePath;
    }
#endif

    UnityWebRequest www = UnityWebRequest.Get(filePath);
    www.SendWebRequest();

    // Wait for the request to complete
    while (!www.isDone) {
      // You might want to yield return null here if this is called from a coroutine
    }

    if (www.result != UnityWebRequest.Result.Success) {
      Debug.LogError($"Error loading file at {filePath}: {www.error}");
      return null;
    }

    return www.downloadHandler.text;
  }

  public static SimulationConfig LoadSimulationConfig(string configFile) {
    string relativePath = Path.Combine("Configs", configFile);
    string fileContent = LoadFromStreamingAssets(relativePath);

    if (string.IsNullOrEmpty(fileContent)) {
      Debug.LogError($"Failed to load SimulationConfig from {relativePath}");
      return null;
    }

    SimulationConfig config =
        JsonConvert.DeserializeObject<SimulationConfig>(fileContent, new JsonSerializerSettings {
          Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() }
        });
    UIManager.Instance.LogActionMessage($"[SIM] Loaded SimulationConfig: {configFile}.");
    return config;
  }

  public static Micromissiles.SimulatorConfig LoadSimulatorConfig() {
    string streamingAssetsPath = GetStreamingAssetsFilePath(SimulatorConfigRelativePath);
    byte[] serializedBuffer = new byte[MaxProtobufSerializedLength];
    int serializedLength = 0;
    unsafe {
      fixed(void* bufferPtr = serializedBuffer) {
        serializedLength = Protobuf.Protobuf_SimulatorConfig_LoadToBinary(
            streamingAssetsPath, (IntPtr)bufferPtr, MaxProtobufSerializedLength);
      }
    }
    var message =
        Micromissiles.SimulatorConfig.Parser.ParseFrom(serializedBuffer, 0, serializedLength);
    ProtobufInitializer.Initialize(message);
    return message;
  }

  public static Micromissiles.StaticConfig LoadStaticConfig(
      Micromissiles.InterceptorType interceptorType) {
    if (InterceptorStaticConfigMap.TryGetValue(interceptorType, out var configFile)) {
      return LoadStaticConfig(configFile);
    }
    var config = new Micromissiles.StaticConfig();
    ProtobufInitializer.Initialize(config);
    return config;
  }

  public static Micromissiles.StaticConfig LoadStaticConfig(Micromissiles.ThreatType threatType) {
    if (ThreatStaticConfigMap.TryGetValue(threatType, out var configFile)) {
      LoadStaticConfig(configFile);
    }
    var config = new Micromissiles.StaticConfig();
    ProtobufInitializer.Initialize(config);
    return config;
  }

  public static Micromissiles.StaticConfig LoadStaticConfig(string configFile) {
    string modelPath = Path.Combine("Configs/Models", configFile);
    string streamingAssetsPath = GetStreamingAssetsFilePath(modelPath);
    byte[] serializedBuffer = new byte[MaxProtobufSerializedLength];
    int serializedLength = 0;
    unsafe {
      fixed(void* bufferPtr = serializedBuffer) {
        serializedLength = Protobuf.Protobuf_StaticConfig_LoadToBinary(
            streamingAssetsPath, (IntPtr)bufferPtr, MaxProtobufSerializedLength);
      }
    }
    var message =
        Micromissiles.StaticConfig.Parser.ParseFrom(serializedBuffer, 0, serializedLength);
    ProtobufInitializer.Initialize(message);
    return message;
  }
}
