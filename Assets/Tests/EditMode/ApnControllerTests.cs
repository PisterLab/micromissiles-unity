using NUnit.Framework;
using UnityEngine;

public class ApnControllerTests : TestBase {
  private AgentBase _agent;
  private AgentBase _targetModel;
  private ApnController _controller;

  [SetUp]
  public void SetUp() {
    _agent = new GameObject("Agent").AddComponent<AgentBase>();
    _agent.gameObject.AddComponent<Rigidbody>();
    _agent.transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, 1));
    InvokePrivateMethod(_agent, "Awake");
    _targetModel = new GameObject("Target").AddComponent<AgentBase>();
    _targetModel.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(_targetModel, "Awake");
    _agent.TargetModel = _targetModel;
    _controller = new ApnController(_agent, gain: 1);
  }

  [Test]
  public void Plan_TargetAtBoresight_NonzeroClosingVelocity_MovingUpwards() {
    _targetModel.Position = new Vector3(0, 0, 1);
    _targetModel.Velocity = new Vector3(0, 1, -1);
    _targetModel.Acceleration = new Vector3(0, 1, 0);
    // Vertical PN acceleration = gain * closing velocity * elevation line-of-sight rate = 1 * 1 * 1
    // = 1. Vertical APN feedforward acceleration = 0.5 * gain * y-acceleration = 1/2 * 1 * 1 = 0.5.
    Assert.AreEqual(new Vector3(0, 1.5f, 0), _controller.Plan());
  }

  [Test]
  public void Plan_TargetAtBoresight_NonzeroClosingVelocity_MovingUpwards_WithNoAcceleration() {
    _targetModel.Position = new Vector3(0, 0, 1);
    _targetModel.Velocity = new Vector3(0, 1, -1);
    _targetModel.Acceleration = new Vector3(0, 0, 0);
    // Vertical PN acceleration = gain * closing velocity * elevation line-of-sight rate = 1 * 1 * 1
    // = 1.
    Assert.AreEqual(new Vector3(0, 1, 0), _controller.Plan());
  }

  [Test]
  public void
  Plan_TargetAtBoresight_NonzeroClosingVelocity_MovingUpwards_WithAccelerationAlongBoresight() {
    _targetModel.Position = new Vector3(0, 0, 1);
    _targetModel.Velocity = new Vector3(0, 1, -1);
    _targetModel.Acceleration = new Vector3(0, 0, -5);
    // Vertical PN acceleration = gain * closing velocity * elevation line-of-sight rate = 1 * 1 * 1
    // = 1.
    Assert.AreEqual(new Vector3(0, 1, 0), _controller.Plan());
  }

  [Test]
  public void Plan_TargetAtBoresight_NonzeroClosingVelocity_MovingLeft() {
    _targetModel.Position = new Vector3(0, 0, 1);
    _targetModel.Velocity = new Vector3(-1, 0, -1);
    _targetModel.Acceleration = new Vector3(0, 1, 0);
    // Horizontal PN acceleration = gain * closing velocity * azimuth line-of-sight rate = 1 * 1 *
    // -1 = -1. Vertical APN feedforward acceleration = 0.5 * gain * y-acceleration = 1/2 * 1 * 1 =
    // 0.5.
    Assert.AreEqual(new Vector3(-1, 0.5f, 0), _controller.Plan());
  }

  [Test]
  public void Plan_TargetAtBoresight_NonzeroClosingVelocity_MovingLeft_WithNoAcceleration() {
    _targetModel.Position = new Vector3(0, 0, 1);
    _targetModel.Velocity = new Vector3(-1, 0, -1);
    _targetModel.Acceleration = new Vector3(0, 0, 0);
    // Horizontal PN acceleration = gain * closing velocity * azimuth line-of-sight rate = 1 * 1 *
    // -1 = -1.
    Assert.AreEqual(new Vector3(-1, 0, 0), _controller.Plan());
  }

  [Test]
  public void
  Plan_TargetAtBoresight_NonzeroClosingVelocity_MovingLeft_WithAccelerationAlongBoresight() {
    _targetModel.Position = new Vector3(0, 0, 1);
    _targetModel.Velocity = new Vector3(-1, 0, -1);
    _targetModel.Acceleration = new Vector3(0, 0, -5);
    // Horizontal PN acceleration = gain * closing velocity * azimuth line-of-sight rate = 1 * 1 *
    // -1 = -1.
    Assert.AreEqual(new Vector3(-1, 0, 0), _controller.Plan());
  }
}
