using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

public class MailboxTests {
  private static string GetProtoConfigPath(string fileName) {
    return Path.Combine(Application.dataPath, "Proto", "Configs", fileName);
  }

  private static string GetSimulatorConfigPath() {
    return Path.Combine(Application.streamingAssetsPath, "simulator.pbtxt");
  }

  // Verifies that SimulatorConfig imports CommunicationConfig and exposes it as field 7.
  [Test]
  public void SimulatorConfigProto_ImportsCommunicationConfig() {
    string contents = File.ReadAllText(GetProtoConfigPath("simulator_config.proto"));
    StringAssert.Contains("import \"Configs/communication_config.proto\";", contents);
    StringAssert.Contains("CommunicationConfig communication_config = 7;", contents);
  }

  // Verifies that CommunicationConfig imports MailboxConfig and exposes mailbox_config as field 1.
  [Test]
  public void CommunicationConfigProto_ImportsMailboxConfig() {
    string contents = File.ReadAllText(GetProtoConfigPath("communication_config.proto"));
    StringAssert.Contains("import \"Configs/mailbox_config.proto\";", contents);
    StringAssert.Contains("MailboxConfig mailbox_config = 1;", contents);
  }

  // Verifies that MailboxConfig defines the expected enums, messages, and fields used by runtime code.
  [Test]
  public void MailboxConfigProto_DefinesExpectedEnumsAndFields() {
    string contents = File.ReadAllText(GetProtoConfigPath("mailbox_config.proto"));
    StringAssert.Contains("enum MailboxNode", contents);
    StringAssert.Contains("MAILBOX_NODE_IADS = 1;", contents);
    StringAssert.Contains("MAILBOX_NODE_CARRIER = 2;", contents);
    StringAssert.Contains("MAILBOX_NODE_INTERCEPTOR = 3;", contents);
    StringAssert.Contains("message MailboxLatencyEntry", contents);
    StringAssert.Contains("MailboxNode from = 1;", contents);
    StringAssert.Contains("MailboxNode to = 2;", contents);
    StringAssert.Contains("float seconds = 3;", contents);
    StringAssert.Contains("message MailboxConfig", contents);
    StringAssert.Contains("LATENCY_MODE_UNSPECIFIED = 0;", contents);
    StringAssert.Contains("NO_LATENCY = 1;", contents);
    StringAssert.Contains("UNIFORM_LATENCY = 2;", contents);
    StringAssert.Contains("INDIVIDUAL_LATENCY = 3;", contents);
    StringAssert.Contains("LatencyMode latency_mode = 1;", contents);
    StringAssert.Contains("float uniform_latency = 2;", contents);
    StringAssert.Contains("float latency_jitter_std_seconds = 3;", contents);
    StringAssert.Contains("repeated MailboxLatencyEntry latency_overrides = 4;", contents);
  }

  // Verifies that BUILD.bazel wires the proto dependency chain from SimulatorConfig to MailboxConfig.
  [Test]
  public void BuildBazel_WiresCommunicationAndMailboxProtoDependencies() {
    string contents = File.ReadAllText(GetProtoConfigPath("BUILD.bazel"));
    StringAssert.Contains("name = \"communication_config_proto\"", contents);
    StringAssert.Contains("srcs = [\"communication_config.proto\"]", contents);
    StringAssert.Contains("deps = [\":mailbox_config_proto\"]", contents);
    StringAssert.Contains("name = \"mailbox_config_proto\"", contents);
    StringAssert.Contains("srcs = [\"mailbox_config.proto\"]", contents);
    StringAssert.Contains("name = \"simulator_config_proto\"", contents);
    StringAssert.Contains("deps = [\":communication_config_proto\"]", contents);
  }

  // Verifies that simulator.pbtxt nests mailbox_config under communication_config instead of top level.
  [Test]
  public void SimulatorPbtxt_NestsMailboxConfigUnderCommunicationConfig() {
    string contents = File.ReadAllText(GetSimulatorConfigPath());
    StringAssert.Contains("communication_config {", contents);
    StringAssert.Contains("mailbox_config {", contents);
    Assert.IsTrue(Regex.IsMatch(contents, @"communication_config\s*\{\s*mailbox_config\s*\{", RegexOptions.Multiline), "Expected mailbox_config to be nested under communication_config.");
    Assert.IsFalse(Regex.IsMatch(contents, @"^mailbox_config\s*\{", RegexOptions.Multiline), "mailbox_config should not appear as a top-level simulator config field.");
  }

  // Verifies that the sample simulator.pbtxt includes the intended mailbox mode, jitter, and node overrides.
  [Test]
  public void SimulatorPbtxt_UsesExpectedMailboxConfigFields() {
    string contents = File.ReadAllText(GetSimulatorConfigPath());
    StringAssert.Contains("latency_mode: INDIVIDUAL_LATENCY", contents);
    StringAssert.Contains("latency_jitter_std_seconds: 0.02", contents);
    StringAssert.Contains("from: MAILBOX_NODE_IADS", contents);
    StringAssert.Contains("to: MAILBOX_NODE_CARRIER", contents);
    StringAssert.Contains("to: MAILBOX_NODE_INTERCEPTOR", contents);
  }
}
