using System.IO;
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
        args, out string simulationConfigFile, out int seed, out string outputDirectoryArg);

    Assert.IsTrue(isWorkerMode);
    Assert.AreEqual("7_ucav.pbtxt", simulationConfigFile);
    Assert.AreEqual(203, seed);
    Assert.AreEqual(Path.GetFullPath(outputDirectory), outputDirectoryArg);
  }
}
