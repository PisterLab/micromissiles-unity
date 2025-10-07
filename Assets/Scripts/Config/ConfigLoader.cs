using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public static class ConfigLoader {
  // Delegate for loading from a Protobuf text file and returning the length of the serialized
  // message as an output argument.
  private delegate Plugin.StatusCode SerializedProtobufLengthDelegate(string file,
                                                                      out int serializedLength);
  // Delegate for loading from a Protobuf text file to binary format and returning the length of the
  // serialized message as an output argument.
  private delegate Plugin.StatusCode LoadProtobufDelegate(string file, byte[] buffer,
                                                          int bufferSize, out int serializedLength);

  // Relative path to the default simulator configuration.
  private const string SimulatorConfigRelativePath = "simulator.pbtxt";

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

    using (UnityWebRequest www = UnityWebRequest.Get(streamingAssetsPath)) {
      www.SendWebRequest();
      while (!www.isDone) {}

      if (www.result != UnityWebRequest.Result.Success) {
        Debug.LogError($"Failed to load {streamingAssetsPath}: {www.error}.");
        return null;
      }
      return www.downloadHandler.text;
    }
  }

  public static Configs.AttackBehaviorConfig LoadAttackBehaviorConfig(string configFile) {
    return LoadProtobufConfig<Configs.AttackBehaviorConfig>(
        Path.Combine("Configs/Attacks", configFile),
        Protobuf.Protobuf_AttackBehaviorConfig_GetSerializedLength,
        Protobuf.Protobuf_AttackBehaviorConfig_LoadToBinary);
  }

  public static Configs.SimulationConfig LoadSimulationConfig(string configFile) {
    var config = LoadProtobufConfig<Configs.SimulationConfig>(
        Path.Combine("Configs/Simulations", configFile),
        Protobuf.Protobuf_SimulationConfig_GetSerializedLength,
        Protobuf.Protobuf_SimulationConfig_LoadToBinary);
    if (config != null) {
      UIManager.Instance.LogActionMessage($"[SIM] Loaded simulation configuration: {configFile}.");
    }
    return config;
  }

  public static Configs.SimulatorConfig LoadSimulatorConfig() {
    return LoadProtobufConfig<Configs.SimulatorConfig>(
        SimulatorConfigRelativePath, Protobuf.Protobuf_SimulatorConfig_GetSerializedLength,
        Protobuf.Protobuf_SimulatorConfig_LoadToBinary);
  }

  public static Configs.StaticConfig LoadStaticConfig(string configFile) {
    return LoadProtobufConfig<Configs.StaticConfig>(
        Path.Combine("Configs/Models", configFile),
        Protobuf.Protobuf_StaticConfig_GetSerializedLength,
        Protobuf.Protobuf_StaticConfig_LoadToBinary);
  }

  private static T LoadProtobufConfig<T>(string relativePath,
                                         SerializedProtobufLengthDelegate serializedLengthFunction,
                                         LoadProtobufDelegate loadFunction)
      where T : Google.Protobuf.IMessage<T>, new() {
    string streamingAssetsPath = GetStreamingAssetsFilePath(relativePath);

    // Determine the length of the serialized Protobuf message.
    Plugin.StatusCode status =
        serializedLengthFunction(streamingAssetsPath, out int serializedLength);
    if (status != Plugin.StatusCode.StatusOk) {
      Debug.Log(
          $"Failed to get the length of the serialized message from the Protobuf text file {relativePath} with status code {status}.");
      return default;
    }

    // Load the Protobuf message to binary format.
    byte[] buffer = new byte[serializedLength];
    status = loadFunction(streamingAssetsPath, buffer, serializedLength, out serializedLength);
    if (status != Plugin.StatusCode.StatusOk) {
      Debug.Log($"Failed to load the Protobuf text file {relativePath} with status code {status}.");
      return default;
    }
    return new Google.Protobuf.MessageParser<T>(() => new T())
        .ParseFrom(buffer, 0, serializedLength);
  }
}
