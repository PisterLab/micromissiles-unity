using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

public class MailboxTests {
  private static string GetProtoConfigPath(string fileName) {
    return Path.Combine(Application.dataPath, "Proto", "Configs", fileName);
  }

  private static string GetSimulationConfigPath(string fileName) {
    return Path.Combine(Application.streamingAssetsPath, "Configs", "Simulations", fileName);
  }

  // Verifies that BUILD.bazel wires the mailbox proto dependency chain into SimulationConfig.
  [Test]
  public void BuildBazel_WiresSimulationCommunicationAndMailboxProtoDependencies() {
    string contents = File.ReadAllText(GetProtoConfigPath("BUILD.bazel"));
    StringAssert.Contains("name = \"simulation_config_proto\"", contents);
    StringAssert.Contains("srcs = [\"simulation_config.proto\"]", contents);
    StringAssert.Contains(":communication_config_proto", contents);
    StringAssert.Contains("name = \"communication_config_proto\"", contents);
    StringAssert.Contains("srcs = [\"communication_config.proto\"]", contents);
    StringAssert.Contains("deps = [\":mailbox_config_proto\"]", contents);
    StringAssert.Contains("name = \"mailbox_config_proto\"", contents);
    StringAssert.Contains("srcs = [\"mailbox_config.proto\"]", contents);
  }

  // Verifies that the sample simulation config nests mailbox_config under communication_config.
  [Test]
  public void SimulationConfig_NestsMailboxConfigUnderCommunicationConfig() {
    string contents = File.ReadAllText(GetSimulationConfigPath("4_swarms_1_ucav.pbtxt"));
    StringAssert.Contains("communication_config {", contents);
    StringAssert.Contains("mailbox_config {", contents);
    Assert.IsTrue(Regex.IsMatch(contents, @"communication_config\s*\{\s*mailbox_config\s*\{", RegexOptions.Multiline), "Expected mailbox_config to be nested under communication_config.");
  }

  // Verifies that the sample simulation config exercises the intended mailbox mode, jitter, and node overrides.
  [Test]
  public void SimulationConfig_UsesExpectedMailboxConfigFields() {
    string contents = File.ReadAllText(GetSimulationConfigPath("4_swarms_1_ucav.pbtxt"));
    StringAssert.Contains("latency_mode: INDIVIDUAL_LATENCY", contents);
    StringAssert.Contains("latency_jitter_std_seconds: 0.02", contents);
    StringAssert.Contains("from: MAILBOX_NODE_IADS", contents);
    StringAssert.Contains("to: MAILBOX_NODE_CARRIER", contents);
    StringAssert.Contains("to: MAILBOX_NODE_INTERCEPTOR", contents);
  }

  // Verifies that the sample simulation config does not declare duplicate mailbox latency overrides. Assert they are all unique, and it protects against accidentally writing repeated items.
  [Test]
  public void SimulationConfig_DoesNotRepeatMailboxLatencyOverrides() {
    string contents = File.ReadAllText(GetSimulationConfigPath("4_swarms_1_ucav.pbtxt"));
    MatchCollection matches = Regex.Matches(contents, @"latency_overrides\s*\{\s*from:\s*(\w+)\s+to:\s*(\w+)\s+seconds:\s*([0-9.]+)\s*\}", RegexOptions.Multiline);
    Assert.IsNotEmpty(matches, "Expected at least one mailbox latency override in the sample config.");
    string[] fromToPairs = matches.Select(match => $"{match.Groups[1].Value}->{match.Groups[2].Value}").ToArray();
    CollectionAssert.AllItemsAreUnique(fromToPairs);
  }
}
