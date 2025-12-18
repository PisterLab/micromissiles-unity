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
    InvokePrivateMethod(_agent, "Awake");
    _agent.Velocity = new Vector3(0, 0, 1);
    _agent.Transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, 1));
    _targetModel = new GameObject("Target").AddComponent<AgentBase>();
    _targetModel.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(_targetModel, "Awake");
    _agent.TargetModel = _targetModel;
    _controller = new ApnController(_agent, gain: 1);
  }

  [Test]
  public void Plan_TargetAtBoresight_NonzeroClosingVelocity_MovingUpward() {
    _targetModel.Position = new Vector3(0, 0, 1);
    _targetModel.Velocity = new Vector3(0, 1, 0);
    _targetModel.Acceleration = new Vector3(0, 1, 0);
    // Relative position = (0, 0, 1)
    // Relative velocity = (0, 1, -1)
    // Rhat = (0, 0, 1)
    // Vhat = (0, 1, 0)
    // Line-of-sight rotation = (-1, 0, 0)
    // Closing velocity = 1
    // Gain = 1
    // isAbeam = false
    // Turn factor = 1
    // (-1, 0, 0) x (0, 0, 1) = (0, 1, 0)
    Assert.AreEqual(new Vector3(0, 1.5f, 0), _controller.Plan());
  }

  [Test]
  public void Plan_TargetAtBoresight_NonzeroClosingVelocity_MovingUpward_WithNoAcceleration() {
    _targetModel.Position = new Vector3(0, 0, 1);
    _targetModel.Velocity = new Vector3(0, 1, 0);
    _targetModel.Acceleration = new Vector3(0, 0, 0);
    // Relative position = (0, 0, 1)
    // Relative velocity = (0, 1, -1)
    // Rhat = (0, 0, 1)
    // Vhat = (0, 1, 0)
    // Line-of-sight rotation = (-1, 0, 0)
    // Closing velocity = 1
    // Gain = 1
    // isAbeam = false
    // Turn factor = 1
    // (-1, 0, 0) x (0, 0, 1) = (0, 1, 0)
    Assert.AreEqual(new Vector3(0, 1, 0), _controller.Plan());
  }

  [Test]
  public void
  Plan_TargetAtBoresight_NonzeroClosingVelocity_MovingUpward_WithAccelerationAlongBoresight() {
    _targetModel.Position = new Vector3(0, 0, 1);
    _targetModel.Velocity = new Vector3(0, 1, 0);
    _targetModel.Acceleration = new Vector3(0, 0, -5);
    // Relative position = (0, 0, 1)
    // Relative velocity = (0, 1, -1)
    // Rhat = (0, 0, 1)
    // Vhat = (0, 1, 0)
    // Line-of-sight rotation = (-1, 0, 0)
    // Closing velocity = 1
    // Gain = 1
    // isAbeam = false
    // Turn factor = 1
    // (-1, 0, 0) x (0, 0, 1) = (0, 1, 0)
    Assert.AreEqual(new Vector3(0, 1, 0), _controller.Plan());
  }

  [Test]
  public void Plan_TargetAtBoresight_NonzeroClosingVelocity_MovingLeft() {
    _targetModel.Position = new Vector3(0, 0, 1);
    _targetModel.Velocity = new Vector3(-1, 0, 0);
    _targetModel.Acceleration = new Vector3(0, 1, 0);
    // Relative position = (0, 0, 1)
    // Relative velocity = (-1, 0, -1)
    // Rhat = (0, 0, 1)
    // Vhat = (-1, 0, 0)
    // Line-of-sight rotation = (0, -1, 0)
    // Closing velocity = 1
    // Gain = 1
    // isAbeam = false
    // Turn factor = 1
    // (0, -1, 0) x (0, 0, 1) = (-1, 0, 0)
    Assert.AreEqual(new Vector3(-1, 0.5f, 0), _controller.Plan());
  }

  [Test]
  public void Plan_TargetAtBoresight_NonzeroClosingVelocity_MovingLeft_WithNoAcceleration() {
    _targetModel.Position = new Vector3(0, 0, 1);
    _targetModel.Velocity = new Vector3(-1, 0, 0);
    _targetModel.Acceleration = new Vector3(0, 0, 0);
    // Relative position = (0, 0, 1)
    // Relative velocity = (-1, 0, -1)
    // Rhat = (0, 0, 1)
    // Vhat = (-1, 0, 0)
    // Line-of-sight rotation = (0, -1, 0)
    // Closing velocity = 1
    // Gain = 1
    // isAbeam = false
    // Turn factor = 1
    // (0, -1, 0) x (0, 0, 1) = (-1, 0, 0)
    Assert.AreEqual(new Vector3(-1, 0, 0), _controller.Plan());
  }

  [Test]
  public void
  Plan_TargetAtBoresight_NonzeroClosingVelocity_MovingLeft_WithAccelerationAlongBoresight() {
    _targetModel.Position = new Vector3(0, 0, 1);
    _targetModel.Velocity = new Vector3(-1, 0, 0);
    _targetModel.Acceleration = new Vector3(0, 0, -5);
    // Relative position = (0, 0, 1)
    // Relative velocity = (-1, 0, -1)
    // Rhat = (0, 0, 1)
    // Vhat = (-1, 0, 0)
    // Line-of-sight rotation = (0, -1, 0)
    // Closing velocity = 1
    // Gain = 1
    // isAbeam = false
    // Turn factor = 1
    // (0, -1, 0) x (0, 0, 1) = (-1, 0, 0)
    Assert.AreEqual(new Vector3(-1, 0, 0), _controller.Plan());
  }
}
