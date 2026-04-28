using NUnit.Framework;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;

public class AgentBaseTests : TestBase {
  private const float _epsilon = 1e-3f;

  private MailboxAwareAgentBase _agent;
  private Mailbox _mailbox;
  private SimManager _simManager;
  private FixedHierarchical _target;

  [SetUp]
  public void SetUp() {
    SetMailboxInstance(null);
    _simManager = CreateSimManagerStub();
    SetSimManagerInstance(_simManager);
    SetElapsedTime(0f);
    _mailbox = new GameObject("Mailbox").AddComponent<Mailbox>();

    _agent = new GameObject("Agent").AddComponent<MailboxAwareAgentBase>();
    _agent.gameObject.AddComponent<Rigidbody>();
    _agent.InvokeAwakeForTest();
    _agent.Transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, 1));
  }

  [TearDown]
  public void TearDown() {
    if (_agent != null) {
      UnityEngine.Object.DestroyImmediate(_agent.gameObject);
    }
    if (_mailbox != null) {
      UnityEngine.Object.DestroyImmediate(_mailbox.gameObject);
    }
    SetMailboxInstance(null);
    SetSimManagerInstance(null);
  }

  [Test]
  public void GetRelativeTransformation_TargetAtBoresightMovingBackwards() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 0, 0);
    _target =
        new FixedHierarchical(position: new Vector3(0, 0, 20), velocity: new Vector3(0, 20, -1));

    Transformation relativeTransformation = _agent.GetRelativeTransformation(_target);
    Assert.AreEqual(_target.Position - _agent.Position, relativeTransformation.Position.Cartesian);
    Assert.AreEqual(20f, relativeTransformation.Position.Range, _epsilon);
    Assert.AreEqual(0f, relativeTransformation.Position.Azimuth, _epsilon);
    Assert.AreEqual(0f, relativeTransformation.Position.Elevation, _epsilon);
    Assert.AreEqual(_target.Velocity - _agent.Velocity, relativeTransformation.Velocity.Cartesian);
    Assert.AreEqual(-1f, relativeTransformation.Velocity.Range, _epsilon);
    Assert.AreEqual(0f, relativeTransformation.Velocity.Azimuth, _epsilon);
    Assert.AreEqual(1f, relativeTransformation.Velocity.Elevation, _epsilon);
  }

  [Test]
  public void GetRelativeTransformation_TargetAtStarboardMovingForwards() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 0, 0);
    _target =
        new FixedHierarchical(position: new Vector3(20, 0, 0), velocity: new Vector3(0, 0, 20));

    Transformation relativeTransformation = _agent.GetRelativeTransformation(_target);
    Assert.AreEqual(_target.Position - _agent.Position, relativeTransformation.Position.Cartesian);
    Assert.AreEqual(20f, relativeTransformation.Position.Range, _epsilon);
    Assert.AreEqual(Mathf.PI / 2, relativeTransformation.Position.Azimuth, _epsilon);
    Assert.AreEqual(0f, relativeTransformation.Position.Elevation, _epsilon);
    Assert.AreEqual(_target.Velocity - _agent.Velocity, relativeTransformation.Velocity.Cartesian);
    Assert.AreEqual(0f, relativeTransformation.Velocity.Range, _epsilon);
    Assert.AreEqual(-1f, relativeTransformation.Velocity.Azimuth, _epsilon);
    Assert.AreEqual(0f, relativeTransformation.Velocity.Elevation, _epsilon);
  }

  [Test]
  public void GetRelativeTransformation_TargetAtStarboardMovingUpwards() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 0, 0);
    _target =
        new FixedHierarchical(position: new Vector3(20, 0, 0), velocity: new Vector3(0, 20, 0));

    Transformation relativeTransformation = _agent.GetRelativeTransformation(_target);
    Assert.AreEqual(_target.Position - _agent.Position, relativeTransformation.Position.Cartesian);
    Assert.AreEqual(20f, relativeTransformation.Position.Range, _epsilon);
    Assert.AreEqual(Mathf.PI / 2, relativeTransformation.Position.Azimuth, _epsilon);
    Assert.AreEqual(0f, relativeTransformation.Position.Elevation, _epsilon);
    Assert.AreEqual(_target.Velocity - _agent.Velocity, relativeTransformation.Velocity.Cartesian);
    Assert.AreEqual(0f, relativeTransformation.Velocity.Range, _epsilon);
    Assert.AreEqual(0f, relativeTransformation.Velocity.Azimuth, _epsilon);
    Assert.AreEqual(1f, relativeTransformation.Velocity.Elevation, _epsilon);
  }

  [Test]
  public void GetRelativeTransformation_TargetAboveMovingForwards() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 0, 0);
    _target =
        new FixedHierarchical(position: new Vector3(0, 20, 0), velocity: new Vector3(0, 0, 20));

    Transformation relativeTransformation = _agent.GetRelativeTransformation(_target);
    Assert.AreEqual(_target.Position - _agent.Position, relativeTransformation.Position.Cartesian);
    Assert.AreEqual(20f, relativeTransformation.Position.Range, _epsilon);
    Assert.AreEqual(0f, relativeTransformation.Position.Azimuth, _epsilon);
    Assert.AreEqual(Mathf.PI / 2, relativeTransformation.Position.Elevation, _epsilon);
    Assert.AreEqual(_target.Velocity - _agent.Velocity, relativeTransformation.Velocity.Cartesian);
    Assert.AreEqual(0f, relativeTransformation.Velocity.Range, _epsilon);
    Assert.AreEqual(0f, relativeTransformation.Velocity.Azimuth, _epsilon);
    Assert.AreEqual(-1f, relativeTransformation.Velocity.Elevation, _epsilon);
  }

  [Test]
  public void GetRelativeTransformation_TargetAboveMovingRight() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 0, 0);
    _target =
        new FixedHierarchical(position: new Vector3(0, 20, 0), velocity: new Vector3(20, 0, 0));

    Transformation relativeTransformation = _agent.GetRelativeTransformation(_target);
    Assert.AreEqual(_target.Position - _agent.Position, relativeTransformation.Position.Cartesian);
    Assert.AreEqual(20f, relativeTransformation.Position.Range, _epsilon);
    Assert.AreEqual(0f, relativeTransformation.Position.Azimuth, _epsilon);
    Assert.AreEqual(Mathf.PI / 2, relativeTransformation.Position.Elevation, _epsilon);
    Assert.AreEqual(_target.Velocity - _agent.Velocity, relativeTransformation.Velocity.Cartesian);
    Assert.AreEqual(0f, relativeTransformation.Velocity.Range, _epsilon);
    Assert.AreEqual(0f, relativeTransformation.Velocity.Azimuth, _epsilon);
    Assert.AreEqual(-1f, relativeTransformation.Velocity.Elevation, _epsilon);
  }

  [Test]
  public void GetRelativeTransformation_TargetAboveMovingForwardsAndRight() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 0, 0);
    _target =
        new FixedHierarchical(position: new Vector3(0, 20, 0), velocity: new Vector3(20, 0, 20));

    Transformation relativeTransformation = _agent.GetRelativeTransformation(_target);
    Assert.AreEqual(_target.Position - _agent.Position, relativeTransformation.Position.Cartesian);
    Assert.AreEqual(20f, relativeTransformation.Position.Range, _epsilon);
    Assert.AreEqual(0f, relativeTransformation.Position.Azimuth, _epsilon);
    Assert.AreEqual(Mathf.PI / 2, relativeTransformation.Position.Elevation, _epsilon);
    Assert.AreEqual(_target.Velocity - _agent.Velocity, relativeTransformation.Velocity.Cartesian);
    Assert.AreEqual(0f, relativeTransformation.Velocity.Range, _epsilon);
    Assert.AreEqual(0f, relativeTransformation.Velocity.Azimuth, _epsilon);
    Assert.AreEqual(-Mathf.Sqrt(2), relativeTransformation.Velocity.Elevation, _epsilon);
  }

  [Test]
  public void GetRelativeTransformation_TargetBelowMovingForwards() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 0, 0);
    _target =
        new FixedHierarchical(position: new Vector3(0, -20, 0), velocity: new Vector3(0, 0, 20));

    Transformation relativeTransformation = _agent.GetRelativeTransformation(_target);
    Assert.AreEqual(_target.Position - _agent.Position, relativeTransformation.Position.Cartesian);
    Assert.AreEqual(20f, relativeTransformation.Position.Range, _epsilon);
    Assert.AreEqual(0f, relativeTransformation.Position.Azimuth, _epsilon);
    Assert.AreEqual(-Mathf.PI / 2, relativeTransformation.Position.Elevation, _epsilon);
    Assert.AreEqual(_target.Velocity - _agent.Velocity, relativeTransformation.Velocity.Cartesian);
    Assert.AreEqual(0f, relativeTransformation.Velocity.Range, _epsilon);
    Assert.AreEqual(0f, relativeTransformation.Velocity.Azimuth, _epsilon);
    Assert.AreEqual(1f, relativeTransformation.Velocity.Elevation, _epsilon);
  }

  [Test]
  public void GetRelativeTransformation_TargetBelowMovingRight() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 0, 0);
    _target =
        new FixedHierarchical(position: new Vector3(0, -20, 0), velocity: new Vector3(20, 0, 0));

    Transformation relativeTransformation = _agent.GetRelativeTransformation(_target);
    Assert.AreEqual(_target.Position - _agent.Position, relativeTransformation.Position.Cartesian);
    Assert.AreEqual(20f, relativeTransformation.Position.Range, _epsilon);
    Assert.AreEqual(0f, relativeTransformation.Position.Azimuth, _epsilon);
    Assert.AreEqual(-Mathf.PI / 2, relativeTransformation.Position.Elevation, _epsilon);
    Assert.AreEqual(_target.Velocity - _agent.Velocity, relativeTransformation.Velocity.Cartesian);
    Assert.AreEqual(0f, relativeTransformation.Velocity.Range, _epsilon);
    Assert.AreEqual(0f, relativeTransformation.Velocity.Azimuth, _epsilon);
    Assert.AreEqual(1f, relativeTransformation.Velocity.Elevation, _epsilon);
  }

  [Test]
  public void GetRelativeTransformation_TargetBelowMovingForwardsAndRight() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 0, 0);
    _target =
        new FixedHierarchical(position: new Vector3(0, -20, 0), velocity: new Vector3(20, 0, 20));

    Transformation relativeTransformation = _agent.GetRelativeTransformation(_target);
    Assert.AreEqual(_target.Position - _agent.Position, relativeTransformation.Position.Cartesian);
    Assert.AreEqual(20f, relativeTransformation.Position.Range, _epsilon);
    Assert.AreEqual(0f, relativeTransformation.Position.Azimuth, _epsilon);
    Assert.AreEqual(-Mathf.PI / 2, relativeTransformation.Position.Elevation, _epsilon);
    Assert.AreEqual(_target.Velocity - _agent.Velocity, relativeTransformation.Velocity.Cartesian);
    Assert.AreEqual(0f, relativeTransformation.Velocity.Range, _epsilon);
    Assert.AreEqual(0f, relativeTransformation.Velocity.Azimuth, _epsilon);
    Assert.AreEqual(Mathf.Sqrt(2), relativeTransformation.Velocity.Elevation, _epsilon);
  }

  // Verifies that AgentBase only registers once even though both Awake() and Start() call the
  // mailbox registration path.
  [Test]
  public void MailboxDelivery_AfterAwakeAndStart_InvokesOnMessageOnlyOnce() {
    var sender = new StubAgent(Configs.AgentType.Vessel);
    var message = new TestMessage(sender, _agent);

    _agent.InvokeStartForTest();
    _mailbox.Configure(null);
    _mailbox.Send(message);

    InvokePrivateMethod(_mailbox, "Update");

    Assert.AreEqual(1, _agent.ReceivedMessageCount);
    Assert.AreSame(message, _agent.LastReceivedMessage);
  }

  // Verifies that AgentBase ignores mailbox deliveries for other receivers and still handles
  // deliveries addressed to itself.
  [Test]
  public void MailboxDelivery_ForDifferentReceiver_DoesNotInvokeOnMessage() {
    var sender = new StubAgent(Configs.AgentType.Vessel);
    var otherReceiver = new StubAgent(Configs.AgentType.CarrierInterceptor);
    var wrongMessage = new TestMessage(sender, otherReceiver);
    var rightMessage = new TestMessage(sender, _agent);

    _agent.InvokeStartForTest();
    _mailbox.Configure(null);

    _mailbox.Send(wrongMessage);
    InvokePrivateMethod(_mailbox, "Update");
    Assert.AreEqual(0, _agent.ReceivedMessageCount);

    _mailbox.Send(rightMessage);
    InvokePrivateMethod(_mailbox, "Update");
    Assert.AreEqual(1, _agent.ReceivedMessageCount);
    Assert.AreSame(rightMessage, _agent.LastReceivedMessage);
  }

  // Verifies that AgentBase unsubscribes from the mailbox during OnDestroy() so later deliveries
  // no longer reach OnMessage().
  [Test]
  public void OnDestroy_UnsubscribesFromMailboxDeliveries() {
    var sender = new StubAgent(Configs.AgentType.Vessel);
    var firstMessage = new TestMessage(sender, _agent);
    var secondMessage = new TestMessage(sender, _agent);

    _agent.InvokeStartForTest();
    _mailbox.Configure(null);

    _mailbox.Send(firstMessage);
    InvokePrivateMethod(_mailbox, "Update");
    Assert.AreEqual(1, _agent.ReceivedMessageCount);

    SetMailboxInstance(null);
    _agent.InvokeOnDestroyForTest();
    _mailbox.Send(secondMessage);
    InvokePrivateMethod(_mailbox, "Update");

    Assert.AreEqual(1, _agent.ReceivedMessageCount);
    Assert.AreSame(firstMessage, _agent.LastReceivedMessage);
    Assert.IsFalse(GetPrivateField<bool>(_agent, "_mailboxRegistered"));
    Assert.IsNull(GetPrivateField<Mailbox>(_agent, "_mailboxInstance"));
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

  private sealed class MailboxAwareAgentBase : AgentBase {
    public int ReceivedMessageCount { get; private set; }
    public Message LastReceivedMessage { get; private set; }

    public void InvokeAwakeForTest() {
      base.Awake();
    }

    public void InvokeStartForTest() {
      base.Start();
    }

    public void InvokeOnDestroyForTest() {
      base.OnDestroy();
    }

    protected override void OnMessage(Message message) {
      ++ReceivedMessageCount;
      LastReceivedMessage = message;
    }
  }

  private sealed class TestPayload : IMessagePayload {}

  private sealed class TestMessage : Message {
    private readonly TestPayload _payload = new TestPayload();

    public override IMessagePayload Payload => _payload;

    public TestMessage(IAgent sender, IAgent receiver)
        : base(sender, receiver, MessageType.AssignTarget) {}
  }

  private sealed class StubAgent : IAgent {
    public event AgentTerminatedEventHandler OnTerminated;

    public HierarchicalAgent HierarchicalAgent { get; set; }
    public Configs.StaticConfig StaticConfig { get; set; }
    public Configs.AgentConfig AgentConfig { get; set; }
    public IMovement Movement { get; set; }
    public IController Controller { get; set; }
    public ISensor Sensor { get; set; }
    public IAgent TargetModel { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    public float Speed => Velocity.magnitude;
    public Vector3 Acceleration { get; set; }
    public Vector3 AccelerationInput { get; set; }
    public bool IsPursuer => false;
    public float ElapsedTime => 0f;
    public bool IsTerminated { get; private set; }
    public GameObject gameObject => null;
    public Transform Transform => null;
    public Vector3 Up => Vector3.up;
    public Vector3 Forward => Vector3.forward;
    public Vector3 Right => Vector3.right;
    public Quaternion InverseRotation => Quaternion.identity;

    public StubAgent(Configs.AgentType agentType) {
      StaticConfig = new Configs.StaticConfig { AgentType = agentType };
    }

    public float MaxForwardAcceleration() => 0f;
    public float MaxNormalAcceleration() => 0f;
    public void CreateTargetModel(IHierarchical target) {}
    public void DestroyTargetModel() {}
    public void UpdateTargetModel() {}

    public void Terminate() {
      IsTerminated = true;
      OnTerminated?.Invoke(this);
    }

    public Transformation GetRelativeTransformation(IAgent target) => default;
    public Transformation GetRelativeTransformation(IHierarchical target) => default;
    public Transformation GetRelativeTransformation(in Vector3 waypoint) => default;
  }
}
