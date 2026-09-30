using NUnit.Framework;
using System.Reflection;
using UnityEngine;

public class TargetRequestTests : TestBase {
  private SimManager _simManager;
  private CommsManager _commsManager;
  private GameObject _interceptorObject;
  private MissileInterceptor _interceptor;

  [SetUp]
  public void SetUp() {
    _simManager = new GameObject("SimManager").AddComponent<SimManager>();
    SetSingleton(_simManager);
    _simManager.SimulationConfig = new Configs.SimulationConfig {
      CommunicationConfig =
          new Configs.CommunicationConfig {
            RetryCooldownSeconds = 1f,
            LinkConfig = new Configs.LinkConfig { PacketDeliveryRatio = 1f },
          },
    };

    _commsManager = new GameObject("CommsManager").AddComponent<CommsManager>();
    SetSingleton(_commsManager);

    _interceptorObject = new GameObject("Interceptor");
    var rigidbody = _interceptorObject.AddComponent<Rigidbody>();
    _interceptor = _interceptorObject.AddComponent<MissileInterceptor>();
    SetPrivateField(_interceptor, "_rigidbody", rigidbody);
    _interceptor.HierarchicalAgent = new HierarchicalAgent(_interceptor);
  }

  [TearDown]
  public void TearDown() {
    Object.DestroyImmediate(_interceptorObject);
    Object.DestroyImmediate(_commsManager.gameObject);
    Object.DestroyImmediate(_simManager.gameObject);
    SetSingleton<CommsManager>(null);
    SetSingleton<SimManager>(null);
  }

  [Test]
  public void FixedUpdate_NoTarget_SendsInitialRequest() {
    _interceptor.HierarchicalAgent = new HierarchicalAgent(_interceptor);

    var sender = new CommsNode(Configs.AgentType.MissileInterceptor);
    var receiver = new CommsNode(Configs.AgentType.CarrierInterceptor);
    _interceptor.CommsNode = sender;
    _interceptor.ParentCommsNode = receiver;
    _commsManager.AddNode(receiver);

    AssignTargetRequestMessage receivedRequest = null;
    receiver.OnReceived += message => receivedRequest = message as AssignTargetRequestMessage;

    FixedUpdate();

    Assert.AreEqual(TargetStatus.TargetRequested, GetTargetStatus());
    Assert.IsNotNull(receivedRequest);
    Assert.AreSame(_interceptor, receivedRequest.PayloadData.SubInterceptor);
    Assert.IsTrue(GetTargetRequestRetryAlarm().IsArmed);
  }

  [Test]
  public void FixedUpdate_TargetAcquired_DoesNotSendRequest() {
    var hierarchicalAgent = new HierarchicalAgent(_interceptor);
    SetPrivateField<IHierarchical>(hierarchicalAgent, "_target", new FixedHierarchical());
    _interceptor.HierarchicalAgent = hierarchicalAgent;
    SetTargetStatus(TargetStatus.TargetAcquired);

    var sender = new CommsNode(Configs.AgentType.MissileInterceptor);
    var receiver = new CommsNode(Configs.AgentType.CarrierInterceptor);
    _interceptor.CommsNode = sender;
    _interceptor.ParentCommsNode = receiver;
    _commsManager.AddNode(receiver);

    int requestCount = 0;
    receiver.OnReceived += message => {
      if (message is AssignTargetRequestMessage) {
        ++requestCount;
      }
    };

    FixedUpdate();

    Assert.AreEqual(TargetStatus.TargetAcquired, GetTargetStatus());
    Assert.AreEqual(0, requestCount);
    Assert.IsFalse(GetTargetRequestRetryAlarm().IsArmed);
  }

  [Test]
  public void UpdateTargetStatus_TargetAcquiredWithoutTarget_TransitionsToNoTarget() {
    _interceptor.HierarchicalAgent = new HierarchicalAgent(_interceptor);
    SetTargetStatus(TargetStatus.TargetAcquired);
    GetTargetRequestRetryAlarm().Set(currentTime: 0f, delaySeconds: 1f);

    UpdateTargetStatus();

    Assert.AreEqual(TargetStatus.NoTarget, GetTargetStatus());
    Assert.IsFalse(GetTargetRequestRetryAlarm().IsArmed);
  }

  [Test]
  public void FixedUpdate_TargetAcquiredThenTerminated_RequestsReplacementImmediately() {
    var target = new TestTarget();
    var hierarchicalAgent = new TestHierarchicalAgent(_interceptor) { Target = target };
    _interceptor.HierarchicalAgent = hierarchicalAgent;

    var sender = new CommsNode(Configs.AgentType.MissileInterceptor);
    var receiver = new CommsNode(Configs.AgentType.CarrierInterceptor);
    _interceptor.CommsNode = sender;
    _interceptor.ParentCommsNode = receiver;
    _commsManager.AddNode(receiver);

    AssignTargetRequestMessage receivedRequest = null;
    receiver.OnReceived += message => receivedRequest = message as AssignTargetRequestMessage;

    target.Terminated = true;
    FixedUpdate();

    Assert.AreEqual(TargetStatus.TargetRequested, GetTargetStatus());
    Assert.IsNotNull(receivedRequest);
    Assert.AreSame(_interceptor, receivedRequest.PayloadData.SubInterceptor);
    Assert.IsTrue(GetTargetRequestRetryAlarm().IsArmed);
  }

  [Test]
  public void FixedUpdate_TargetRequestedWithActiveTarget_RetriesAfterDelay() {
    var hierarchicalAgent = new HierarchicalAgent(_interceptor);
    SetPrivateField<IHierarchical>(hierarchicalAgent, "_target", new FixedHierarchical());
    _interceptor.HierarchicalAgent = hierarchicalAgent;

    var sender = new CommsNode(Configs.AgentType.MissileInterceptor);
    var receiver = new CommsNode(Configs.AgentType.CarrierInterceptor);
    _interceptor.CommsNode = sender;
    _interceptor.ParentCommsNode = receiver;
    _commsManager.AddNode(receiver);

    int requestCount = 0;
    receiver.OnReceived += message => {
      if (message is AssignTargetRequestMessage) {
        ++requestCount;
      }
    };

    SendOwnAssignTargetRequest();
    FixedUpdate();

    Assert.AreEqual(TargetStatus.TargetRequested, GetTargetStatus());
    Assert.AreEqual(1, requestCount,
                    "The pending request should not repeat before the retry delay.");

    SetPrivateField(_interceptor, "<ElapsedTime>k__BackingField", 1f);
    FixedUpdate();

    Assert.AreEqual(2, requestCount,
                    "The pending request should retry after the configured delay.");
  }

  [Test]
  public void ForwardAssignTargetRequest_DoesNotChangeOwnTargetStatus() {
    var sender = new CommsNode(Configs.AgentType.CarrierInterceptor);
    var receiver = new CommsNode(Configs.AgentType.Vessel);
    _interceptor.CommsNode = sender;
    _interceptor.ParentCommsNode = receiver;
    _commsManager.AddNode(receiver);
    SetTargetStatus(TargetStatus.TargetAcquired);

    var subInterceptorObject = new GameObject("SubInterceptor");
    subInterceptorObject.AddComponent<Rigidbody>();
    var subInterceptor = subInterceptorObject.AddComponent<MissileInterceptor>();
    try {
      AssignTargetRequestMessage receivedRequest = null;
      receiver.OnReceived += message => receivedRequest = message as AssignTargetRequestMessage;

      ForwardAssignTargetRequest(subInterceptor);

      Assert.IsNotNull(receivedRequest);
      Assert.AreSame(subInterceptor, receivedRequest.PayloadData.SubInterceptor);
      Assert.AreEqual(TargetStatus.TargetAcquired, GetTargetStatus());
      Assert.IsFalse(GetTargetRequestRetryAlarm().IsArmed);
    } finally {
      Object.DestroyImmediate(subInterceptorObject);
    }
  }

  [Test]
  public void ForwardAssignTargetRequest_RepeatedRequestDoesNotUseOwnFsm() {
    var sender = new CommsNode(Configs.AgentType.CarrierInterceptor);
    var receiver = new CommsNode(Configs.AgentType.Vessel);
    _interceptor.CommsNode = sender;
    _interceptor.ParentCommsNode = receiver;
    _commsManager.AddNode(receiver);
    SetTargetStatus(TargetStatus.TargetAcquired);

    var subInterceptorObject = new GameObject("SubInterceptor");
    subInterceptorObject.AddComponent<Rigidbody>();
    var subInterceptor = subInterceptorObject.AddComponent<MissileInterceptor>();
    try {
      int forwardedRequestCount = 0;
      receiver.OnReceived += message => {
        if (message is AssignTargetRequestMessage request) {
          Assert.AreSame(subInterceptor, request.PayloadData.SubInterceptor);
          ++forwardedRequestCount;
        }
      };

      ForwardAssignTargetRequest(subInterceptor);
      ForwardAssignTargetRequest(subInterceptor);

      Assert.AreEqual(
          2, forwardedRequestCount,
          "Forwarded requests should not be suppressed by the carrier's own response timer.");
      Assert.AreEqual(TargetStatus.TargetAcquired, GetTargetStatus());
      Assert.IsFalse(GetTargetRequestRetryAlarm().IsArmed);
    } finally {
      Object.DestroyImmediate(subInterceptorObject);
    }
  }

  [Test]
  public void ParentCommsNode_Changed_ClearsAlarmAndSendsImmediately() {
    var sender = new CommsNode(Configs.AgentType.MissileInterceptor);
    var firstReceiver = new CommsNode(Configs.AgentType.CarrierInterceptor);
    var secondReceiver = new CommsNode(Configs.AgentType.Vessel);
    _interceptor.CommsNode = sender;
    _interceptor.ParentCommsNode = firstReceiver;
    _commsManager.AddNode(firstReceiver);
    _commsManager.AddNode(secondReceiver);

    int firstReceiverMessageCount = 0;
    int secondReceiverMessageCount = 0;
    firstReceiver.OnReceived +=
        _ => ++firstReceiverMessageCount;
    secondReceiver.OnReceived +=
        _ => ++secondReceiverMessageCount;

    FixedUpdate();
    FixedUpdate();
    Assert.AreEqual(1, firstReceiverMessageCount,
                    "The alarm should suppress a retry before the cooldown expires.");

    _interceptor.ParentCommsNode = secondReceiver;
    Assert.IsFalse(GetTargetRequestRetryAlarm().IsArmed);
    FixedUpdate();
    Assert.AreEqual(1, secondReceiverMessageCount,
                    "Changing the parent should allow an immediate request.");

    FixedUpdate();
    Assert.AreEqual(1, secondReceiverMessageCount, "The new request should arm a fresh cooldown.");

    SetPrivateField(_interceptor, "<ElapsedTime>k__BackingField", 2f);
    FixedUpdate();
    Assert.AreEqual(2, secondReceiverMessageCount,
                    "The request should retry when the new alarm fires.");
  }

  [Test]
  public void RegisterMessageReceived_AcceptedResponse_CompletesTargetRequest() {
    var hierarchicalAgent = new TestHierarchicalAgent(_interceptor);
    _interceptor.HierarchicalAgent = hierarchicalAgent;

    var sender = new CommsNode(Configs.AgentType.MissileInterceptor);
    var receiver = new CommsNode(Configs.AgentType.CarrierInterceptor);
    _interceptor.CommsNode = sender;
    _interceptor.ParentCommsNode = receiver;
    _commsManager.AddNode(receiver);

    SetPrivateField(_interceptor, "<ElapsedTime>k__BackingField", 0.5f);
    SendOwnAssignTargetRequest();
    var target = new FixedHierarchical();

    RegisterMessageReceived(new AssignTargetResponseMessage(receiver, sender, target));

    Assert.AreSame(target, hierarchicalAgent.Target);
    Assert.AreEqual(TargetStatus.TargetAcquired, GetTargetStatus());
    Assert.IsFalse(GetTargetRequestRetryAlarm().IsArmed);
  }

  [Test]
  public void RegisterMessageReceived_RejectedResponseWithLiveTarget_CompletesTargetRequest() {
    _interceptor.Velocity = Vector3.forward;
    _interceptor.StaticConfig = new Configs.StaticConfig {
      AccelerationConfig =
          new Configs.AccelerationConfig {
            MaxReferenceNormalAcceleration = 1f,
            ReferenceSpeed = 1f,
          },
      LiftDragConfig =
          new Configs.LiftDragConfig {
            DragCoefficient = 0.7f,
            LiftDragRatio = 5f,
          },
      BodyConfig =
          new Configs.BodyConfig {
            CrossSectionalArea = 1f,
            Mass = 1f,
          },
    };

    var currentTarget = new FixedHierarchical(new Vector3(0f, 0f, 10f));
    var offeredTarget = new FixedHierarchical(new Vector3(0f, 0f, 100f));
    var hierarchicalAgent = new TestHierarchicalAgent(_interceptor) { Target = currentTarget };
    _interceptor.HierarchicalAgent = hierarchicalAgent;

    var sender = new CommsNode(Configs.AgentType.MissileInterceptor);
    var receiver = new CommsNode(Configs.AgentType.CarrierInterceptor);
    _interceptor.CommsNode = sender;
    _interceptor.ParentCommsNode = receiver;
    _commsManager.AddNode(receiver);

    SetPrivateField(_interceptor, "<ElapsedTime>k__BackingField", 0.5f);
    SendOwnAssignTargetRequest();

    RegisterMessageReceived(new AssignTargetResponseMessage(receiver, sender, offeredTarget));

    Assert.AreSame(currentTarget, hierarchicalAgent.Target,
                   "The lower-speed offered target should be rejected.");
    Assert.AreEqual(TargetStatus.TargetAcquired, GetTargetStatus());
    Assert.IsFalse(GetTargetRequestRetryAlarm().IsArmed);
  }

  [Test]
  public void RegisterMessageReceived_TerminatedTarget_KeepsTargetRequestPending() {
    var hierarchicalAgent = new TestHierarchicalAgent(_interceptor);
    _interceptor.HierarchicalAgent = hierarchicalAgent;

    var sender = new CommsNode(Configs.AgentType.MissileInterceptor);
    var receiver = new CommsNode(Configs.AgentType.CarrierInterceptor);
    _interceptor.CommsNode = sender;
    _interceptor.ParentCommsNode = receiver;
    _commsManager.AddNode(receiver);

    SetPrivateField(_interceptor, "<ElapsedTime>k__BackingField", 0.5f);
    SendOwnAssignTargetRequest();

    RegisterMessageReceived(
        new AssignTargetResponseMessage(receiver, sender, new HierarchicalBase()));

    Assert.IsNull(hierarchicalAgent.Target);
    Assert.AreEqual(TargetStatus.TargetRequested, GetTargetStatus());
    Assert.IsTrue(GetTargetRequestRetryAlarm().IsArmed);
  }

  private void UpdateTargetStatus() {
    MethodInfo method =
        typeof(InterceptorBase)
            .GetMethod("UpdateTargetStatus", BindingFlags.NonPublic | BindingFlags.Instance);
    Assert.IsNotNull(method);
    method.Invoke(_interceptor, null);
  }

  private void FixedUpdate() {
    MethodInfo method =
        typeof(InterceptorBase)
            .GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
    Assert.IsNotNull(method);
    method.Invoke(_interceptor, null);
  }

  private TargetStatus GetTargetStatus() => _interceptor.HierarchicalAgent.TargetStatus;

  private Alarm GetTargetRequestRetryAlarm() => GetPrivateField<Alarm>(_interceptor,
                                                                       "_targetRequestRetryAlarm");

  private void SetTargetStatus(TargetStatus status) => _interceptor.HierarchicalAgent.TargetStatus =
      status;

  private void ForwardAssignTargetRequest(IInterceptor subInterceptor) {
    MethodInfo method = typeof(InterceptorBase)
                            .GetMethod("ForwardAssignTargetRequest",
                                       BindingFlags.NonPublic | BindingFlags.Instance);
    Assert.IsNotNull(method);
    method.Invoke(_interceptor, new object[] { subInterceptor });
  }

  private void SendOwnAssignTargetRequest() {
    MethodInfo method = typeof(InterceptorBase)
                            .GetMethod("SendOwnAssignTargetRequest",
                                       BindingFlags.NonPublic | BindingFlags.Instance);
    Assert.IsNotNull(method);
    method.Invoke(_interceptor, null);
  }

  private void RegisterMessageReceived(Message message) {
    MethodInfo method =
        typeof(InterceptorBase)
            .GetMethod("RegisterMessageReceived", BindingFlags.NonPublic | BindingFlags.Instance);
    Assert.IsNotNull(method);
    method.Invoke(_interceptor, new object[] { message });
  }

  private sealed class TestTarget : HierarchicalBase {
    public bool Terminated { get; set; }

    public override bool IsTerminated => Terminated;
  }

  private sealed class TestHierarchicalAgent : HierarchicalAgent {
    private IHierarchical _target;

    public override IHierarchical Target {
      get => _target;
      set {
        _target = value;
        TargetStatus = _target != null && !_target.IsTerminated ? TargetStatus.TargetAcquired
                                                                : TargetStatus.NoTarget;
      }
    }

    public TestHierarchicalAgent(IAgent agent) : base(agent) {}
  }
}
