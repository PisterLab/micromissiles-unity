using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class RunWorkerTests : TestBase {
  [UnityTest]
  [Order(1)]
  public IEnumerator InteractiveModeAutoStartsWithoutWorkerFlags() {
    yield return LoadMainScene();

    Assert.IsNull(RunWorker.Instance);
    Assert.IsTrue(SimManager.Instance.IsRunning);

    yield return new WaitForSecondsRealtime(0.1f);
    Assert.Greater(SimManager.Instance.ElapsedTime, 0f);
  }

  [UnityTest]
  [Order(2)]
  public IEnumerator WorkerRunWritesAssignedLogsAndRespectsSeeds() {
    yield return LoadMainScene();

    if (SimManager.Instance.IsRunning) {
      SimManager.Instance.EndSimulation();
      yield return null;
    }

    string firstRunEvents = null;
    yield return ExecuteWorkerRun(seed: 203, outputDir: CreateOutputDirectory("first"),
                                  onCompleted: contents => firstRunEvents = contents);

    string secondRunEvents = null;
    yield return ExecuteWorkerRun(seed: 203, outputDir: CreateOutputDirectory("second"),
                                  onCompleted: contents => secondRunEvents = contents);

    string differentSeedEvents = null;
    yield return ExecuteWorkerRun(seed: 204, outputDir: CreateOutputDirectory("different"),
                                  onCompleted: contents => differentSeedEvents = contents);

    Assert.AreEqual(firstRunEvents, secondRunEvents);
    Assert.AreNotEqual(firstRunEvents, differentSeedEvents);
  }

  private static string CreateOutputDirectory(string name) {
    return Path.Combine(Path.GetTempPath(), "micromissiles_run_worker_tests",
                        $"{name}_{Guid.NewGuid():N}");
  }

  private IEnumerator ExecuteWorkerRun(int seed, string outputDir, Action<string> onCompleted) {
    var workerObject = new GameObject($"RunWorker_{seed}");
    var worker = workerObject.AddComponent<RunWorker>();
    InvokePrivateMethod(worker, "Initialize", "7_ucav.pbtxt", seed, outputDir);

    yield return new WaitUntil(() => Directory.Exists(outputDir));
    yield return new WaitUntil(() => SimManager.Instance.IsRunning);
    yield return new WaitForSecondsRealtime(0.1f);
    SimManager.Instance.EndSimulation();
    yield return null;

    yield return new WaitUntil(() => Directory.GetFiles(outputDir, "sim_events_*.csv").Length == 1);
    string eventLogPath = Directory.GetFiles(outputDir, "sim_events_*.csv")[0];
    Assert.AreEqual(outputDir, Path.GetDirectoryName(eventLogPath));
    onCompleted(File.ReadAllText(eventLogPath));

    UnityEngine.Object.Destroy(workerObject);
    yield return null;
    Directory.Delete(outputDir, recursive: true);
  }

  private static IEnumerator LoadMainScene() {
    SceneManager.LoadScene("Scenes/MainScene");
    yield return null;
  }
}
