using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using UnityEngine.SceneManagement;
using System.IO;
public class ConfigTest : TestBase {
  [OneTimeSetUp]
  public void LoadScene() {
    SceneManager.LoadScene("Scenes/MainScene");
  }

  [UnityTest]
  public IEnumerator TestAllConfigFilesLoad() {
    string configPath = Path.Combine(Application.streamingAssetsPath, "Configs");
    string[] jsonFiles = Directory.GetFiles(configPath, "*.json");

    Assert.IsTrue(jsonFiles.Length > 0, "No JSON files found in the Configs directory");

    foreach (string jsonFile in jsonFiles) {
      yield return new WaitForSeconds(0.1f);
      SimManager.Instance.LoadNewConfig(jsonFile);
    }
  }

  
}
