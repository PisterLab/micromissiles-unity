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
  }

  [Test]
  public void Plan_TargetAtBoresight_NegativeClosingVelocity_ReturnsZero() {
    _agent.HierarchicalAgent.TargetModel =
        new FixedHierarchical(position: new Vector3(0, 0, 1), velocity: new Vector3(0, 1, 1));
    // Vertical acceleration = gain * closing velocity * elevation line-of-sight rate = 1 * 1 * 1
    // = 1. The acceleration is multiplied by a turn factor of 100 to force a quicker turn.
    Assert.AreEqual(new Vector3(0, 1, 0) * 100, _controller.Plan());
  }

  [Test]
  public void Plan_TargetAtBoresight_NonzeroClosingVelocity_MovingUpwards() {
    _agent.HierarchicalAgent.TargetModel =
        new FixedHierarchical(position: new Vector3(0, 0, 1), velocity: new Vector3(0, 1, -1));
    // Vertical acceleration = gain * closing velocity * elevation line-of-sight rate = 1 * 1 * 1
    // = 1.
    Assert.AreEqual(new Vector3(0, 1, 0), _controller.Plan());
  }

  [Test]
  public void Plan_TargetAtBoresight_NonzeroClosingVelocity_MovingLeft() {
    _agent.HierarchicalAgent.TargetModel =
        new FixedHierarchical(position: new Vector3(0, 0, 1), velocity: new Vector3(-1, 0, -1));
    // Horizontal acceleration = gain * closing velocity * azimuth line-of-sight rate = 1 * 1 * -1 =
    // -1.
    Assert.AreEqual(new Vector3(-1, 0, 0), _controller.Plan());
  }

  [Test]
  public void Plan_TargetAtBroadside_MovingInParallel() {
    _agent.HierarchicalAgent.TargetModel =
        new FixedHierarchical(position: new Vector3(1, 0, 0), velocity: new Vector3(-1, 0, 1));
    // Horizontal acceleration = gain * closing velocity * azimuth line-of-sight rate = 1 * 1 * -1 =
    // -1. Vertical acceleration is clamped, so gain * closing velocity * elevation line-of-sight
    // rate = 1 * 1 * 0.2 = 0.2. The acceleration is multiplied by a turn factor of 100 to force a
    // quicker turn.
    Assert.AreEqual(new Vector3(-1, 0.2f, 0) * 100, _controller.Plan());
  }

  [Test]
  public void Plan_TargetAtBroadside_MovingUpwards() {
    _agent.HierarchicalAgent.TargetModel =
        new FixedHierarchical(position: new Vector3(1, 0, 0), velocity: new Vector3(-1, 1, 0));
    // Horizontal acceleration is clamped, so gain * closing velocity * azimuth line-of-sight rate =
    // 1 * 1 * 0.2 = 0.2. Vertical acceleration = gain * closing velocity * elevation line-of-sight
    // rate = 1 * 1 * 1 = 1. The acceleration is multiplied by a turn factor of 100 to force a
    // quicker turn.
    Assert.AreEqual(new Vector3(0.2f, 1, 0) * 100, _controller.Plan());
  }

  [Test]
  public void Plan_TargetOverhead_MovingInParallel() {
    _agent.HierarchicalAgent.TargetModel =
        new FixedHierarchical(position: new Vector3(0, 1, 0), velocity: new Vector3(0, -1, 1));
    // Horizontal acceleration is clamped, so gain * closing velocity * azimuth line-of-sight rate =
    // 1 * 1 * 0.2 = 0.2.
    // Vertical acceleration = gain * closing velocity * elevation line-of-sight rate = 1 * 1 * -1 =
    // -1.
    // The acceleration is multiplied by a turn factor of 100 to force a quicker turn.
    Assert.AreEqual(new Vector3(0.2f, -1, 0) * 100, _controller.Plan());
  }

  [Test]
  public void Plan_TargetOverhead_MovingRight() {
    _agent.HierarchicalAgent.TargetModel =
        new FixedHierarchical(position: new Vector3(0, 1, 0), velocity: new Vector3(1, -1, 0));
    // Horizontal acceleration = gain * closing velocity * azimuth line-of-sight rate = 1 * 1 * 1
    // = 1.
    // Vertical acceleration is clamped, so gain * closing velocity * elevation line-of-sight rate =
    // 1 * 1 * 0.2 = 0.2. The acceleration is multiplied by a turn factor of 100 to force a quicker
    // turn.
    Assert.AreEqual(new Vector3(1, 0.2f, 0) * 100, _controller.Plan());
  }
}
