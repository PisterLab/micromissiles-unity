using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;

public class IADSMailboxTests : TestBase {
  private Mailbox _mailbox;
  private SimManager _simManager;
  private IADS _iads;
  private IadsCommsAgent _commsAgent;

  [SetUp]
  public void SetUp() {
    SetMailboxInstance(null);
    SetSimManagerInstance(null);
    SetIadsInstance(null);

    _simManager = CreateSimManagerStub();
    SetPrivateField(_simManager, "_interceptors", new List<IAgent>());
    SetPrivateField(_simManager, "_threats", new List<IAgent>());
    SetSimManagerInstance(_simManager);

    _mailbox = new GameObject("Mailbox").AddComponent<Mailbox>();

    _iads = new GameObject("IADS").AddComponent<IADS>();
    InvokePrivateMethod(_iads, "Awake");
    _commsAgent = _iads.GetComponent<IadsCommsAgent>();
    InvokePrivateMethod(_iads, "Start");
  }

  [TearDown]
  public void TearDown() {
    if (_iads != null) {
      InvokePrivateMethod(_iads, "OnDestroy");
      Object.DestroyImmediate(_iads.gameObject);
    }
    if (_mailbox != null) {
      Object.DestroyImmediate(_mailbox.gameObject);
    }
    SetMailboxInstance(null);
    SetSimManagerInstance(null);
    SetIadsInstance(null);
  }

  // Verifies that an AssignTargetRequest mailbox request to the IADS proxy produces an outgoing
  // AssignTargetResponse mailbox message for the requested sub-interceptor.
  [Test]
  public void MailboxDelivery_AssignTargetRequest_SendsAssignTargetResponseMessage() {
    var launcher =
        new StubInterceptor(agentType: Configs.AgentType.Vessel, position: Vector3.zero,
                            capacity: 1, capacityPerSubInterceptor: 1, capacityPlannedRemaining: 1,
                            capacityRemaining: 1, isPursuer: false);
    launcher.HierarchicalAgent = new HierarchicalAgent(launcher);
    var expectedLeafTarget = new FixedHierarchical(position: new Vector3(20f, 0f, 0f));
    launcher.HierarchicalAgent.Target = CreateCluster(expectedLeafTarget);
    _iads.RegisterNewLauncher(launcher);

    var subInterceptor = new StubInterceptor(
        agentType: Configs.AgentType.MissileInterceptor, position: new Vector3(1f, 0f, 0f),
        capacity: 1, capacityPerSubInterceptor: 1, capacityPlannedRemaining: 1,
        capacityRemaining: 1, isPursuer: true);
    subInterceptor.HierarchicalAgent = new HierarchicalAgent(subInterceptor);

    AssignTargetResponseMessage deliveredMessage = null;
    _mailbox.OnMessageDelivered += (_, message) => {
      if (message is AssignTargetResponseMessage assignTargetMessage &&
          ReferenceEquals(assignTargetMessage.Receiver, subInterceptor)) {
        deliveredMessage = assignTargetMessage;
      }
    };

    _mailbox.Configure(null);
    _mailbox.Send(new AssignTargetRequestMessage(new StubAgent(Configs.AgentType.Vessel),
                                                 _commsAgent, subInterceptor));

    InvokePrivateMethod(_mailbox, "Update");
    Assert.IsNull(deliveredMessage);

    InvokePrivateMethod(_mailbox, "Update");
    Assert.NotNull(deliveredMessage);
    Assert.AreSame(_commsAgent, deliveredMessage.Sender);
    Assert.AreSame(subInterceptor, deliveredMessage.Receiver);
    Assert.AreEqual(MessageType.AssignTargetResponse, deliveredMessage.Type);

    List<IHierarchical> assignedTargets = deliveredMessage.PayloadData.Target.LeafHierarchicals(
        activeOnly: true, withTargetOnly: false);
    Assert.AreEqual(1, assignedTargets.Count);
    Assert.AreSame(expectedLeafTarget, assignedTargets[0]);
  }

  // Verifies that a target reassignment mailbox request to the IADS proxy chooses the closest
  // launcher that still has planned capacity remaining.
  [Test]
  public void MailboxDelivery_ReassignTargetRequest_SendsToClosestEligibleLauncher() {
    var closestLauncher =
        new StubInterceptor(agentType: Configs.AgentType.Vessel, position: new Vector3(5f, 0f, 0f),
                            capacity: 1, capacityPerSubInterceptor: 1, capacityPlannedRemaining: 1,
                            capacityRemaining: 1, isPursuer: false);
    closestLauncher.HierarchicalAgent = new HierarchicalAgent(closestLauncher);
    _iads.RegisterNewLauncher(closestLauncher);

    var fartherLauncher = new StubInterceptor(
        agentType: Configs.AgentType.ShoreBattery, position: new Vector3(50f, 0f, 0f), capacity: 1,
        capacityPerSubInterceptor: 1, capacityPlannedRemaining: 1, capacityRemaining: 1,
        isPursuer: false);
    fartherLauncher.HierarchicalAgent = new HierarchicalAgent(fartherLauncher);
    _iads.RegisterNewLauncher(fartherLauncher);

    var noCapacityLauncher = new StubInterceptor(
        agentType: Configs.AgentType.CarrierInterceptor, position: new Vector3(1f, 0f, 0f),
        capacity: 1, capacityPerSubInterceptor: 1, capacityPlannedRemaining: 0,
        capacityRemaining: 1, isPursuer: false);
    noCapacityLauncher.HierarchicalAgent = new HierarchicalAgent(noCapacityLauncher);
    _iads.RegisterNewLauncher(noCapacityLauncher);

    var target = new FixedHierarchical(position: Vector3.zero);
    ReassignTargetRequestMessage deliveredMessage = null;
    _mailbox.OnMessageDelivered += (_, message) => {
      if (message is ReassignTargetRequestMessage reassignTargetRequest &&
          !ReferenceEquals(reassignTargetRequest.Receiver, _commsAgent)) {
        deliveredMessage = reassignTargetRequest;
      }
    };

    _mailbox.Configure(null);
    _mailbox.Send(new ReassignTargetRequestMessage(new StubAgent(Configs.AgentType.Vessel),
                                                   _commsAgent, target));

    InvokePrivateMethod(_mailbox, "Update");
    Assert.IsNull(deliveredMessage);

    InvokePrivateMethod(_mailbox, "Update");
    Assert.NotNull(deliveredMessage);
    Assert.AreSame(_commsAgent, deliveredMessage.Sender);
    Assert.AreSame(closestLauncher, deliveredMessage.Receiver);
    Assert.AreSame(target, deliveredMessage.PayloadData.Target);
  }

  // Verifies that the IADS proxy ignores mailbox deliveries addressed to some other receiver.
  [Test]
  public void MailboxDelivery_ForDifferentReceiver_DoesNotTriggerIadsResponse() {
    var launcher =
        new StubInterceptor(agentType: Configs.AgentType.Vessel, position: Vector3.zero,
                            capacity: 1, capacityPerSubInterceptor: 1, capacityPlannedRemaining: 1,
                            capacityRemaining: 1, isPursuer: false);
    launcher.HierarchicalAgent = new HierarchicalAgent(launcher);
    launcher.HierarchicalAgent.Target =
        CreateCluster(new FixedHierarchical(position: Vector3.right));
    _iads.RegisterNewLauncher(launcher);

    var unrelatedReceiver = new StubAgent(Configs.AgentType.ShoreBattery);
    var subInterceptor =
        new StubInterceptor(agentType: Configs.AgentType.MissileInterceptor, position: Vector3.zero,
                            capacity: 1, capacityPerSubInterceptor: 1, capacityPlannedRemaining: 1,
                            capacityRemaining: 1, isPursuer: true);
    subInterceptor.HierarchicalAgent = new HierarchicalAgent(subInterceptor);

    AssignTargetResponseMessage deliveredResponse = null;
    _mailbox.OnMessageDelivered += (_, message) => {
      if (message is AssignTargetResponseMessage assignTargetMessage &&
          ReferenceEquals(assignTargetMessage.Receiver, subInterceptor)) {
        deliveredResponse = assignTargetMessage;
      }
    };

    _mailbox.Configure(null);
    _mailbox.Send(new AssignTargetRequestMessage(new StubAgent(Configs.AgentType.Vessel),
                                                 unrelatedReceiver, subInterceptor));

    InvokePrivateMethod(_mailbox, "Update");
    InvokePrivateMethod(_mailbox, "Update");

    Assert.IsNull(deliveredResponse);
  }

  // Verifies that top-level launchers route mailbox requests to the IADS proxy.
  [Test]
  public void RegisterNewLauncher_SetsCommsParentToIadsProxy() {
    var launcherObject = new GameObject("Launcher");
    try {
      var launcher = launcherObject.AddComponent<TestLauncherInterceptor>();
      launcherObject.AddComponent<Rigidbody>();
      launcher.HierarchicalAgent = new HierarchicalAgent(launcher);
      launcher.InvokeAwakeForTest();

      _iads.RegisterNewLauncher(launcher);

      Assert.AreSame(_commsAgent, launcher.CommsParent);
    } finally {
      Object.DestroyImmediate(launcherObject);
    }
  }

  private static SimManager CreateSimManagerStub() {
    return (SimManager)FormatterServices.GetUninitializedObject(typeof(SimManager));
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

  private static void SetIadsInstance(IADS iads) {
    FieldInfo instanceField = typeof(IADS).GetField("<Instance>k__BackingField",
                                                    BindingFlags.NonPublic | BindingFlags.Static);
    instanceField.SetValue(null, iads);
  }

  private sealed class StubInterceptor : IInterceptor {
    private readonly int _capacity;
    private readonly int _capacityPerSubInterceptor;
    private readonly int _capacityPlannedRemaining;
    private readonly int _capacityRemaining;
    private readonly bool _isPursuer;

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
    public bool IsPursuer => _isPursuer;
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
    public int CapacityPerSubInterceptor => _capacityPerSubInterceptor;
    public int CapacityPlannedRemaining => _capacityPlannedRemaining;
    public int CapacityRemaining => _capacityRemaining;
    public int NumSubInterceptors => 0;
    public int NumSubInterceptorsPlannedRemaining => 0;
    public int NumSubInterceptorsRemaining => 0;
    public bool IsReassignable => true;

    public StubInterceptor(Configs.AgentType agentType, Vector3 position, int capacity,
                           int capacityPerSubInterceptor, int capacityPlannedRemaining,
                           int capacityRemaining, bool isPursuer) {
      _capacity = capacity;
      _capacityPerSubInterceptor = capacityPerSubInterceptor;
      _capacityPlannedRemaining = capacityPlannedRemaining;
      _capacityRemaining = capacityRemaining;
      _isPursuer = isPursuer;
      Position = position;
      StaticConfig = new Configs.StaticConfig {
        AgentType = agentType,
        BodyConfig = new Configs.BodyConfig { Mass = 1f, CrossSectionalArea = 1f },
        LiftDragConfig = new Configs.LiftDragConfig { DragCoefficient = 1f, LiftDragRatio = 1f },
      };
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

  private sealed class TestLauncherInterceptor : InterceptorBase, IAgent {
    public void InvokeAwakeForTest() {
      base.Awake();
    }

    void IAgent.CreateTargetModel(IHierarchical target) {}

    void IAgent.DestroyTargetModel() {}

    void IAgent.UpdateTargetModel() {}
  }
}
