using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;

public class MailboxTests : TestBase {
  private Mailbox _mailbox;
  private SimManager _simManager;

  [SetUp]
  public void SetUp() {
    SetMailboxInstance(null);
    _simManager = CreateSimManagerStub();
    SetSimManagerInstance(_simManager);
    _mailbox = new GameObject("Mailbox").AddComponent<Mailbox>();
  }

  [TearDown]
  public void TearDown() {
    SetMailboxInstance(null);
    SetSimManagerInstance(null);
    if (_mailbox != null) {
      UnityEngine.Object.DestroyImmediate(_mailbox.gameObject);
    }
  }

  // Verifies that the mailbox delivers zero-latency messages on the next delivery update.
  [Test]
  public void Send_WithNoCommunicationConfig_DeliversOnUpdate() {
    var sender = new CommsNode(Configs.AgentType.Vessel);
    var receiver = new CommsNode(Configs.AgentType.CarrierInterceptor);
    var message = new TestMessage(sender, receiver);
    int deliveredCount = 0;
    Message deliveredMessage = null;

    _mailbox.OnMessageDelivered += (_, delivered) => {
      ++deliveredCount;
      deliveredMessage = delivered;
    };

    SetElapsedTime(5f);
    _mailbox.Configure(null);
    _mailbox.Send(message);

    InvokePrivateMethod(_mailbox, "Update");

    Assert.AreEqual(1, deliveredCount);
    Assert.AreSame(message, deliveredMessage);
  }

  // Verifies that Send() recreates the internal queue when it has become null.
  [Test]
  public void Send_WithNullQueue_ReinitializesQueueAndDeliversMessage() {
    var sender = new CommsNode(Configs.AgentType.Vessel);
    var receiver = new CommsNode(Configs.AgentType.CarrierInterceptor);
    var message = new TestMessage(sender, receiver);
    int deliveredCount = 0;

    _mailbox.OnMessageDelivered += (_, _) => ++deliveredCount;

    _mailbox.Configure(null);
    SetPrivateField<PriorityQueue<PendingMessage>>(_mailbox, "_messageQueue", null);
    _mailbox.Send(message);

    InvokePrivateMethod(_mailbox, "Update");

    Assert.AreEqual(1, deliveredCount);
    Assert.NotNull(GetPrivateField<PriorityQueue<PendingMessage>>(_mailbox, "_messageQueue"));
  }

  // Verifies that packet delivery ratio can drop a message before it is queued for delivery.
  [Test]
  public void Send_WithZeroPacketDeliveryRatio_DropsMessage() {
    var sender = new CommsNode(Configs.AgentType.Vessel);
    var receiver = new CommsNode(Configs.AgentType.CarrierInterceptor);
    var message = new TestMessage(sender, receiver);
    int deliveredCount = 0;

    _mailbox.OnMessageDelivered += (_, _) => ++deliveredCount;
    _mailbox.Configure(new Configs.CommunicationConfig { LinkConfig = new Configs.LinkConfig {
      LatencySeconds = 0f,
      LatencyStdSeconds = 0f,
      PacketDeliveryRatio = 0f,
    } });

    _mailbox.Send(message);
    InvokePrivateMethod(_mailbox, "Update");

    Assert.AreEqual(0, deliveredCount);
  }

  // Verifies that a per-link override latency is used instead of the default link latency.
  [Test]
  public void Send_WithLinkOverride_UsesOverrideLatency() {
    var sender = new CommsNode(Configs.AgentType.Vessel);
    var receiver = new CommsNode(Configs.AgentType.CarrierInterceptor);
    var message = new TestMessage(sender, receiver);
    int deliveredCount = 0;

    _mailbox.OnMessageDelivered += (_, _) => ++deliveredCount;

    var communicationConfig =
        new Configs.CommunicationConfig { LinkConfig = new Configs.LinkConfig {
          LatencySeconds = 0f,
          LatencyStdSeconds = 0f,
          PacketDeliveryRatio = 1f,
        } };
    communicationConfig.LinkOverrides.Add(
        new Configs.LinkOverride { From = Configs.AgentType.Vessel,
                                   To = Configs.AgentType.CarrierInterceptor,
                                   LinkConfig = new Configs.LinkConfig {
                                     LatencySeconds = 2f,
                                     LatencyStdSeconds = 0f,
                                     PacketDeliveryRatio = 1f,
                                   } });

    _mailbox.Configure(communicationConfig);
    SetElapsedTime(0f);
    _mailbox.Send(message);

    InvokePrivateMethod(_mailbox, "Update");
    Assert.AreEqual(0, deliveredCount);

    SetElapsedTime(1f);
    InvokePrivateMethod(_mailbox, "Update");
    Assert.AreEqual(0, deliveredCount);

    SetElapsedTime(2f);
    InvokePrivateMethod(_mailbox, "Update");
    Assert.AreEqual(1, deliveredCount);
  }

  // Verifies that messages sent during delivery are deferred until the next update batch.
  [Test]
  public void Update_BatchesCurrentFrameMessages() {
    var sender = new CommsNode(Configs.AgentType.Vessel);
    var receiver = new CommsNode(Configs.AgentType.CarrierInterceptor);
    var firstMessage = new TestMessage(sender, receiver);
    var secondMessage = new TestMessage(sender, receiver);
    var deliveredMessages = new List<Message>();

    _mailbox.OnMessageDelivered += (_, delivered) => {
      deliveredMessages.Add(delivered);
      if (delivered == firstMessage) {
        _mailbox.Send(secondMessage);
      }
    };

    _mailbox.Configure(null);
    SetElapsedTime(0f);
    _mailbox.Send(firstMessage);

    InvokePrivateMethod(_mailbox, "Update");
    Assert.AreEqual(1, deliveredMessages.Count);
    Assert.AreSame(firstMessage, deliveredMessages[0]);

    InvokePrivateMethod(_mailbox, "Update");
    Assert.AreEqual(2, deliveredMessages.Count);
    Assert.AreSame(secondMessage, deliveredMessages[1]);
  }

  [Test]
  public void Update_WithTerminatedReceiver_SkipsDelivery() {
    var sender = new CommsNode(Configs.AgentType.Vessel);
    var receiver = new CommsNode(Configs.AgentType.CarrierInterceptor);
    var message = new TestMessage(sender, receiver);
    int deliveredCount = 0;
    int receivedCount = 0;

    _mailbox.OnMessageDelivered += (_, _) => ++deliveredCount;
    receiver.OnMessageReceived +=
        _ => ++receivedCount;

    _mailbox.Configure(null);
    _mailbox.Send(message);
    receiver.Terminate();

    InvokePrivateMethod(_mailbox, "Update");

    Assert.AreEqual(0, deliveredCount);
    Assert.AreEqual(0, receivedCount);
  }

  private static SimManager CreateSimManagerStub() {
    return (SimManager)FormatterServices.GetUninitializedObject(typeof(SimManager));
  }

  private static void SetMailboxInstance(Mailbox mailbox) {
    FieldInfo instanceField = typeof(Mailbox).GetField(
        "<Instance>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static);
    instanceField.SetValue(null, mailbox);
  }

  private static void SetSimManagerInstance(SimManager simManager) {
    FieldInfo instanceField =
        typeof(SimManager)
            .GetField("<Instance>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static);
    instanceField.SetValue(null, simManager);
  }

  private void SetElapsedTime(float elapsedTime) {
    FieldInfo elapsedTimeField = typeof(SimManager)
                                     .GetField("<ElapsedTime>k__BackingField",
                                               BindingFlags.NonPublic | BindingFlags.Instance);
    elapsedTimeField.SetValue(_simManager, elapsedTime);
  }

  private sealed class TestPayload : IMessagePayload {}

  private sealed class TestMessage : Message {
    private readonly TestPayload _payload = new TestPayload();

    public override IMessagePayload Payload => _payload;

    public TestMessage(CommsNode sender, CommsNode receiver)
        : base(sender, receiver, MessageType.AssignTargetRequest) {}
  }
}
