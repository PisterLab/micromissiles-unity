using NUnit.Framework;
using System.Reflection;
using UnityEngine;

public class MailboxTests : TestBase {
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

  [TearDown]
  public void TearDown() {
    Object.DestroyImmediate(_commsManager.gameObject);
    SetSingleton<CommsManager>(null);
    Object.DestroyImmediate(_simManager.gameObject);
    SetSingleton<SimManager>(null);
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
  public void SendMessage_DoesNotDeliverToRemovedReceiver() {
    var sender = new CommsNode(Configs.AgentType.Vessel);
    var receiver = new CommsNode(Configs.AgentType.Iads);
    _commsManager.AddNode(sender);
    _commsManager.AddNode(receiver);

    Message receivedMessage = null;
    receiver.OnReceived += message => receivedMessage = message;

    _commsManager.SendMessage(new TestMessage(sender, receiver));
    _commsManager.RemoveNode(receiver);
    SetPrivateProperty(_simManager, "ElapsedTime", 1f);
    InvokePrivateMethod(_commsManager, "FixedUpdate");

    Assert.IsNull(receivedMessage);
  }

  private sealed class TestPayload : IMessagePayload {}

  private sealed class TestMessage : Message<TestPayload> {
    public TestMessage(CommsNode sender, CommsNode receiver)
        : base(sender, receiver, MessageType.AssignTargetRequest, new TestPayload()) {}
  }

  private static void SetSingleton<T>(T instance) {
    typeof(T)
        .GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
        .SetValue(null, instance);
  }
}
