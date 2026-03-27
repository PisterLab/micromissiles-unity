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

  // Verifies that a zero-latency message is delivered on the next mailbox update.
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

  // Verifies that sending a null message is a no-op and does not enqueue anything.
  [Test]
  public void Send_NullMessage_DoesNotEnqueue() {
    Mailbox mailbox = CreateMailbox(baseLatency: 0f);
    mailbox.Send(null);
    Assert.IsTrue(GetMessageQueue(mailbox).IsEmpty());
  }

  // Verifies that a non-zero latency message is scheduled for the future and not delivered early.
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

  // Verifies that latency jitter changes the scheduled delivery time deterministically for a fixed random seed.
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

  // Verifies that all due messages are delivered in priority order when the mailbox updates.
  [Test]
  public void DeliverDueMessages_MultipleMessages_DeliverInScheduledOrder() {
    Mailbox mailbox = CreateMailbox(baseLatency: 0f);
    IadsCommsAgent sender = CreateCommsAgent("Sender");
    IadsCommsAgent receiver = CreateCommsAgent("Receiver");
    AssignTargetMessage firstMessage = new(sender, receiver, new FixedHierarchical());
    ReassignTargetRequestMessage secondMessage = new(sender, receiver, new FixedHierarchical());
    float currentTime = Time.time;
    List<Message> deliveredMessages = new();

    mailbox.OnMessageDelivered += (_, message) => { deliveredMessages.Add(message); };

    GetMessageQueue(mailbox).Enqueue(new PendingMessage(currentTime - 0.1f, secondMessage), currentTime - 0.1f);
    GetMessageQueue(mailbox).Enqueue(new PendingMessage(currentTime - 0.2f, firstMessage), currentTime - 0.2f);

    InvokePrivateMethod(mailbox, "Update");

    CollectionAssert.AreEqual(new Message[] { firstMessage, secondMessage }, deliveredMessages);
    Assert.IsTrue(GetMessageQueue(mailbox).IsEmpty());
  }

  [Test]
  public void DeliverDueMessages_ZeroLatencyFollowUpWaitsUntilNextUpdate() {
    Mailbox mailbox = CreateMailbox(baseLatency: 0f);
    IadsCommsAgent sender = CreateCommsAgent("Sender");
    IadsCommsAgent receiver = CreateCommsAgent("Receiver");
    AssignTargetMessage firstMessage = new(sender, receiver, new FixedHierarchical());
    ReassignTargetRequestMessage secondMessage = new(sender, receiver, new FixedHierarchical());
    List<Message> deliveredMessages = new();
    bool sentFollowUp = false;

    mailbox.OnMessageDelivered += (_, message) => {
      deliveredMessages.Add(message);
      if (!sentFollowUp) {
        sentFollowUp = true;
        mailbox.Send(secondMessage);
      }
    };
    mailbox.Send(firstMessage);
    InvokePrivateMethod(mailbox, "Update");

    CollectionAssert.AreEqual(new Message[] { firstMessage }, deliveredMessages);
    Assert.IsFalse(GetMessageQueue(mailbox).IsEmpty());

    InvokePrivateMethod(mailbox, "Update");

    CollectionAssert.AreEqual(new Message[] { firstMessage, secondMessage }, deliveredMessages);
    Assert.IsTrue(GetMessageQueue(mailbox).IsEmpty());
  }

  // Verifies that messages addressed to terminated receivers are dropped before delivery.
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

  // Verifies that messages addressed to destroyed Unity receivers are dropped before delivery.
  [Test]
  public void DeliverDueMessages_DestroyedReceiver_DoesNotDeliver() {
    Mailbox mailbox = CreateMailbox(baseLatency: 0f);
    IadsCommsAgent sender = CreateCommsAgent("Sender");
    IadsCommsAgent receiver = CreateCommsAgent("Receiver");
    int deliveredCount = 0;

    mailbox.OnMessageDelivered += (_, _) => { ++deliveredCount; };

    mailbox.Send(new AssignTargetMessage(sender, receiver, new FixedHierarchical()));
    Object.DestroyImmediate(receiver.gameObject);
    InvokePrivateMethod(mailbox, "Update");

    Assert.AreEqual(0, deliveredCount);
  }

  // Verifies that only the intended AgentBase receiver handles a delivered mailbox message.
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

  // Verifies that mailbox latency lookup changes based on the concrete sender/receiver comms roles.
  [Test]
  public void Send_UsesConcreteAgentRolesForLatencyLookup() {
    const float iadsToCarrierLatency = 0.75f;
    const float iadsToInterceptorLatency = 0.25f;

    Mailbox mailbox = CreateMailbox(baseLatency: 0f);
    LatencyTable latencyTable = new();
    latencyTable.Set(CommsNode.IADS, CommsNode.Carrier, iadsToCarrierLatency);
    latencyTable.Set(CommsNode.IADS, CommsNode.Interceptor, iadsToInterceptorLatency);
    SetPrivateField(mailbox, "_latencyTable", latencyTable);

    IadsCommsAgent sender = CreateCommsAgent("Sender");
    TestMailboxCarrier carrierReceiver = CreateMailboxCarrier("Carrier");
    TestMailboxAgent interceptorReceiver = CreateMailboxAgent("Interceptor");

    float carrierSendTime = Time.time;
    mailbox.Send(new AssignTargetMessage(sender, carrierReceiver, new FixedHierarchical()));
    float carrierLatency = PeekPendingMessage(mailbox).DeliverAt - carrierSendTime;

    GetMessageQueue(mailbox).Dequeue();

    float interceptorSendTime = Time.time;
    mailbox.Send(new AssignTargetMessage(sender, interceptorReceiver, new FixedHierarchical()));
    float interceptorLatency = PeekPendingMessage(mailbox).DeliverAt - interceptorSendTime;

    Assert.That(carrierLatency, Is.EqualTo(iadsToCarrierLatency).Within(_epsilon));
    Assert.That(interceptorLatency, Is.EqualTo(iadsToInterceptorLatency).Within(_epsilon));
  }

  // Verifies that editor validation clamps negative latency configuration values to zero.
  [Test]
  public void OnValidate_ClampsNegativeLatencyInputsToZero() {
    Mailbox mailbox = CreateMailbox(baseLatency: 0f);

    SetPrivateField(mailbox, "_latencyJitterStdSeconds", -1f);
    SetPrivateField(mailbox, "_uniformLatency", -2f);

    InvokePrivateMethod(mailbox, "OnValidate");

    Assert.AreEqual(0f, GetPrivateField<float>(mailbox, "_latencyJitterStdSeconds"));
    Assert.AreEqual(0f, GetPrivateField<float>(mailbox, "_uniformLatency"));
  }

  [Test]
  public void ClearPendingMessages_RemovesQueuedMessages() {
    Mailbox mailbox = CreateMailbox(baseLatency: 0f);
    IadsCommsAgent sender = CreateCommsAgent("Sender");
    IadsCommsAgent receiver = CreateCommsAgent("Receiver");

    mailbox.Send(new AssignTargetMessage(sender, receiver, new FixedHierarchical()));
    Assert.IsFalse(GetMessageQueue(mailbox).IsEmpty());

    mailbox.ClearPendingMessages();

    Assert.IsTrue(GetMessageQueue(mailbox).IsEmpty());
  }

  // Verifies that AssignTargetMessage stores the expected sender, receiver, type, and payload.
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

  // Verifies that ReassignTargetRequestMessage stores the expected sender, receiver, type, and payload.
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

  // Verifies that AssignSubInterceptorRequestMessage stores the expected sender, receiver, type, and payload.
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
    return GetMessageQueue(mailbox).Peek();
  }

  private PriorityQueue<PendingMessage> GetMessageQueue(Mailbox mailbox) {
    return GetPrivateField<PriorityQueue<PendingMessage>>(mailbox, "_messageQueue");
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

  private TestMailboxCarrier CreateMailboxCarrier(string name) {
    GameObject carrierObject = new(name);
    _createdObjects.Add(carrierObject);
    carrierObject.AddComponent<Rigidbody>();
    return carrierObject.AddComponent<TestMailboxCarrier>();
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

  private sealed class TestMailboxCarrier : CarrierBase {}
}
