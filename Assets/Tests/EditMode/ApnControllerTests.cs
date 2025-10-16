using NUnit.Framework;
using UnityEngine;

public class ApnControllerTests : TestBase {
  private AgentBase _agent;
  private ApnController _controller;

  [SetUp]
  public void SetUp() {
    _agent = new GameObject("Agent").AddComponent<AgentBase>();
    Rigidbody agentRb = _agent.gameObject.AddComponent<Rigidbody>();
    _agent.transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, 1));
    InvokePrivateMethod(_agent, "Awake");
    _agent.HierarchicalAgent = new HierarchicalAgent(_agent);
    _controller = new ApnController(_agent, gain: 1);
  }

  [Test]
  public void Plan_TargetAtBoresight_NonzeroClosingVelocity_MovingUpwards() {
    _agent.HierarchicalAgent.TargetModel =
        new FixedHierarchical(position: new Vector3(0, 0, 1), velocity: new Vector3(0, 1, -1),
                              acceleration: new Vector3(0, 1, 0));
    Assert.AreEqual(new Vector3(0, 1.5f, 0), _controller.Plan());
  }

  [Test]
  public void Plan_TargetAtBoresight_NonzeroClosingVelocity_MovingLeft() {
    _agent.HierarchicalAgent.TargetModel =
        new FixedHierarchical(position: new Vector3(0, 0, 1), velocity: new Vector3(-1, 0, -1),
                              acceleration: new Vector3(0, 1, 0));
    Assert.AreEqual(new Vector3(-1, 0.5f, 0), _controller.Plan());
  }
}
