using NUnit.Framework;
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
  public void SendMessage_DiscardsIdenticalTargetUntilCooldownExpires() {
    ConfigureImmediateDelivery(0.25f);
    var sender = new CommsNode(Configs.AgentType.Vessel);
    var receiver = new CommsNode(Configs.AgentType.Iads);
    var target = new HierarchicalBase();
    _commsManager.AddNode(sender);
    _commsManager.AddNode(receiver);
    int receivedCount = 0;
    receiver.OnReceived +=
        _ => ++receivedCount;

    _commsManager.SendMessage(new AssignTargetResponseMessage(sender, receiver, target));
    SetPrivateProperty(_simManager, "ElapsedTime", 0.125f);
    _commsManager.SendMessage(new AssignTargetResponseMessage(sender, receiver, target));
    Assert.AreEqual(1, receivedCount);

    SetPrivateProperty(_simManager, "ElapsedTime", 0.25f);
    _commsManager.SendMessage(new AssignTargetResponseMessage(sender, receiver, target));
    Assert.AreEqual(2, receivedCount);

    SetPrivateProperty(_simManager, "ElapsedTime", 0.375f);
    _commsManager.SendMessage(new AssignTargetResponseMessage(sender, receiver, target));
    Assert.AreEqual(2, receivedCount);
  }

  [Test]
  public void SendMessage_ChangedTargetBypassesAndResetsCooldown() {
    ConfigureImmediateDelivery(0.25f);
    var sender = new CommsNode(Configs.AgentType.Vessel);
    var receiver = new CommsNode(Configs.AgentType.Iads);
    var firstTarget = new HierarchicalBase();
    var secondTarget = new HierarchicalBase();
    _commsManager.AddNode(sender);
    _commsManager.AddNode(receiver);
    int receivedCount = 0;
    receiver.OnReceived +=
        _ => ++receivedCount;

    _commsManager.SendMessage(new AssignTargetResponseMessage(sender, receiver, firstTarget));
    SetPrivateProperty(_simManager, "ElapsedTime", 0.125f);
    _commsManager.SendMessage(new AssignTargetResponseMessage(sender, receiver, secondTarget));
    Assert.AreEqual(2, receivedCount);

    SetPrivateProperty(_simManager, "ElapsedTime", 0.25f);
    _commsManager.SendMessage(new AssignTargetResponseMessage(sender, receiver, secondTarget));
    Assert.AreEqual(2, receivedCount);

    SetPrivateProperty(_simManager, "ElapsedTime", 0.375f);
    _commsManager.SendMessage(new AssignTargetResponseMessage(sender, receiver, secondTarget));
    Assert.AreEqual(3, receivedCount);
  }

  [Test]
  public void SendMessage_CooldownIsIndependentPerSenderReceiverAndType() {
    ConfigureImmediateDelivery(0.25f);
    var firstSender = new CommsNode(Configs.AgentType.Vessel);
    var secondSender = new CommsNode(Configs.AgentType.Vessel);
    var firstReceiver = new CommsNode(Configs.AgentType.Iads);
    var secondReceiver = new CommsNode(Configs.AgentType.Iads);
    var target = new HierarchicalBase();
    _commsManager.AddNode(firstSender);
    _commsManager.AddNode(secondSender);
    _commsManager.AddNode(firstReceiver);
    _commsManager.AddNode(secondReceiver);
    int firstReceiverCount = 0;
    int secondReceiverCount = 0;
    firstReceiver.OnReceived +=
        _ => ++firstReceiverCount;
    secondReceiver.OnReceived +=
        _ => ++secondReceiverCount;

    _commsManager.SendMessage(new AssignTargetResponseMessage(firstSender, firstReceiver, target));
    _commsManager.SendMessage(new AssignTargetResponseMessage(secondSender, firstReceiver, target));
    _commsManager.SendMessage(new AssignTargetResponseMessage(firstSender, secondReceiver, target));
    _commsManager.SendMessage(new ReassignTargetRequestMessage(firstSender, firstReceiver, target));

    Assert.AreEqual(3, firstReceiverCount);
    Assert.AreEqual(1, secondReceiverCount);
  }

  [Test]
  public void SendMessage_ZeroCooldownAllowsIdenticalMessages() {
    ConfigureImmediateDelivery(0f);
    var sender = new CommsNode(Configs.AgentType.Vessel);
    var receiver = new CommsNode(Configs.AgentType.Iads);
    var target = new HierarchicalBase();
    _commsManager.AddNode(sender);
    _commsManager.AddNode(receiver);
    int receivedCount = 0;
    receiver.OnReceived +=
        _ => ++receivedCount;

    _commsManager.SendMessage(new AssignTargetResponseMessage(sender, receiver, target));
    _commsManager.SendMessage(new AssignTargetResponseMessage(sender, receiver, target));

    Assert.AreEqual(2, receivedCount);
  }

  private void ConfigureImmediateDelivery(float cooldownSeconds) {
    Configs.CommunicationConfig config = _simManager.SimulationConfig.CommunicationConfig;
    config.CooldownSeconds = cooldownSeconds;
    config.LinkConfig.LatencySeconds = 0f;
  }
}
