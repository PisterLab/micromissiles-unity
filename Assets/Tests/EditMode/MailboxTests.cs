using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class MailboxTests : TestBase {
  private const float _epsilon = 1e-2f;

  private readonly List<GameObject> _createdObjects = new();

  [TearDown]
  public void TearDown() {
    foreach (GameObject gameObject in _createdObjects) {
      if (gameObject != null) {
        Object.DestroyImmediate(gameObject);
      }
    }

    foreach (Mailbox mailbox in Object.FindObjectsByType<Mailbox>(FindObjectsSortMode.None)) {
      if (mailbox != null) {
        Object.DestroyImmediate(mailbox.gameObject);
      }
    }
  }

  [Test]
  public void Send_ZeroLatency_DeliversOnNextUpdate() {
    Mailbox mailbox = CreateMailbox(baseLatency: 0f);
    IadsCommsAgent sender = CreateCommsAgent("Sender");
    IadsCommsAgent receiver = CreateCommsAgent("Receiver");
    Message deliveredMessage = null;
    IAgent deliveredReceiver = null;

    mailbox.OnMessageDelivered += (messageReceiver, message) => {
      deliveredReceiver = messageReceiver;
      deliveredMessage = message;
    };

    Message message = new AssignTargetMessage(sender, receiver, new FixedHierarchical());
    mailbox.Send(message);
    InvokePrivateMethod(mailbox, "Update");

    Assert.AreSame(receiver, deliveredReceiver);
    Assert.AreSame(message, deliveredMessage);
  }

  [Test]
  public void Send_NonZeroLatency_DoesNotDeliverImmediately() {
    Mailbox mailbox = CreateMailbox(baseLatency: 0.5f);
    IadsCommsAgent sender = CreateCommsAgent("Sender");
    IadsCommsAgent receiver = CreateCommsAgent("Receiver");
    int deliveredCount = 0;

    mailbox.OnMessageDelivered += (_, _) => { ++deliveredCount; };

    mailbox.Send(new AssignTargetMessage(sender, receiver, new FixedHierarchical()));
    PendingMessage pendingMessage = PeekPendingMessage(mailbox);
    InvokePrivateMethod(mailbox, "Update");

    Assert.That(pendingMessage.DeliverAt, Is.GreaterThan(Time.time));
    Assert.AreEqual(0, deliveredCount);
    Assert.IsFalse(GetPrivateField<PriorityQueue<PendingMessage>>(mailbox, "_messageQueue").IsEmpty());
  }

  [Test]
  public void Send_WithLatencyJitter_AdjustsScheduledTimeDeterministically() {
    const float baseLatency = 0.2f;
    const float jitterStdSeconds = 0.1f;
    const int randomSeed = 12345;

    Mailbox mailbox = CreateMailbox(baseLatency, jitterStdSeconds);
    IadsCommsAgent sender = CreateCommsAgent("Sender");
    IadsCommsAgent receiver = CreateCommsAgent("Receiver");
    float timeBeforeSend = Time.time;

    Random.InitState(randomSeed);
    mailbox.Send(new AssignTargetMessage(sender, receiver, new FixedHierarchical()));

    PendingMessage pendingMessage = PeekPendingMessage(mailbox);
    float expectedLatency = ComputeExpectedLatency(baseLatency, jitterStdSeconds, randomSeed);

    Assert.That(pendingMessage.DeliverAt, Is.EqualTo(timeBeforeSend + expectedLatency).Within(_epsilon));
  }

  [Test]
  public void Send_TerminatedReceiver_DoesNotDeliver() {
    Mailbox mailbox = CreateMailbox(baseLatency: 0f);
    IadsCommsAgent sender = CreateCommsAgent("Sender");
    IadsCommsAgent receiver = CreateCommsAgent("Receiver");
    int deliveredCount = 0;

    mailbox.OnMessageDelivered += (_, _) => { ++deliveredCount; };

    receiver.Terminate();
    mailbox.Send(new AssignTargetMessage(sender, receiver, new FixedHierarchical()));
    InvokePrivateMethod(mailbox, "Update");

    Assert.AreEqual(0, deliveredCount);
  }

  [Test]
  public void AgentBase_OnlyReceiverHandlesDeliveredMessage() {
    Mailbox mailbox = CreateMailbox(baseLatency: 0f);
    TestMailboxAgent sender = CreateMailboxAgent("Sender");
    TestMailboxAgent receiver = CreateMailboxAgent("Receiver");
    TestMailboxAgent otherAgent = CreateMailboxAgent("OtherAgent");
    Message message = new AssignTargetMessage(sender, receiver, new FixedHierarchical());

    mailbox.Send(message);
    InvokePrivateMethod(mailbox, "Update");

    Assert.AreEqual(0, sender.ReceivedCount);
    Assert.AreEqual(1, receiver.ReceivedCount);
    Assert.AreSame(message, receiver.LastMessage);
    Assert.AreEqual(0, otherAgent.ReceivedCount);
  }

  [Test]
  public void AssignTargetMessage_StoresExpectedMetadataAndPayload() {
    IadsCommsAgent sender = CreateCommsAgent("Sender");
    IadsCommsAgent receiver = CreateCommsAgent("Receiver");
    FixedHierarchical target = new(position: Vector3.one);
    AssignTargetMessage message = new(sender, receiver, target);

    Assert.AreSame(sender, message.Sender);
    Assert.AreSame(receiver, message.Receiver);
    Assert.AreEqual(MessageType.AssignTarget, message.Type);
    Assert.AreSame(message.PayloadData, message.Payload);
    Assert.AreSame(target, message.PayloadData.Target);
  }

  [Test]
  public void ReassignTargetRequestMessage_StoresExpectedMetadataAndPayload() {
    IadsCommsAgent sender = CreateCommsAgent("Sender");
    IadsCommsAgent receiver = CreateCommsAgent("Receiver");
    FixedHierarchical target = new(position: Vector3.forward);
    ReassignTargetRequestMessage message = new(sender, receiver, target);

    Assert.AreSame(sender, message.Sender);
    Assert.AreSame(receiver, message.Receiver);
    Assert.AreEqual(MessageType.ReassignTargetRequest, message.Type);
    Assert.AreSame(message.PayloadData, message.Payload);
    Assert.AreSame(target, message.PayloadData.Target);
  }

  [Test]
  public void AssignSubInterceptorRequestMessage_StoresExpectedMetadataAndPayload() {
    IadsCommsAgent sender = CreateCommsAgent("Sender");
    IadsCommsAgent receiver = CreateCommsAgent("Receiver");
    TestMailboxInterceptor subInterceptor = CreateMailboxInterceptor("SubInterceptor");
    AssignSubInterceptorRequestMessage message = new(sender, receiver, subInterceptor);

    Assert.AreSame(sender, message.Sender);
    Assert.AreSame(receiver, message.Receiver);
    Assert.AreEqual(MessageType.AssignSubInterceptorRequest, message.Type);
    Assert.AreSame(message.PayloadData, message.Payload);
    Assert.AreSame(subInterceptor, message.PayloadData.SubInterceptor);
  }

  private Mailbox CreateMailbox(float baseLatency, float jitterStdSeconds = 0f) {
    GameObject mailboxObject = new("Mailbox");
    _createdObjects.Add(mailboxObject);

    Mailbox mailbox = mailboxObject.AddComponent<Mailbox>();
    InvokePrivateMethod(mailbox, "Awake");
    SetPrivateField(mailbox, "_latencyTable", new LatencyTable(baseLatency));
    SetPrivateField(mailbox, "_latencyJitterStdSeconds", jitterStdSeconds);
    SetPrivateField(mailbox, "_messageQueue", new PriorityQueue<PendingMessage>());
    return mailbox;
  }

  private PendingMessage PeekPendingMessage(Mailbox mailbox) {
    PriorityQueue<PendingMessage> queue = GetPrivateField<PriorityQueue<PendingMessage>>(mailbox, "_messageQueue");
    return queue.Peek();
  }

  private IadsCommsAgent CreateCommsAgent(string name) {
    GameObject agentObject = new(name);
    _createdObjects.Add(agentObject);
    return agentObject.AddComponent<IadsCommsAgent>();
  }

  private TestMailboxAgent CreateMailboxAgent(string name) {
    GameObject agentObject = new(name);
    _createdObjects.Add(agentObject);
    agentObject.AddComponent<Rigidbody>();
    TestMailboxAgent agent = agentObject.AddComponent<TestMailboxAgent>();
    InvokePrivateMethod(agent, "Awake");
    return agent;
  }

  private TestMailboxInterceptor CreateMailboxInterceptor(string name) {
    GameObject interceptorObject = new(name);
    _createdObjects.Add(interceptorObject);
    interceptorObject.AddComponent<Rigidbody>();
    return interceptorObject.AddComponent<TestMailboxInterceptor>();
  }

  private static float ComputeExpectedLatency(float baseLatency, float jitterStdSeconds, int randomSeed) {
    Random.InitState(randomSeed);
    float u1 = Mathf.Max(float.Epsilon, Random.value);
    float u2 = Random.value;
    float standardNormal = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
    return Mathf.Max(0f, baseLatency + jitterStdSeconds * standardNormal);
  }

  private sealed class TestMailboxAgent : AgentBase {
    public int ReceivedCount { get; private set; }
    public Message LastMessage { get; private set; }

    protected override void OnMessage(Message message) {
      ++ReceivedCount;
      LastMessage = message;
    }
  }

  private sealed class TestMailboxInterceptor : InterceptorBase {}
}
