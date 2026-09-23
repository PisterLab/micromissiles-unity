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
    _interceptorObject.AddComponent<Rigidbody>();
    _interceptor = _interceptorObject.AddComponent<MissileInterceptor>();
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
  public void SendAssignTargetRequest_ChangedRequestBypassesRetryDelay() {
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

    SendAssignTargetRequest();
    SendAssignTargetRequest();
    Assert.AreEqual(1, firstReceiverMessageCount,
                    "The same request should wait for the retry delay.");

    _interceptor.ParentCommsNode = secondReceiver;
    SendAssignTargetRequest();
    Assert.AreEqual(1, secondReceiverMessageCount, "A changed request should be sent immediately.");

    SendAssignTargetRequest();
    Assert.AreEqual(1, secondReceiverMessageCount,
                    "The changed request should then become the pending request.");

    SetPrivateField(_interceptor, "<ElapsedTime>k__BackingField", 1f);
    SendAssignTargetRequest();
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

  private void SendAssignTargetRequest() {
    MethodInfo method =
        typeof(InterceptorBase)
            .GetMethod("SendAssignTargetRequest", BindingFlags.NonPublic | BindingFlags.Instance);
    Assert.IsNotNull(method);
    method.Invoke(_interceptor, new object[] { _interceptor });
  }
}
