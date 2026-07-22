using NUnit.Framework;
using System.Reflection;
using UnityEngine;

public class MailboxTests : TestBase {
  private GameObject _simManagerObject;
  private GameObject _mailboxObject;
  private SimManager _simManager;
  private Mailbox _mailbox;

  [SetUp]
  public void SetUp() {
    _simManagerObject = new GameObject("SimManager");
    _simManager = _simManagerObject.AddComponent<SimManager>();
    InvokePrivateMethod(_simManager, "Awake");

    _mailboxObject = new GameObject("Mailbox");
    _mailbox = _mailboxObject.AddComponent<Mailbox>();
    InvokePrivateMethod(_mailbox, "Awake");
  }

  [TearDown]
  public void TearDown() {
    if (_mailboxObject != null) {
      Object.DestroyImmediate(_mailboxObject);
      _mailboxObject = null;
    }
    if (_simManagerObject != null) {
      Object.DestroyImmediate(_simManagerObject);
      _simManagerObject = null;
    }

    SetStaticInstance<Mailbox>(null);
    SetStaticInstance<SimManager>(null);
  }

  [Test]
  public void Send_DeliversMessageOnlyAfterConfiguredLatency() {
    _mailbox.Configure(new Configs.CommunicationConfig { LinkConfig = new Configs.LinkConfig {
      LatencySeconds = 1f,
      LatencyStdSeconds = 0f,
      PacketDeliveryRatio = 1f,
    } });

    var sender = new CommsNode(Configs.AgentType.Iads);
    var receiver = new CommsNode(Configs.AgentType.ShoreBattery);
    int receivedCount = 0;
    Message receivedMessage = null;
    receiver.OnMessageReceived += message => {
      receivedCount++;
      receivedMessage = message;
    };

    var target = new FixedHierarchical(position: Vector3.one);
    var messageToSend = new AssignTargetResponseMessage(sender, receiver, target);

    SetPrivateProperty(_simManager, "ElapsedTime", 0f);
    _mailbox.Send(messageToSend);

    SetPrivateProperty(_simManager, "ElapsedTime", 0.99f);
    InvokePrivateMethod(_mailbox, "DeliverMessages");
    Assert.AreEqual(0, receivedCount);
    Assert.IsNull(receivedMessage);

    SetPrivateProperty(_simManager, "ElapsedTime", 1f);
    InvokePrivateMethod(_mailbox, "DeliverMessages");
    Assert.AreEqual(1, receivedCount);
    Assert.AreSame(messageToSend, receivedMessage);
  }

  private static void SetStaticInstance<T>(T value)
      where T : class {
    PropertyInfo instanceProperty =
        typeof(T).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
    instanceProperty.SetValue(obj: null, value: value);
  }
}
