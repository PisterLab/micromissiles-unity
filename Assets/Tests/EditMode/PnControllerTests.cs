using NUnit.Framework;
using UnityEngine;

public class PnControllerTests : TestBase {
  private AgentBase _agent;
  private PnController _controller;

  [SetUp]
  public void SetUp() {
    _agent = new GameObject("Agent").AddComponent<AgentBase>();
    Rigidbody agentRb = _agent.gameObject.AddComponent<Rigidbody>();
    _agent.transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, 1));
    InvokePrivateMethod(_agent, "Awake");
    _agent.HierarchicalAgent = new HierarchicalAgent(_agent);
    _controller = new PnController(_agent, gain: 1);
  }

  [Test]
  public void Plan_NoHierarchicalAgent_ReturnsZero() {
    _agent.HierarchicalAgent = null;
    Assert.AreEqual(Vector3.zero, _controller.Plan());
  }

  [Test]
  public void Plan_NoTargetModel_ReturnsZero() {
    _agent.HierarchicalAgent.TargetModel = null;
    Assert.AreEqual(Vector3.zero, _controller.Plan());
  }

  [Test]
  public void Plan_TargetAtBoresight_ZeroClosingVelocity_ReturnsZero() {
    _agent.HierarchicalAgent.TargetModel =
        new FixedHierarchical(position: new Vector3(0, 0, 1), velocity: new Vector3(0, 1, 0));
    Assert.AreEqual(Vector3.zero, _controller.Plan());
    Assert.AreEqual(Vector3.zero, _controller.Plan());
  }

  [Test]
  public void Plan_TargetAtBoresight_NonzeroClosingVelocity_MovingUpwards() {
    _agent.HierarchicalAgent.TargetModel =
        new FixedHierarchical(position: new Vector3(0, 0, 1), velocity: new Vector3(0, 1, -1));
    Assert.AreEqual(new Vector3(0, 1, 0), _controller.Plan());
  }

  [Test]
  public void Plan_TargetAtBoresight_NonzeroClosingVelocity_MovingLeft() {
    _agent.HierarchicalAgent.TargetModel =
        new FixedHierarchical(position: new Vector3(0, 0, 1), velocity: new Vector3(-1, 0, -1));
    Assert.AreEqual(new Vector3(-1, 0, 0), _controller.Plan());
  }
}
