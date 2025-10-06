using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public static class ConfigLoader {
  // Initial maximum serialized length of a Protobuf message.
  // This value is estimated and is increased if necessary.
  private const int InitialMaxProtobufSerializedLength = 1024;

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
        Protobuf.Protobuf_AttackBehaviorConfig_LoadToBinary);
  }

  public static Configs.SimulationConfig LoadSimulationConfig(string configFile) {
    var config = LoadProtobufConfig<Configs.SimulationConfig>(
        Path.Combine("Configs/Simulations", configFile),
        Protobuf.Protobuf_SimulationConfig_LoadToBinary);
    if (config != null) {
      UIManager.Instance.LogActionMessage($"[SIM] Loaded simulation configuration: {configFile}.");
    }
    return config;
  }

  public static Configs.SimulatorConfig LoadSimulatorConfig() {
    return LoadProtobufConfig<Configs.SimulatorConfig>(
        SimulatorConfigRelativePath, Protobuf.Protobuf_SimulatorConfig_LoadToBinary);
  }

  public static Configs.StaticConfig LoadStaticConfig(string configFile) {
    return LoadProtobufConfig<Configs.StaticConfig>(Path.Combine("Configs/Models", configFile),
                                                    Protobuf.Protobuf_StaticConfig_LoadToBinary);
  }

  private static T LoadProtobufConfig<T>(
      string relativePath, Func<string, IntPtr, int, IntPtr, Plugin.StatusCode> loadFunction)
      where T : Google.Protobuf.IMessage<T>, new() {
    string streamingAssetsPath = GetStreamingAssetsFilePath(relativePath);

    byte[] buffer = null;
    int bufferSize = InitialMaxProtobufSerializedLength;
    int serializedLength = 0;
    Plugin.StatusCode status = Plugin.StatusCode.StatusOk;
    while (true) {
      buffer = new byte[bufferSize];
      unsafe {
        int* serializedLengthPtr = &serializedLength;
        fixed(void* bufferPtr = buffer) {
          status = loadFunction(streamingAssetsPath, (IntPtr)bufferPtr, bufferSize,
                                (IntPtr)serializedLengthPtr);
        }
      }
      if (status == Plugin.StatusCode.StatusOk) {
        return new Google.Protobuf.MessageParser<T>(() => new T())
            .ParseFrom(buffer, 0, serializedLength);
      }
      if (status == Plugin.StatusCode.StatusFailedPrecondition) {
        // Double the maximum serialized length and try again.
        bufferSize *= 2;
        continue;
      }
      Debug.Log($"Failed to load the Protobuf text file {relativePath} with status code {status}.");
      return default;
    }
  }
}
