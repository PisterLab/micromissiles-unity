using Google.Protobuf;
using System.IO;
using System.Reflection;
using NUnit.Framework;

public class RunWorkerTests {
  [Test]
  public void TryGetWorkerModeArgumentsParsesSingleRunFlags() {
    string outputDirectory = Path.Combine(Path.GetTempPath(), "worker_run");
    string[] args = {
      "micromissiles", "--simulation_config", "7_ucav.pbtxt",  "--seed",
      "203",           "--output_dir",        outputDirectory,
    };

    bool isWorkerMode = RunWorker.TryGetWorkerModeArguments(
        args, out string simulationConfigFile, out int seed, out string outputDirectoryArg,
        out string communicationConfigOverridePath);

    Assert.IsTrue(isWorkerMode);
    Assert.AreEqual("7_ucav.pbtxt", simulationConfigFile);
    Assert.AreEqual(203, seed);
    Assert.AreEqual(Path.GetFullPath(outputDirectory), outputDirectoryArg);
    Assert.IsNull(communicationConfigOverridePath);
  }

  [Test]
  public void TryGetWorkerModeArgumentsParsesCommunicationConfigOverride() {
    string outputDirectory = Path.Combine(Path.GetTempPath(), "worker_run");
    string overridePath = Path.Combine(Path.GetTempPath(), "communication_config.pb");
    string[] args = {
      "micromissiles", "--simulation_config", "7_ucav.pbtxt",  "--seed",
      "203",           "--output_dir",        outputDirectory, "--communication_config_override",
      overridePath,
    };

    bool isWorkerMode = RunWorker.TryGetWorkerModeArguments(
        args, out _, out _, out _, out string communicationConfigOverridePath);

    Assert.IsTrue(isWorkerMode);
    Assert.AreEqual(Path.GetFullPath(overridePath), communicationConfigOverridePath);
  }

  [Test]
  public void LoadCommunicationConfigOverrideParsesSerializedConfig() {
    string overridePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    var expectedConfig = new Configs.CommunicationConfig {
      LinkConfig =
          new Configs.LinkConfig {
            LatencySeconds = 0.25f,
            LatencyStdSeconds = 0.02f,
            PacketDeliveryRatio = 0.98f,
          },
    };
    File.WriteAllBytes(overridePath, expectedConfig.ToByteArray());

    try {
      MethodInfo method = typeof(RunWorker).GetMethod("LoadCommunicationConfigOverride",
                                                      BindingFlags.NonPublic | BindingFlags.Static);
      Assert.IsNotNull(method);
      var actualConfig =
          (Configs.CommunicationConfig)method.Invoke(null, new object[] { overridePath });

      Assert.AreEqual(expectedConfig, actualConfig);
    } finally {
      File.Delete(overridePath);
    }
  }
}
