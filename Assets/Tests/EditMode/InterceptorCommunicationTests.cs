using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public sealed class InterceptorCommunicationTestInterceptor : InterceptorBase {}

public class InterceptorCommunicationTests : TestBase {
  private const float _cooldownSeconds = 2f;
  private const float _scanPeriodSeconds = 3f;
  private const float _beforeDeadlineSeconds = 0.001f;

  private readonly List<GameObject> _testObjects = new();

  private SimManager _simManager;
  private CommsManager _commsManager;
  private Configs.LinkConfig _defaultLinkConfig;

  [SetUp]
  public void SetUp() {
    _simManager = CreateManager<SimManager>("SimManager");
    SetSingleton(_simManager);

    _defaultLinkConfig = new Configs.LinkConfig {
      LatencySeconds = 0f,
      LatencyStdSeconds = 0f,
      PacketDeliveryRatio = 1f,
      CooldownRetrySeconds = _cooldownSeconds,
    };
    _simManager.SimulationConfig = new Configs.SimulationConfig {
      CommunicationConfig =
          new Configs.CommunicationConfig {
            LinkConfig = _defaultLinkConfig,
            EscapingTargetScanPeriodSeconds = _scanPeriodSeconds,
          },
    };

    _commsManager = CreateManager<CommsManager>("CommsManager");
    SetSingleton(_commsManager);
  }

  [TearDown]
  public void TearDown() {
    foreach (GameObject testObject in _testObjects) {
      if (testObject != null) {
        Object.DestroyImmediate(testObject);
      }
    }
    _testObjects.Clear();

    SetSingleton<CommsManager>(null);
    SetSingleton<SimManager>(null);
  }

  [Test]
  // Drops the initial AssignTargetRequest at 0 seconds, verifies that it is not resent at
  // 1.999 seconds, and verifies that it is resent when the 2-second link cooldown expires.
  public void AssignTargetRequest_FirstAttemptDropped_RetriesAtLinkCooldown() {
    var parentNode = new CommsNode(Configs.AgentType.CarrierInterceptor);
    InterceptorCommunicationTestInterceptor interceptor =
        CreateInterceptor("Interceptor", Configs.AgentType.MissileInterceptor, parentNode);

    int receivedRequests = 0;
    parentNode.OnReceived += message => {
      if (message is AssignTargetRequestMessage) {
        ++receivedRequests;
      }
    };
    _commsManager.AddNode(parentNode);

    _defaultLinkConfig.PacketDeliveryRatio = 0f;
    InvokeFixedUpdate(interceptor);

    Assert.AreEqual(0, receivedRequests);
    Assert.IsFalse(interceptor.HierarchicalAgent.CanRequestTarget);

    _defaultLinkConfig.PacketDeliveryRatio = 1f;
    SetSimulationTime(_cooldownSeconds - _beforeDeadlineSeconds);
    InvokeFixedUpdate(interceptor);
    Assert.AreEqual(0, receivedRequests);

    SetSimulationTime(_cooldownSeconds);
    InvokeFixedUpdate(interceptor);
    Assert.AreEqual(1, receivedRequests);
  }

  [Test]
  // Drops the initial ReassignTargetRequest at 0 seconds, verifies that it is not resent at
  // 2.999 seconds, and verifies that it is resent when the 3-second target scan period expires.
  public void ReassignTargetRequest_FirstAttemptDropped_RetriesAtScanPeriod() {
    var parentNode = new CommsNode(Configs.AgentType.CarrierInterceptor);
    InterceptorCommunicationTestInterceptor interceptor =
        CreateInterceptor("Interceptor", Configs.AgentType.MissileInterceptor, parentNode);
    var target =
        new FixedHierarchical(position: new Vector3(0, 0, 10), velocity: new Vector3(0, 0, -1));
    SetPrivateField(interceptor.HierarchicalAgent, "_target", target);
    interceptor.EscapeDetector = new GeometricEscapeDetector(interceptor);

    int receivedRequests = 0;
    parentNode.OnReceived += message => {
      if (message is ReassignTargetRequestMessage) {
        ++receivedRequests;
      }
    };
    _commsManager.AddNode(parentNode);

    _defaultLinkConfig.PacketDeliveryRatio = 0f;
    InvokeFixedUpdate(interceptor);

    Assert.AreEqual(0, receivedRequests);
    Dictionary<IHierarchical, float> retryTimes =
        GetPrivateField<Dictionary<IHierarchical, float>>(interceptor, "_reassignTargetRetryTimes");
    Assert.AreEqual(_scanPeriodSeconds, retryTimes[target]);

    _defaultLinkConfig.PacketDeliveryRatio = 1f;
    SetSimulationTime(_scanPeriodSeconds - _beforeDeadlineSeconds);
    InvokeFixedUpdate(interceptor);
    Assert.AreEqual(0, receivedRequests);

    SetSimulationTime(_scanPeriodSeconds);
    InvokeFixedUpdate(interceptor);
    Assert.AreEqual(1, receivedRequests);
  }

  [Test]
  // Delivers the initial AssignTargetRequest but drops its response at 0 seconds. It verifies that
  // neither message is repeated at 1.999 seconds, then verifies that the request retry at the
  // 2-second cooldown causes the parent to send a second response, which is delivered.
  public void AssignTargetResponse_FirstAttemptDropped_RequestRetryCausesSecondResponse() {
    var parentNode = new CommsNode(Configs.AgentType.CarrierInterceptor);
    InterceptorCommunicationTestInterceptor requester =
        CreateInterceptor("Requester", Configs.AgentType.MissileInterceptor, parentNode);
    InterceptorCommunicationTestInterceptor parent =
        CreateInterceptor("Parent", Configs.AgentType.CarrierInterceptor, parentCommsNode: null);
    parent.CommsNode = parentNode;
    _commsManager.AddNode(parentNode);
    var target = new FixedHierarchical();

    var responseLinkConfig = new Configs.LinkConfig {
      PacketDeliveryRatio = 0f,
      CooldownRetrySeconds = _cooldownSeconds,
    };
    _simManager.SimulationConfig.CommunicationConfig.LinkOverrides.Add(new Configs.LinkOverride {
      From = Configs.AgentType.CarrierInterceptor,
      To = Configs.AgentType.MissileInterceptor,
      LinkConfig = responseLinkConfig,
    });

    int receivedRequests = 0;
    int responseAttempts = 0;
    int receivedResponses = 0;
    parentNode.OnReceived += message => {
      if (message is not AssignTargetRequestMessage request) {
        return;
      }

      ++receivedRequests;
      ++responseAttempts;
      InvokeInterceptorMethod(parent, "SendAssignTargetResponse",
                              request.PayloadData.SubInterceptor, target);
    };
    requester.CommsNode.OnReceived += message => {
      if (message is AssignTargetResponseMessage) {
        ++receivedResponses;
      }
    };

    InvokeFixedUpdate(requester);

    Assert.AreEqual(1, receivedRequests);
    Assert.AreEqual(1, responseAttempts);
    Assert.AreEqual(0, receivedResponses);

    responseLinkConfig.PacketDeliveryRatio = 1f;
    SetSimulationTime(_cooldownSeconds - _beforeDeadlineSeconds);
    InvokeFixedUpdate(requester);
    Assert.AreEqual(1, receivedRequests);
    Assert.AreEqual(1, responseAttempts);
    Assert.AreEqual(0, receivedResponses);

    SetSimulationTime(_cooldownSeconds);
    InvokeFixedUpdate(requester);
    Assert.AreEqual(2, receivedRequests);
    Assert.AreEqual(2, responseAttempts);
    Assert.AreEqual(1, receivedResponses);
  }

  [Test]
  // At 0 seconds, an intermediate interceptor receives a ReassignTargetRequest while its
  // 2-second assignment cooldown and 3-second reassignment retry deadline are both active. The
  // request must be propagated to the next parent immediately instead of waiting for either timer.
  public void ReassignTargetRequest_PropagatedUpward_BypassesCooldownAndScanPeriod() {
    var childNode = new CommsNode(Configs.AgentType.MissileInterceptor);
    var upperParentNode = new CommsNode(Configs.AgentType.Iads);
    InterceptorCommunicationTestInterceptor intermediate =
        CreateInterceptor("Intermediate", Configs.AgentType.CarrierInterceptor, upperParentNode);
    _commsManager.AddNode(childNode);
    _commsManager.AddNode(upperParentNode);

    var target = new FixedHierarchical();
    intermediate.HierarchicalAgent.StartTargetRequestCooldown(_cooldownSeconds);
    SetPrivateField(intermediate, "_nextReassignTargetScanTime", _scanPeriodSeconds);
    Dictionary<IHierarchical, float> retryTimes = GetPrivateField<Dictionary<IHierarchical, float>>(
        intermediate, "_reassignTargetRetryTimes");
    retryTimes[target] = _scanPeriodSeconds;

    int propagatedRequests = 0;
    upperParentNode.OnReceived += message => {
      if (message is ReassignTargetRequestMessage) {
        ++propagatedRequests;
      }
    };
    intermediate.CommsNode.OnReceived += message =>
        InvokeInterceptorMethod(intermediate, "RegisterMessageReceived", message);

    _commsManager.SendMessage(
        new ReassignTargetRequestMessage(childNode, intermediate.CommsNode, target));

    Assert.AreEqual(0f, _simManager.ElapsedTime);
    Assert.IsFalse(intermediate.HierarchicalAgent.CanRequestTarget);
    Assert.AreEqual(1, propagatedRequests);
  }

  [Test]
  // At 0 seconds, a requester is still inside its 2-second assignment cooldown. A parent that can
  // resolve the propagated request must send AssignTargetResponse downward immediately rather than
  // waiting until the requester's cooldown expires at 2 seconds.
  public void AssignTargetResponse_PropagatedDownward_BypassesRequesterCooldown() {
    var parentNode = new CommsNode(Configs.AgentType.CarrierInterceptor);
    InterceptorCommunicationTestInterceptor requester =
        CreateInterceptor("Requester", Configs.AgentType.MissileInterceptor, parentNode);
    InterceptorCommunicationTestInterceptor parent =
        CreateInterceptor("Parent", Configs.AgentType.CarrierInterceptor, parentCommsNode: null);
    parent.CommsNode = parentNode;
    _commsManager.AddNode(parentNode);

    SetPrivateField(requester, "_capacityPerSubInterceptor", 1);
    SetPrivateField(requester, "_numSubInterceptorsRemaining", 1);
    var availableTarget = new FixedHierarchical(position: new Vector3(0, 0, 10));
    var availableTargets = new HierarchicalBase();
    availableTargets.AddSubHierarchical(availableTarget);
    SetPrivateField(parent.HierarchicalAgent, "_target", availableTargets);

    requester.HierarchicalAgent.StartTargetRequestCooldown(_cooldownSeconds);
    Assert.IsFalse(requester.HierarchicalAgent.CanRequestTarget);

    AssignTargetResponseMessage receivedResponse = null;
    requester.CommsNode.OnReceived += message => {
      if (message is AssignTargetResponseMessage response) {
        receivedResponse = response;
      }
    };
    parentNode.OnReceived += message =>
        InvokeInterceptorMethod(parent, "RegisterMessageReceived", message);

    _commsManager.SendMessage(
        new AssignTargetRequestMessage(requester.CommsNode, parentNode, requester));

    Assert.AreEqual(0f, _simManager.ElapsedTime);
    Assert.IsNotNull(receivedResponse);
    List<IHierarchical> assignedTargets = receivedResponse.PayloadData.Target.LeafHierarchicals(
        activeOnly: true, withTargetOnly: false);
    Assert.AreEqual(1, assignedTargets.Count);
    Assert.AreSame(availableTarget, assignedTargets[0]);
  }

  private T CreateManager<T>(string name)
      where T : MonoBehaviour {
    var gameObject = new GameObject(name);
    _testObjects.Add(gameObject);
    return gameObject.AddComponent<T>();
  }

  private InterceptorCommunicationTestInterceptor CreateInterceptor(string name,
                                                                    Configs.AgentType endpointType,
                                                                    CommsNode parentCommsNode) {
    var gameObject = new GameObject(name);
    _testObjects.Add(gameObject);
    gameObject.AddComponent<Rigidbody>();
    var interceptor = gameObject.AddComponent<InterceptorCommunicationTestInterceptor>();
    InvokeMethod(typeof(AgentBase), interceptor, "Awake");
    interceptor.StaticConfig = new Configs.StaticConfig();

    interceptor.HierarchicalAgent = new HierarchicalAgent(interceptor);
    interceptor.CommsNode = new CommsNode(endpointType);
    interceptor.ParentCommsNode = parentCommsNode;
    _commsManager.AddNode(interceptor.CommsNode);
    return interceptor;
  }

  private void SetSimulationTime(float elapsedTime) {
    SetPrivateProperty(_simManager, "ElapsedTime", elapsedTime);
  }

  private static void InvokeFixedUpdate(InterceptorBase interceptor) {
    InvokeMethod(typeof(InterceptorBase), interceptor, "FixedUpdate");
  }

  private static void InvokeInterceptorMethod(InterceptorBase interceptor, string methodName,
                                              params object[] parameters) {
    InvokeMethod(typeof(InterceptorBase), interceptor, methodName, parameters);
  }

  private static void InvokeMethod(System.Type declaringType, object instance, string methodName,
                                   params object[] parameters) {
    MethodInfo method =
        declaringType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
    Assert.IsNotNull(method, $"Method '{methodName}' was not found on {declaringType.Name}.");
    method.Invoke(instance, parameters);
  }
}
