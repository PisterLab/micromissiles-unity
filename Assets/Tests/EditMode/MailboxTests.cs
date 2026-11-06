using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class MailboxTests : TestBase {
  private sealed class TestPayload : IMessagePayload {}

  private sealed class TestMessage : Message<TestPayload> {
    public TestMessage(CommsNode sender, CommsNode receiver)
        : base(sender, receiver, MessageType.AssignTargetRequest, new TestPayload()) {}
  }

  private SimManager _simManager;
  private CommsManager _commsManager;

  [SetUp]
  public void SetUp() {
    _simManager = new GameObject("SimManager").AddComponent<SimManager>();
    SetSingleton(_simManager);
    _simManager.SimulationConfig = new Configs.SimulationConfig {
      CommunicationConfig =
          new Configs.CommunicationConfig {
            LinkConfig =
                new Configs.LinkConfig {
                  LatencySeconds = 1f,
                  PacketDeliveryRatio = 1f,
                },
            TimingConfig = new Configs.TimingConfig(),
          },
    };
    SetPrivateProperty(_simManager, "IsRunning", true);

    _commsManager = new GameObject("CommsManager").AddComponent<CommsManager>();
    SetSingleton(_commsManager);
  }

  [Test]
  public void SendMessage_DeliversAfterConfiguredLatency() {
    var sender = new CommsNode(Configs.AgentType.Vessel);
    var receiver = new CommsNode(Configs.AgentType.Iads);
    _commsManager.AddNode(sender);
    _commsManager.AddNode(receiver);

    Message receivedMessage = null;
    receiver.OnReceived += message => receivedMessage = message;
    var sentMessage = new TestMessage(sender, receiver);

    _commsManager.SendMessage(sentMessage);
    InvokePrivateMethod(_commsManager, "FixedUpdate");
    Assert.IsNull(receivedMessage);

    SetPrivateProperty(_simManager, "ElapsedTime", 1f);
    InvokePrivateMethod(_commsManager, "FixedUpdate");
    Assert.AreSame(sentMessage, receivedMessage);
  }

  [Test]
  public void SendMessage_RoundsLatencyAdjustedDeliveryUpToNextLink16Slot() {
    _simManager.SimulationConfig.CommunicationConfig.LinkConfig.LatencySeconds = 0.4f;
    _simManager.SimulationConfig.CommunicationConfig.TimingConfig.Link16SlotDurationSeconds = 1f;
    SetPrivateProperty(_simManager, "ElapsedTime", 2.3f);

    var sender = new CommsNode(Configs.AgentType.Vessel);
    var receiver = new CommsNode(Configs.AgentType.Iads);
    _commsManager.AddNode(sender);
    _commsManager.AddNode(receiver);

    Message receivedMessage = null;
    receiver.OnReceived += message => receivedMessage = message;
    var sentMessage = new TestMessage(sender, receiver);

    _commsManager.SendMessage(sentMessage);

    SetPrivateProperty(_simManager, "ElapsedTime", 2.999f);
    InvokePrivateMethod(_commsManager, "FixedUpdate");
    Assert.IsNull(receivedMessage);

    SetPrivateProperty(_simManager, "ElapsedTime", 3f);
    InvokePrivateMethod(_commsManager, "FixedUpdate");
    Assert.AreSame(sentMessage, receivedMessage);
  }

  [Test]
  public void SendMessage_DeliversUpToBandwidthLimitPerSlotInFifoOrder() {
    _simManager.SimulationConfig.CommunicationConfig.TimingConfig.Link16SlotDurationSeconds = 1f;
    _simManager.SimulationConfig.CommunicationConfig.TimingConfig.Link16MaxBandwidth = 2;

    var sender = new CommsNode(Configs.AgentType.Vessel);
    var receiver = new CommsNode(Configs.AgentType.Iads);
    _commsManager.AddNode(sender);
    _commsManager.AddNode(receiver);

    var receivedMessages = new List<Message>();
    receiver.OnReceived += message => receivedMessages.Add(message);
    var firstMessage = new TestMessage(sender, receiver);
    var secondMessage = new TestMessage(sender, receiver);
    var thirdMessage = new TestMessage(sender, receiver);

    _commsManager.SendMessage(firstMessage);
    _commsManager.SendMessage(secondMessage);
    _commsManager.SendMessage(thirdMessage);

    SetPrivateProperty(_simManager, "ElapsedTime", 1f);
    InvokePrivateMethod(_commsManager, "FixedUpdate");
    CollectionAssert.AreEqual(new[] { firstMessage, secondMessage }, receivedMessages);

    // Repeated delivery checks within the same slot must not reset its bandwidth allowance.
    SetPrivateProperty(_simManager, "ElapsedTime", 1.5f);
    InvokePrivateMethod(_commsManager, "FixedUpdate");
    CollectionAssert.AreEqual(new[] { firstMessage, secondMessage }, receivedMessages);

    SetPrivateProperty(_simManager, "ElapsedTime", 2f);
    InvokePrivateMethod(_commsManager, "FixedUpdate");
    CollectionAssert.AreEqual(new[] { firstMessage, secondMessage, thirdMessage },
                              receivedMessages);
  }

  [Test]
  public void SendMessage_DoesNotDeliverToUnregisteredReceiver() {
    var sender = new CommsNode(Configs.AgentType.Vessel);
    var receiver = new CommsNode(Configs.AgentType.Iads);
    _commsManager.AddNode(sender);

    Message receivedMessage = null;
    receiver.OnReceived += message => receivedMessage = message;

    _commsManager.SendMessage(new TestMessage(sender, receiver));
    SetPrivateProperty(_simManager, "ElapsedTime", 1f);
    InvokePrivateMethod(_commsManager, "FixedUpdate");

    Assert.IsNull(receivedMessage);
  }

  [Test]
  public void SendMessage_DropsMessageWhenPacketDeliveryRatioIsZero() {
    _simManager.SimulationConfig.CommunicationConfig.LinkConfig.PacketDeliveryRatio = 0f;

    var sender = new CommsNode(Configs.AgentType.Vessel);
    var receiver = new CommsNode(Configs.AgentType.Iads);
    _commsManager.AddNode(sender);
    _commsManager.AddNode(receiver);

    Message receivedMessage = null;
    receiver.OnReceived += message => receivedMessage = message;

    _commsManager.SendMessage(new TestMessage(sender, receiver));
    SetPrivateProperty(_simManager, "ElapsedTime", 1f);
    InvokePrivateMethod(_commsManager, "FixedUpdate");

    Assert.IsNull(receivedMessage);
  }

  [Test]
  public void GetLinkConfig_ReturnsMatchingLinkOverride() {
    var overrideConfig = new Configs.LinkConfig { CooldownRetrySeconds = 2f };
    _simManager.SimulationConfig.CommunicationConfig.LinkOverrides.Add(new Configs.LinkOverride {
      From = Configs.AgentType.Vessel,
      To = Configs.AgentType.Iads,
      LinkConfig = overrideConfig,
    });

    Configs.LinkConfig resolvedConfig = _commsManager.GetLinkConfig(
        new CommsNode(Configs.AgentType.Vessel), new CommsNode(Configs.AgentType.Iads));

    Assert.AreSame(overrideConfig, resolvedConfig);
  }
}
