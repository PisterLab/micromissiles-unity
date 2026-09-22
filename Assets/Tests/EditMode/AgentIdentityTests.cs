using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class AgentIdentityTests {
  private sealed class TestAgent : IAgent {
    private static readonly IReadOnlyList<string> _emptyTargetIds = Array.Empty<string>();

    public event Action<IAgent> OnTerminated;

    public string AgentId { get; set; }
    public string TargetId => HierarchicalAgent?.TargetId ?? "";
    public IReadOnlyList<string> TargetIds => HierarchicalAgent?.TargetIds ?? _emptyTargetIds;
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
    public bool IsPursuer => true;
    public float ElapsedTime => 0f;
    public bool IsTerminated { get; private set; }
    public GameObject gameObject { get; }
    public Transform Transform => gameObject.transform;
    public Vector3 Up => Transform.up;
    public Vector3 Forward => Transform.forward;
    public Vector3 Right => Transform.right;
    public Quaternion InverseRotation => Quaternion.Inverse(Transform.rotation);
    public CommsNode CommsNode { get; set; }

    public TestAgent(string agentId) {
      AgentId = agentId;
      gameObject = new GameObject(agentId);
      HierarchicalAgent = new HierarchicalAgent(this);
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

    public Transformation GetRelativeTransformation(IAgent target) =>
        throw new NotImplementedException();
    public Transformation GetRelativeTransformation(IHierarchical target) =>
        throw new NotImplementedException();
    public Transformation GetRelativeTransformation(in Vector3 waypoint) =>
        throw new NotImplementedException();
  }

  private readonly List<GameObject> _gameObjects = new List<GameObject>();

  [TearDown]
  public void TearDown() {
    foreach (GameObject gameObject in _gameObjects) {
      UnityEngine.Object.DestroyImmediate(gameObject);
    }
    _gameObjects.Clear();
  }

  [Test]
  public void DeterministicIds_EncodeConfigurationAndParentSlots() {
    Assert.AreEqual("AST-001", AgentIdentity.AssetId(1));
    Assert.AreEqual("LCH-002", AgentIdentity.LauncherId(2));
    Assert.AreEqual("THR-S003-A0012", AgentIdentity.ThreatId(3, 12));

    string carrierId = AgentIdentity.ChildId("LCH-002", Configs.AgentType.CarrierInterceptor, 4);
    string missileId = AgentIdentity.ChildId(carrierId, Configs.AgentType.MissileInterceptor, 7);
    Assert.AreEqual("LCH-002-C004", carrierId);
    Assert.AreEqual("LCH-002-C004-M007", missileId);
  }

  [Test]
  public void ParentIds_AreDecodedOnlyFromInterceptorChildSegments() {
    Assert.IsTrue(AgentIdentity.TryGetParentId("LCH-002-C004-M007", out string carrierId));
    Assert.AreEqual("LCH-002-C004", carrierId);
    Assert.IsTrue(AgentIdentity.TryGetParentId(carrierId, out string launcherId));
    Assert.AreEqual("LCH-002", launcherId);
    Assert.IsFalse(AgentIdentity.TryGetParentId(launcherId, out _));
    Assert.IsFalse(AgentIdentity.TryGetParentId("THR-S002-A0007", out _));
  }

  [Test]
  public void CompareIds_OrdersNumericSegmentsNumerically() {
    var ids = new List<string> {
      "THR-S001-A0010",
      "THR-S001-A0002",
      "THR-S001-A0001",
    };

    ids.Sort(AgentIdentity.CompareIds);

    CollectionAssert.AreEqual(new[] { "THR-S001-A0001", "THR-S001-A0002", "THR-S001-A0010" }, ids);
    Assert.Less(AgentIdentity.CompareIds("LCH-1-C9", "LCH-1-C10"), 0);
  }

  [Test]
  public void TargetAssignment_UpdatesSingularAndPluralIdsAndRaisesHistory() {
    TestAgent interceptor = CreateAgent("LCH-001-C001");
    TestAgent threat1 = CreateAgent("THR-S001-A0001");
    TestAgent threat2 = CreateAgent("THR-S001-A0002");
    var targetCluster = new HierarchicalBase();
    targetCluster.AddSubHierarchical(threat2.HierarchicalAgent);
    targetCluster.AddSubHierarchical(threat1.HierarchicalAgent);

    var changes = new List<(IReadOnlyList<string> Previous, IReadOnlyList<string> Current)>();
    interceptor.HierarchicalAgent.OnTargetChanged += (_, previous, current) =>
        changes.Add((previous, current));

    interceptor.HierarchicalAgent.Target = targetCluster;
    Assert.AreEqual("", interceptor.TargetId);
    CollectionAssert.AreEqual(new[] { threat1.AgentId, threat2.AgentId }, interceptor.TargetIds);

    threat1.Terminate();
    Assert.AreEqual(threat2.AgentId, interceptor.TargetId);
    CollectionAssert.AreEqual(new[] { threat2.AgentId }, interceptor.TargetIds);

    interceptor.HierarchicalAgent.Target = null;
    Assert.AreEqual("", interceptor.TargetId);
    Assert.IsEmpty(interceptor.TargetIds);

    Assert.AreEqual(3, changes.Count);
    CollectionAssert.AreEqual(new[] { threat1.AgentId, threat2.AgentId }, changes[1].Previous);
    CollectionAssert.AreEqual(new[] { threat2.AgentId }, changes[1].Current);
    CollectionAssert.AreEqual(new[] { threat2.AgentId }, changes[2].Previous);
    Assert.IsEmpty(changes[2].Current);
  }

  private TestAgent CreateAgent(string agentId) {
    var agent = new TestAgent(agentId);
    _gameObjects.Add(agent.gameObject);
    return agent;
  }
}
