using NUnit.Framework;
using UnityEngine;

public class PnControllerTests : TestBase {
  private AgentBase _agent;
  private AgentBase _targetModel;
  private PnController _controller;

  [SetUp]
  public void SetUp() {
    _agent = new GameObject("Agent").AddComponent<AgentBase>();
    _agent.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(_agent, "Awake");
    _agent.Transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, 1));
    _targetModel = new GameObject("Target").AddComponent<AgentBase>();
    _targetModel.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(_targetModel, "Awake");
    _agent.TargetModel = _targetModel;
    _controller = new PnController(_agent, gain: 1);
  }

  [Test]
  public void Plan_NoTargetModel_ReturnsZero() {
    _agent.TargetModel = null;
    Assert.AreEqual(Vector3.zero, _controller.Plan());
  }

  [Test]
  public void Plan_TargetAtBoresight_ZeroClosingVelocity_ReturnsZero() {
    _targetModel.Position = new Vector3(0, 0, 1);
    _targetModel.Velocity = new Vector3(0, 1, 0);
    Assert.AreEqual(Vector3.zero, _controller.Plan());
  }

  [Test]
  public void Plan_TargetAtBoresight_NegativeClosingVelocity_ReturnsZero() {
    _targetModel.Position = new Vector3(0, 0, 1);
    _targetModel.Velocity = new Vector3(0, 1, 1);
    // Vertical acceleration = gain * closing velocity * elevation line-of-sight rate = 1 * 1 * 1
    // = 1. The acceleration is multiplied by a turn factor of 100 to force a quicker turn.
    Assert.AreEqual(new Vector3(0, 1, 0) * 100, _controller.Plan());
  }

  [Test]
  public void Plan_TargetAtBoresight_NonzeroClosingVelocity_MovingUpward() {
    _targetModel.Position = new Vector3(0, 0, 1);
    _targetModel.Velocity = new Vector3(0, 1, -1);
    // Vertical acceleration = gain * closing velocity * elevation line-of-sight rate = 1 * 1 * 1
    // = 1.
    Assert.AreEqual(new Vector3(0, 1, 0), _controller.Plan());
  }

  [Test]
  public void Plan_TargetAtBoresight_NonzeroClosingVelocity_MovingLeft() {
    _targetModel.Position = new Vector3(0, 0, 1);
    _targetModel.Velocity = new Vector3(-1, 0, -1);
    // Horizontal acceleration = gain * closing velocity * azimuth line-of-sight rate = 1 * 1 * -1 =
    // -1.
    Assert.AreEqual(new Vector3(-1, 0, 0), _controller.Plan());
  }

  [Test]
  public void Plan_TargetAtBroadside_MovingInParallel() {
    _targetModel.Position = new Vector3(1, 0, 0);
    _targetModel.Velocity = new Vector3(-1, 0, 1);
    // Horizontal acceleration = gain * closing velocity * azimuth line-of-sight rate = 1 * 1 * -1 =
    // -1. Vertical acceleration is clamped, so gain * closing velocity * elevation line-of-sight
    // rate = 1 * 1 * 0.2 = 0.2. The acceleration is multiplied by a turn factor of 100 to force a
    // quicker turn.
    Assert.AreEqual(new Vector3(-1, 0.2f, 0) * 100, _controller.Plan());
  }

  [Test]
  public void Plan_TargetAtBroadside_MovingUpward() {
    _targetModel.Position = new Vector3(1, 0, 0);
    _targetModel.Velocity = new Vector3(-1, 1, 0);
    // Horizontal acceleration is clamped, so gain * closing velocity * azimuth line-of-sight rate =
    // 1 * 1 * 0.2 = 0.2. Vertical acceleration = gain * closing velocity * elevation line-of-sight
    // rate = 1 * 1 * 1 = 1. The acceleration is multiplied by a turn factor of 100 to force a
    // quicker turn.
    Assert.AreEqual(new Vector3(0.2f, 1, 0) * 100, _controller.Plan());
  }

  [Test]
  public void Plan_TargetOverhead_MovingInParallel() {
    _targetModel.Position = new Vector3(0, 1, 0);
    _targetModel.Velocity = new Vector3(0, -1, 1);
    // Horizontal acceleration is clamped, so gain * closing velocity * azimuth line-of-sight rate =
    // 1 * 1 * 0.2 = 0.2.
    // Vertical acceleration = gain * closing velocity * elevation line-of-sight rate = 1 * 1 * -1 =
    // -1.
    // The acceleration is multiplied by a turn factor of 100 to force a quicker turn.
    Assert.AreEqual(new Vector3(0.2f, -1, 0) * 100, _controller.Plan());
  }

  [Test]
  public void Plan_TargetOverhead_MovingRight() {
    _targetModel.Position = new Vector3(0, 1, 0);
    _targetModel.Velocity = new Vector3(1, -1, 0);
    // Horizontal acceleration = gain * closing velocity * azimuth line-of-sight rate = 1 * 1 * 0.2
    // = 0.2.
    // Vertical acceleration is clamped, so gain * closing velocity * elevation line-of-sight rate =
    // 1 * 1 * -1 = -1.
    // The acceleration is multiplied by a turn factor of 100 to force a quicker turn.
    Assert.AreEqual(new Vector3(0.2f, -1, 0) * 100, _controller.Plan());
  }
}
