using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;

#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices {
  internal static class IsExternalInit {}
}
#endif

public class CarrierBaseMailboxTests : TestBase {
  private Mailbox _mailbox;
  private SimManager _simManager;
  private TestCarrier _carrier;
  private readonly List<GameObject> _spawnedObjects = new List<GameObject>();

  [SetUp]
  public void SetUp() {
    SetMailboxInstance(null);
    SetSimManagerInstance(null);

    _simManager = CreateSimManagerStub();
    SetPrivateField(_simManager, "_dummyAgents", new List<IAgent>());
    SetSimManagerInstance(_simManager);
    SetElapsedTime(0f);

    _mailbox = new GameObject("Mailbox").AddComponent<Mailbox>();
    _spawnedObjects.Add(_mailbox.gameObject);

    _carrier = CreateCarrier("Carrier", Configs.AgentType.CarrierInterceptor);
    SetPrivateField(_carrier, "_capacityPerSubInterceptor", 1);
  }

  [TearDown]
  public void TearDown() {
    for (int i = _spawnedObjects.Count - 1; i >= 0; --i) {
      if (_spawnedObjects[i] != null) {
        Object.DestroyImmediate(_spawnedObjects[i]);
      }
    }
    _spawnedObjects.Clear();
    SetMailboxInstance(null);
    SetSimManagerInstance(null);
  }

  // Verifies that release bookkeeping only counts released interceptor children and wires those
  // children back to the carrier as their mailbox parent.
  [Test]
  public void ReleaseManager_MixedReleasedAgents_CountsOnlyInterceptorsAndAssignsCommsParent() {
    SetPrivateField(_carrier, "_numSubInterceptorsRemaining", 3);

    TestReleasedInterceptor releasedInterceptor =
        CreateReleasedInterceptor("ReleasedInterceptor", Configs.AgentType.MissileInterceptor);
    IAgent releasedNonInterceptor = new StubAgent(Configs.AgentType.Vessel);
    _carrier.ReleaseStrategy = new FixedReleaseStrategy(
        new List<IAgent> { releasedInterceptor, releasedNonInterceptor }) { Agent = _carrier };

    RunReleaseManagerStep(_carrier, period: 0.2f);

    Assert.AreSame(_carrier, releasedInterceptor.CommsParent);
    Assert.AreEqual(2, _carrier.NumSubInterceptorsRemaining);
  }

  // Verifies that a released interceptor can use its assigned carrier mailbox parent to request a
  // target and receive an AssignTarget response back through the mailbox on the next update.
  [Test]
  public void ReleasedInterceptor_RequestViaMailbox_ProducesCarrierAssignTargetResponse() {
    SetPrivateField(_carrier, "_numSubInterceptorsRemaining", 1);

    TestReleasedInterceptor releasedInterceptor =
        CreateReleasedInterceptor("ReleasedInterceptor", Configs.AgentType.MissileInterceptor);
    _carrier.ReleaseStrategy =
        new FixedReleaseStrategy(new List<IAgent> { releasedInterceptor }) { Agent = _carrier };

    FixedHierarchical expectedLeafTarget =
        new FixedHierarchical(position: new Vector3(25f, 0f, 0f));
    _carrier.HierarchicalAgent.Target = CreateCluster(expectedLeafTarget);
    RunReleaseManagerStep(_carrier, period: 0.2f);

    StubInterceptor requestedSubInterceptor =
        new StubInterceptor(Configs.AgentType.MissileInterceptor, capacity: 1);
    requestedSubInterceptor.HierarchicalAgent = new HierarchicalAgent(requestedSubInterceptor);

    AssignTargetMessage deliveredMessage = null;
    _mailbox.OnMessageDelivered += (_, message) => {
      if (message is AssignTargetMessage assignTarget &&
          ReferenceEquals(assignTarget.Receiver, requestedSubInterceptor)) {
        deliveredMessage = assignTarget;
      }
    };

    _mailbox.Configure(null);
    releasedInterceptor.AssignSubInterceptor(requestedSubInterceptor);

    InvokePrivateMethod(_mailbox, "Update");
    Assert.IsNull(deliveredMessage);

    InvokePrivateMethod(_mailbox, "Update");
    Assert.NotNull(deliveredMessage);
    Assert.AreSame(_carrier, deliveredMessage.Sender);
    Assert.AreSame(requestedSubInterceptor, deliveredMessage.Receiver);

    List<IHierarchical> assignedTargets = deliveredMessage.PayloadData.Target.LeafHierarchicals(
        activeOnly: true, withTargetOnly: false);
    Assert.AreEqual(1, assignedTargets.Count);
    Assert.AreSame(expectedLeafTarget, assignedTargets[0]);
  }

  private TestCarrier CreateCarrier(string name, Configs.AgentType agentType) {
    GameObject carrierObject = new GameObject(name);
    _spawnedObjects.Add(carrierObject);
    carrierObject.AddComponent<Rigidbody>();

    TestCarrier carrier = carrierObject.AddComponent<TestCarrier>();
    carrier.HierarchicalAgent = new HierarchicalAgent(carrier);
    carrier.InvokeAwakeForTest();
    carrier.StaticConfig = CreateStaticConfig(agentType);
    return carrier;
  }

  private TestReleasedInterceptor CreateReleasedInterceptor(string name,
                                                            Configs.AgentType agentType) {
    GameObject interceptorObject = new GameObject(name);
    _spawnedObjects.Add(interceptorObject);
    interceptorObject.AddComponent<Rigidbody>();

    TestReleasedInterceptor interceptor = interceptorObject.AddComponent<TestReleasedInterceptor>();
    interceptor.HierarchicalAgent = new HierarchicalAgent(interceptor);
    interceptor.InvokeAwakeForTest();
    interceptor.StaticConfig = CreateStaticConfig(agentType);
    return interceptor;
  }

  private static void RunReleaseManagerStep(CarrierBase carrier, float period) {
    MethodInfo releaseManagerMethod =
        typeof(CarrierBase)
            .GetMethod("ReleaseManager", BindingFlags.NonPublic | BindingFlags.Instance);
    IEnumerator releaseManager =
        (IEnumerator)releaseManagerMethod.Invoke(carrier, new object[] { period });
    releaseManager.MoveNext();
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

  private static Configs.StaticConfig CreateStaticConfig(Configs.AgentType agentType) {
    return new Configs.StaticConfig {
      AgentType = agentType,
      BodyConfig = new Configs.BodyConfig { Mass = 1f, CrossSectionalArea = 1f },
      LiftDragConfig = new Configs.LiftDragConfig { DragCoefficient = 1f, LiftDragRatio = 1f },
    };
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

  private sealed class TestCarrier : CarrierBase, IAgent {
    public void InvokeAwakeForTest() {
      base.Awake();
    }

    void IAgent.CreateTargetModel(IHierarchical target) {}

    void IAgent.DestroyTargetModel() {}

    void IAgent.UpdateTargetModel() {}
  }

  private sealed class TestReleasedInterceptor : InterceptorBase, IAgent {
    public void InvokeAwakeForTest() {
      base.Awake();
    }

    void IAgent.CreateTargetModel(IHierarchical target) {}

    void IAgent.DestroyTargetModel() {}

    void IAgent.UpdateTargetModel() {}
  }

  private sealed class FixedReleaseStrategy : IReleaseStrategy {
    private readonly List<IAgent> _releasedAgents;

    public IAgent Agent { get; init; }

    public FixedReleaseStrategy(List<IAgent> releasedAgents) {
      _releasedAgents = releasedAgents;
    }

    public List<IAgent> Release() {
      return _releasedAgents;
    }
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
