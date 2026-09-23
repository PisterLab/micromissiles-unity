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
            ResponseRetrySeconds = 1f,
            LinkConfig = new Configs.LinkConfig { PacketDeliveryRatio = 1f },
          },
    };

    _commsManager = new GameObject("CommsManager").AddComponent<CommsManager>();
    SetSingleton(_commsManager);

    _interceptorObject = new GameObject("Interceptor");
    var rigidbody = _interceptorObject.AddComponent<Rigidbody>();
    _interceptor = _interceptorObject.AddComponent<MissileInterceptor>();
    SetPrivateField(_interceptor, "_rigidbody", rigidbody);
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
  public void InitializeTargetStatus_WithNoTarget_InitializesNoTarget() {
    _interceptor.HierarchicalAgent = new HierarchicalAgent(_interceptor);
    SetTargetStatus("TargetRequested");
    SetPrivateField(_interceptor, "_lastTargetRequestTime", 5f);

    InitializeTargetStatus();

    Assert.AreEqual("NoTarget", GetTargetStatus());
    Assert.AreEqual(Mathf.NegativeInfinity,
                    GetPrivateField<float>(_interceptor, "_lastTargetRequestTime"));
  }

  [Test]
  public void InitializeTargetStatus_WithExistingTarget_InitializesTargetAcquired() {
    var hierarchicalAgent = new HierarchicalAgent(_interceptor);
    SetPrivateField<IHierarchical>(hierarchicalAgent, "_target", new FixedHierarchical());
    _interceptor.HierarchicalAgent = hierarchicalAgent;

    InitializeTargetStatus();

    Assert.AreEqual("TargetAcquired", GetTargetStatus());
  }

  [Test]
  public void UpdateTargetStatus_TargetAcquiredWithoutTarget_TransitionsToNoTarget() {
    _interceptor.HierarchicalAgent = new HierarchicalAgent(_interceptor);
    SetTargetStatus("TargetAcquired");

    UpdateTargetStatus();

    Assert.AreEqual("NoTarget", GetTargetStatus());
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

    Assert.AreEqual("TargetRequested", GetTargetStatus());
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
    SetTargetStatus("TargetAcquired");

    var subInterceptorObject = new GameObject("SubInterceptor");
    subInterceptorObject.AddComponent<Rigidbody>();
    var subInterceptor = subInterceptorObject.AddComponent<MissileInterceptor>();
    try {
      AssignTargetRequestMessage receivedRequest = null;
      receiver.OnReceived += message => receivedRequest = message as AssignTargetRequestMessage;

      ForwardAssignTargetRequest(subInterceptor);

      Assert.IsNotNull(receivedRequest);
      Assert.AreSame(subInterceptor, receivedRequest.PayloadData.SubInterceptor);
      Assert.AreEqual("TargetAcquired", GetTargetStatus());
      Assert.IsNull(
          GetPrivateField<AssignTargetRequestMessage>(_interceptor, "_pendingTargetRequest"));
    } finally {
      Object.DestroyImmediate(subInterceptorObject);
    }
  }

  [Test]
  public void SendOwnAssignTargetRequest_ChangedRequestBypassesRetryDelay() {
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

    SendOwnAssignTargetRequest();
    SendOwnAssignTargetRequest();
    Assert.AreEqual(1, firstReceiverMessageCount,
                    "The same request should wait for the retry delay.");

    _interceptor.ParentCommsNode = secondReceiver;
    SendOwnAssignTargetRequest();
    Assert.AreEqual(1, secondReceiverMessageCount, "A changed request should be sent immediately.");

    SendOwnAssignTargetRequest();
    Assert.AreEqual(1, secondReceiverMessageCount,
                    "The changed request should then become the pending request.");

    SetPrivateField(_interceptor, "<ElapsedTime>k__BackingField", 1f);
    SendOwnAssignTargetRequest();
    Assert.AreEqual(2, secondReceiverMessageCount,
                    "The same request should be sent again after the retry delay.");
  }

  private void InitializeTargetStatus() {
    MethodInfo method =
        typeof(InterceptorBase)
            .GetMethod("InitializeTargetStatus", BindingFlags.NonPublic | BindingFlags.Instance);
    Assert.IsNotNull(method);
    method.Invoke(_interceptor, null);
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

  private string GetTargetStatus() {
    FieldInfo field =
        typeof(InterceptorBase)
            .GetField("_targetStatus", BindingFlags.NonPublic | BindingFlags.Instance);
    Assert.IsNotNull(field);
    return field.GetValue(_interceptor).ToString();
  }

  private void SetTargetStatus(string status) {
    FieldInfo field =
        typeof(InterceptorBase)
            .GetField("_targetStatus", BindingFlags.NonPublic | BindingFlags.Instance);
    Assert.IsNotNull(field);
    field.SetValue(_interceptor, System.Enum.Parse(field.FieldType, status));
  }

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
}
