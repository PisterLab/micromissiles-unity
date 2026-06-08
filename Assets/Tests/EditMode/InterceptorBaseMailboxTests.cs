using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;

public class InterceptorBaseMailboxTests : TestBase {
  private Mailbox _mailbox;
  private SimManager _simManager;
  private TestInterceptor _interceptor;

  [SetUp]
  public void SetUp() {
    SetMailboxInstance(null);
    SetSimManagerInstance(null);

    _simManager = CreateSimManagerStub();
    SetPrivateField(_simManager, "_dummyAgents", new List<IAgent>());
    SetSimManagerInstance(_simManager);
    SetElapsedTime(0f);

    _mailbox = new GameObject("Mailbox").AddComponent<Mailbox>();

    _interceptor = new GameObject("Interceptor").AddComponent<TestInterceptor>();
    _interceptor.gameObject.AddComponent<Rigidbody>();
    _interceptor.HierarchicalAgent = new HierarchicalAgent(_interceptor);
    _interceptor.InvokeAwakeForTest();
    _interceptor.StaticConfig = CreateStaticConfig(Configs.AgentType.CarrierInterceptor);
  }

  [TearDown]
  public void TearDown() {
    if (_interceptor?.TargetModel != null) {
      _interceptor.DestroyTargetModel();
    }
    if (_interceptor != null) {
      Object.DestroyImmediate(_interceptor.gameObject);
    }
    if (_mailbox != null) {
      Object.DestroyImmediate(_mailbox.gameObject);
    }
    SetMailboxInstance(null);
    SetSimManagerInstance(null);
  }

  // Verifies that a mailbox-delivered AssignTargetResponse message updates the interceptor target.
  [Test]
  public void MailboxDelivery_AssignTargetResponseMessage_AssignsHierarchicalTarget() {
    var target = new FixedHierarchical(position: new Vector3(10f, 0f, 0f));
    var message = new AssignTargetResponseMessage(new StubAgent(Configs.AgentType.Vessel),
                                                  _interceptor, target);

    _mailbox.Configure(null);
    _mailbox.Send(message);

    InvokePrivateMethod(_mailbox, "Update");

    Assert.AreSame(target, _interceptor.HierarchicalAgent.Target);
  }

  // Verifies that a mailbox-delivered reassignment request is queued for later clustering.
  [Test]
  public void MailboxDelivery_ReassignTargetRequest_QueuesUnassignedTarget() {
    SetPrivateField(_interceptor, "_capacityPerSubInterceptor", 1);
    SetPrivateField(_interceptor, "_numSubInterceptorsPlannedRemaining", 1);

    var target = new FixedHierarchical(position: new Vector3(5f, 0f, 0f));
    var message = new ReassignTargetRequestMessage(new StubAgent(Configs.AgentType.Vessel),
                                                   _interceptor, target);

    _mailbox.Configure(null);
    _mailbox.Send(message);

    InvokePrivateMethod(_mailbox, "Update");

    var queuedTargets = GetPrivateField<HashSet<IHierarchical>>(_interceptor, "_unassignedTargets");
    Assert.True(queuedTargets.Contains(target));
  }

  // Verifies that a mailbox-delivered AssignTargetRequest message to a parent interceptor produces
  // an AssignTargetResponse message for the requesting sub-interceptor.
  [Test]
  public void MailboxDelivery_AssignTargetRequest_SendsAssignTargetResponseToSubInterceptor() {
    SetPrivateField(_interceptor, "_capacityPerSubInterceptor", 1);
    var expectedTarget = new FixedHierarchical(position: new Vector3(12f, 0f, 0f));
    _interceptor.HierarchicalAgent.Target = CreateCluster(expectedTarget);

    var subInterceptor = new StubInterceptor(Configs.AgentType.MissileInterceptor, capacity: 1);
    subInterceptor.HierarchicalAgent = new HierarchicalAgent(subInterceptor);

    AssignTargetResponseMessage deliveredMessage = null;
    _mailbox.OnMessageDelivered += (_, message) => {
      if (message is AssignTargetResponseMessage assignTargetResponse &&
          ReferenceEquals(assignTargetResponse.Receiver, subInterceptor)) {
        deliveredMessage = assignTargetResponse;
      }
    };

    _mailbox.Configure(null);
    _mailbox.Send(new AssignTargetRequestMessage(new StubAgent(Configs.AgentType.Vessel),
                                                 _interceptor, subInterceptor));

    InvokePrivateMethod(_mailbox, "Update");
    Assert.IsNull(deliveredMessage);

    InvokePrivateMethod(_mailbox, "Update");
    Assert.NotNull(deliveredMessage);
    Assert.AreSame(_interceptor, deliveredMessage.Sender);
    Assert.AreSame(subInterceptor, deliveredMessage.Receiver);
    Assert.AreSame(expectedTarget, deliveredMessage.PayloadData.Target);
  }

  // Verifies that a sub-interceptor assignment request is sent through the mailbox to the parent
  // receiver when no target is currently available.
  [Test]
  public void AssignSubInterceptor_WithoutTarget_SendsAssignTargetRequestToParentMailboxReceiver() {
    var parent = new StubAgent(Configs.AgentType.Vessel);
    _interceptor.CommsParent = parent;

    var subInterceptor = new StubInterceptor(Configs.AgentType.MissileInterceptor, capacity: 1);
    subInterceptor.HierarchicalAgent = new HierarchicalAgent(subInterceptor);

    AssignTargetRequestMessage deliveredMessage = null;
    _mailbox.OnMessageDelivered += (_, message) => {
      if (message is AssignTargetRequestMessage assignRequest &&
          ReferenceEquals(assignRequest.Receiver, parent)) {
        deliveredMessage = assignRequest;
      }
    };

    _mailbox.Configure(null);
    _interceptor.AssignSubInterceptor(subInterceptor);

    InvokePrivateMethod(_mailbox, "Update");

    Assert.NotNull(deliveredMessage);
    Assert.AreSame(_interceptor, deliveredMessage.Sender);
    Assert.AreSame(parent, deliveredMessage.Receiver);
    Assert.AreSame(subInterceptor, deliveredMessage.PayloadData.SubInterceptor);
  }

  // Verifies that reassignment propagation uses the mailbox when the interceptor has no remaining
  // planned capacity.
  [Test]
  public void ReassignTarget_WithoutPlannedCapacity_SendsRequestToParentMailboxReceiver() {
    SetPrivateField(_interceptor, "_capacityPerSubInterceptor", 1);
    SetPrivateField(_interceptor, "_numSubInterceptorsPlannedRemaining", 0);
    _interceptor.CommsParent = new StubAgent(Configs.AgentType.ShoreBattery);

    var target = new FixedHierarchical(position: new Vector3(3f, 0f, 0f));
    ReassignTargetRequestMessage deliveredMessage = null;
    _mailbox.OnMessageDelivered += (_, message) => {
      if (message is ReassignTargetRequestMessage reassignRequest &&
          ReferenceEquals(reassignRequest.Receiver, _interceptor.CommsParent)) {
        deliveredMessage = reassignRequest;
      }
    };

    _mailbox.Configure(null);
    _interceptor.ReassignTarget(target);

    InvokePrivateMethod(_mailbox, "Update");

    Assert.NotNull(deliveredMessage);
    Assert.AreSame(_interceptor, deliveredMessage.Sender);
    Assert.AreSame(_interceptor.CommsParent, deliveredMessage.Receiver);
    Assert.AreSame(target, deliveredMessage.PayloadData.Target);
  }

  private static SimManager CreateSimManagerStub() {
    return (SimManager)FormatterServices.GetUninitializedObject(typeof(SimManager));
  }

  private static Configs.StaticConfig CreateStaticConfig(Configs.AgentType agentType) {
    return new Configs.StaticConfig {
      AgentType = agentType,
      BodyConfig = new Configs.BodyConfig { Mass = 1f, CrossSectionalArea = 1f },
      LiftDragConfig = new Configs.LiftDragConfig { DragCoefficient = 1f, LiftDragRatio = 1f },
    };
  }

  private static HierarchicalBase CreateCluster(params IHierarchical[] targets) {
    var cluster = new HierarchicalBase();
    foreach (IHierarchical target in targets) {
      cluster.AddSubHierarchical(target);
    }
    return cluster;
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

  private sealed class TestInterceptor : InterceptorBase, IAgent {
    public void InvokeAwakeForTest() {
      base.Awake();
    }

    void IAgent.CreateTargetModel(IHierarchical target) {}

    void IAgent.DestroyTargetModel() {}

    void IAgent.UpdateTargetModel() {}
  }

  private sealed class StubInterceptor : IInterceptor {
    private readonly int _capacity;

    public event InterceptorEventHandler OnHit;
    public event InterceptorEventHandler OnMiss;
    public event InterceptorEventHandler OnDestroyed;
    public event InterceptorEventHandler OnAssignSubInterceptor;
    public event TargetReassignEventHandler OnReassignTarget;
    public event AgentTerminatedEventHandler OnTerminated;

    public HierarchicalAgent HierarchicalAgent { get; set; }
    public Configs.StaticConfig StaticConfig { get; set; }
    public Configs.AgentConfig AgentConfig { get; set; }
    public IMovement Movement { get; set; }
    public IController Controller { get; set; }
    public ISensor Sensor { get; set; }
    public IAgent TargetModel { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; } = Vector3.forward;
    public float Speed => Velocity.magnitude;
    public Vector3 Acceleration { get; set; }
    public Vector3 AccelerationInput { get; set; }
    public bool IsPursuer => true;
    public float ElapsedTime => 0f;
    public bool IsTerminated { get; private set; }
    public GameObject gameObject => null;
    public Transform Transform => null;
    public Vector3 Up => Vector3.up;
    public Vector3 Forward => Vector3.forward;
    public Vector3 Right => Vector3.right;
    public Quaternion InverseRotation => Quaternion.identity;
    public IEscapeDetector EscapeDetector { get; set; }
    public int Capacity => _capacity;
    public int CapacityPerSubInterceptor => _capacity;
    public int CapacityPlannedRemaining => _capacity;
    public int CapacityRemaining => _capacity;
    public int NumSubInterceptors => 0;
    public int NumSubInterceptorsPlannedRemaining => 0;
    public int NumSubInterceptorsRemaining => 0;
    public bool IsReassignable => true;

    public StubInterceptor(Configs.AgentType agentType, int capacity) {
      _capacity = capacity;
      StaticConfig = CreateStaticConfig(agentType);
    }

    public float MaxForwardAcceleration() => 1f;
    public float MaxNormalAcceleration() => 1f;
    public void CreateTargetModel(IHierarchical target) {}
    public void DestroyTargetModel() {}
    public void UpdateTargetModel() {}
    public bool EvaluateReassignedTarget(IHierarchical target) => false;
    public void AssignSubInterceptor(IInterceptor subInterceptor) {
      OnAssignSubInterceptor?.Invoke(subInterceptor);
    }
    public void ReassignTarget(IHierarchical target) {
      OnReassignTarget?.Invoke(target);
    }

    public void Terminate() {
      IsTerminated = true;
      OnTerminated?.Invoke(this);
    }

    public Transformation GetRelativeTransformation(IAgent target) => default;
    public Transformation GetRelativeTransformation(IHierarchical target) => default;
    public Transformation GetRelativeTransformation(in Vector3 waypoint) => default;
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
      StaticConfig = CreateStaticConfig(agentType);
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
