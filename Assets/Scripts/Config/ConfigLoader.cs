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
  private static readonly Dictionary<Configs.InterceptorType, string> InterceptorStaticConfigMap =
      new() {
        { Configs.InterceptorType.Hydra70, "hydra70.pbtxt" },
        { Configs.InterceptorType.Micromissile, "micromissile.pbtxt" },
      };

  // Map from the threat type to the static configuration file.
  private static readonly Dictionary<Configs.ThreatType, string> ThreatStaticConfigMap = new() {
    { Configs.ThreatType.Quadcopter, "quadcopter.pbtxt" },
    { Configs.ThreatType.Ucav, "ucav.pbtxt" },
    { Configs.ThreatType.Brahmos, "brahmos.pbtxt" },
    { Configs.ThreatType.Ascm, "ascm.pbtxt" },
    { Configs.ThreatType.Fateh110B, "fateh_110b.pbtxt" },
  };

  public static string GetStreamingAssetsFilePath(string relativePath) {
    return Path.Combine(Application.streamingAssetsPath, relativePath);
  }

  public static string LoadFromStreamingAssets(string relativePath) {
    string streamingAssetsPath = GetStreamingAssetsFilePath(relativePath);

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX || UNITY_IOS
    if (!streamingAssetsPath.StartsWith("file://")) {
      streamingAssetsPath = "file://" + streamingAssetsPath;
    }
#endif

    UnityWebRequest www = UnityWebRequest.Get(streamingAssetsPath);
    www.SendWebRequest();

    while (!www.isDone) {}
    if (www.result != UnityWebRequest.Result.Success) {
      Debug.LogError($"Error loading {streamingAssetsPath}: {www.error}.");
      return null;
    }
    return www.downloadHandler.text;
  }

  public static Configs.SimulationConfig LoadSimulationConfig(string configFile) {
    string configPath = Path.Combine("Configs/Simulations", configFile);
    string streamingAssetsPath = GetStreamingAssetsFilePath(configPath);
    byte[] serializedBuffer = new byte[MaxProtobufSerializedLength];
    int serializedLength = 0;
    unsafe {
      fixed(void* bufferPtr = serializedBuffer) {
        serializedLength = Protobuf.Protobuf_SimulationConfig_LoadToBinary(
            streamingAssetsPath, (IntPtr)bufferPtr, MaxProtobufSerializedLength);
      }
    }
    var message = Configs.SimulationConfig.Parser.ParseFrom(serializedBuffer, 0, serializedLength);
    UIManager.Instance.LogActionMessage($"[SIM] Loaded simulation config: {configFile}.");
    return message;
  }

  public static Configs.SimulatorConfig LoadSimulatorConfig() {
    string streamingAssetsPath = GetStreamingAssetsFilePath(SimulatorConfigRelativePath);
    byte[] serializedBuffer = new byte[MaxProtobufSerializedLength];
    int serializedLength = 0;
    unsafe {
      fixed(void* bufferPtr = serializedBuffer) {
        serializedLength = Protobuf.Protobuf_SimulatorConfig_LoadToBinary(
            streamingAssetsPath, (IntPtr)bufferPtr, MaxProtobufSerializedLength);
      }
    }
    var message = Configs.SimulatorConfig.Parser.ParseFrom(serializedBuffer, 0, serializedLength);
    return message;
  }

  public static Configs.StaticConfig LoadStaticConfig(Configs.InterceptorType interceptorType) {
    if (InterceptorStaticConfigMap.TryGetValue(interceptorType, out var configFile)) {
      return LoadStaticConfig(configFile);
    }
    return new Configs.StaticConfig();
  }

  public static Configs.StaticConfig LoadStaticConfig(Configs.ThreatType threatType) {
    if (ThreatStaticConfigMap.TryGetValue(threatType, out var configFile)) {
      return LoadStaticConfig(configFile);
    }
    return new Configs.StaticConfig();
  }

  public static Configs.StaticConfig LoadStaticConfig(string configFile) {
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
    return Configs.StaticConfig.Parser.ParseFrom(serializedBuffer, 0, serializedLength);
  }
}
