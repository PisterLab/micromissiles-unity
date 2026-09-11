using NUnit.Framework;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManagerTests : TestBase {
  private sealed class TestInterceptor : IInterceptor {
    private static readonly IReadOnlyList<string> _emptyTargetIds = Array.Empty<string>();

    public event Action<IAgent> OnTerminated;
    public event Action<IInterceptor> OnHit;
    public event Action<IInterceptor> OnMiss;
    public event Action<IInterceptor> OnDestroyed;

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
    public CommsNode ParentCommsNode { get; set; }
    public IEscapeDetector EscapeDetector { get; set; }
    public int Capacity => 1;
    public int CapacityPerSubInterceptor => 1;
    public int CapacityPlannedRemaining => 1;
    public int CapacityRemaining => 1;
    public int NumSubInterceptors => 0;
    public int NumSubInterceptorsPlannedRemaining => 0;
    public int NumSubInterceptorsRemaining => 0;
    public bool IsReassignable => false;

    public TestInterceptor(string agentId, Configs.AgentType agentType) {
      AgentId = agentId;
      gameObject = new GameObject(agentId);
      StaticConfig = new Configs.StaticConfig { AgentType = agentType };
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
  private UIManager _uiManager;

  [SetUp]
  public void SetUp() {
    SetSingleton<UIManager>(null);
    GameObject uiObject = CreateGameObject("UI Manager");
    _uiManager = uiObject.AddComponent<UIManager>();
    _uiManager.actionMessageTextHandle = CreateText("Current");
    _uiManager.pActionMessageTextHandle = CreateText("Previous 1");
    _uiManager.ppActionMessageTextHandle = CreateText("Previous 2");
    _uiManager.ppppActionMessageTextHandle = CreateText("Previous 3");
    _uiManager.pppppActionMessageTextHandle = CreateText("Previous 4");
    _uiManager.interceptorHitTextHandle = CreateText("Interceptor Hits");
    _uiManager.interceptorMissTextHandle = CreateText("Interceptor Misses");
    _uiManager.interceptorRemainingTextHandle = CreateText("Interceptor Remaining");
    _uiManager.threatRemainingTextHandle = CreateText("Threat Remaining");
  }

  [TearDown]
  public void TearDown() {
    SetSingleton<UIManager>(null);
    foreach (GameObject gameObject in _gameObjects) {
      UnityEngine.Object.DestroyImmediate(gameObject);
    }
    _gameObjects.Clear();
  }

  [Test]
  public void AgentFilter_ShowsOnlyFollowedAgentThenRestoresFullHistory() {
    TestInterceptor launcher = CreateAgent("LCH-001", Configs.AgentType.Vessel);
    TestInterceptor carrier = CreateAgent("LCH-001-C001", Configs.AgentType.CarrierInterceptor);
    TestInterceptor missile =
        CreateAgent("LCH-001-C001-M002", Configs.AgentType.MissileInterceptor);
    TestInterceptor unrelated = CreateAgent("LCH-002-C001", Configs.AgentType.CarrierInterceptor);

    _uiManager.LogActionMessage("system update");
    _uiManager.LogActionMessage("launcher update", launcher);
    _uiManager.LogActionMessage("carrier update", carrier);
    _uiManager.LogActionMessage("missile update", missile);
    _uiManager.LogActionMessage("unrelated update", unrelated);

    _uiManager.SetAgentLogFilter(missile);

    StringAssert.Contains(missile.AgentId, _uiManager.actionMessageTextHandle.text);
    Assert.AreEqual("missile update", _uiManager.pActionMessageTextHandle.text);
    Assert.AreEqual("", _uiManager.ppActionMessageTextHandle.text);
    Assert.AreEqual("", _uiManager.ppppActionMessageTextHandle.text);
    Assert.AreEqual("", _uiManager.pppppActionMessageTextHandle.text);

    _uiManager.ClearAgentLogFilter();

    Assert.AreEqual("unrelated update", _uiManager.actionMessageTextHandle.text);
    Assert.AreEqual("missile update", _uiManager.pActionMessageTextHandle.text);
    Assert.AreEqual("carrier update", _uiManager.ppActionMessageTextHandle.text);
    Assert.AreEqual("launcher update", _uiManager.ppppActionMessageTextHandle.text);
    Assert.AreEqual("system update", _uiManager.pppppActionMessageTextHandle.text);
  }

  [Test]
  public void RegisteredInterceptor_TargetChangesAreAddedToAgentLog() {
    TestInterceptor interceptor =
        CreateAgent("LCH-001-C001-M001", Configs.AgentType.MissileInterceptor);
    TestInterceptor threat = CreateAgent("THR-S001-A0001", Configs.AgentType.FixedWingThreat);
    InvokePrivateMethod(_uiManager, "RegisterNewInterceptor", interceptor);

    interceptor.HierarchicalAgent.Target = threat.HierarchicalAgent;

    StringAssert.Contains("[TARGET] LCH-001-C001-M001", _uiManager.actionMessageTextHandle.text);
    StringAssert.Contains("THR-S001-A0001", _uiManager.actionMessageTextHandle.text);
  }

  [Test]
  public void AgentFilter_DisplaysAndRefreshesFollowedAgentTarget() {
    TestInterceptor carrier = CreateAgent("LCH-001-C001", Configs.AgentType.CarrierInterceptor);
    TestInterceptor threat1 = CreateAgent("THR-S001-A0001", Configs.AgentType.FixedWingThreat);
    TestInterceptor threat2 = CreateAgent("THR-S001-A0002", Configs.AgentType.FixedWingThreat);

    _uiManager.SetAgentLogFilter(carrier);
    StringAssert.Contains("TARGET: none", _uiManager.actionMessageTextHandle.text);

    carrier.HierarchicalAgent.Target = threat1.HierarchicalAgent;
    StringAssert.Contains("TARGET: THR-S001-A0001", _uiManager.actionMessageTextHandle.text);

    carrier.HierarchicalAgent.Target = threat2.HierarchicalAgent;
    StringAssert.Contains("TARGET: THR-S001-A0002", _uiManager.actionMessageTextHandle.text);
    StringAssert.DoesNotContain("THR-S001-A0001", _uiManager.actionMessageTextHandle.text);
  }

  private TestInterceptor CreateAgent(string agentId, Configs.AgentType agentType) {
    var agent = new TestInterceptor(agentId, agentType);
    _gameObjects.Add(agent.gameObject);
    return agent;
  }

  private TextMeshProUGUI CreateText(string name) {
    var textObject = new GameObject(name, typeof(RectTransform));
    _gameObjects.Add(textObject);
    return textObject.AddComponent<TextMeshProUGUI>();
  }

  private GameObject CreateGameObject(string name) {
    var gameObject = new GameObject(name);
    _gameObjects.Add(gameObject);
    return gameObject;
  }
}
