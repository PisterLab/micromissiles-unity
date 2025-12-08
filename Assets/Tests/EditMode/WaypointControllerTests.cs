using NUnit.Framework;
using UnityEngine;

public class WaypointControllerTests : TestBase {
  private AgentBase _agent;
  private AgentBase _targetModel;
  private WaypointController _controller;

  [SetUp]
  public void SetUp() {
    _agent = new GameObject("Agent").AddComponent<AgentBase>();
    _agent.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(_agent, "Awake");
    _agent.Transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, 1));
    _agent.StaticConfig = new Configs.StaticConfig() {
      AccelerationConfig =
          new Configs.AccelerationConfig() {
            MaxForwardAcceleration = 10,
            MaxReferenceNormalAcceleration = 5 / Constants.kGravity,
            ReferenceSpeed = 1,
          },
    };
    _agent.Velocity = new Vector3(0, 0, 1);
    _targetModel = new GameObject("Target").AddComponent<AgentBase>();
    _targetModel.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(_targetModel, "Awake");
    _agent.TargetModel = _targetModel;
    _controller = new WaypointController(_agent);
  }

  [Test]
  public void Plan_WithinCutoffDistance_ReturnsZero() {
    _targetModel.Position = new Vector3(0, 0.5f, 0.5f);
    Assert.AreEqual(Vector3.zero, _controller.Plan());
  }

  [Test]
  public void Plan_UsesMaximumForwardAcceleration() {
    _targetModel.Position = new Vector3(0, 1, 1);
    Assert.AreEqual(10, _controller.Plan().z);
  }

  [Test]
  public void Plan_UsesMaximumNormalAcceleration() {
    _targetModel.Position = new Vector3(0, 1, 1);
    Assert.AreEqual(5, _controller.Plan().y);
  }

  [Test]
  public void Plan_WaypointBehind_UsesMaximumNegativeForwardAcceleration() {
    _targetModel.Position = new Vector3(0, 0, -1);
    _targetModel.Velocity = new Vector3(0, 1, -1);
    _targetModel.Acceleration = new Vector3(0, 1, 0);
    Assert.AreEqual(-10, _controller.Plan().z);
  }

  [Test]
  public void Plan_WaypointAtRightBoresight_AcceleratesToTheRight() {
    _targetModel.Position = new Vector3(1, 0, 0);
    _targetModel.Velocity = new Vector3(0, 1, -1);
    _targetModel.Acceleration = new Vector3(0, 1, 0);
    Assert.AreEqual(5, _controller.Plan().x);
    Assert.AreEqual(0, _controller.Plan().y);
  }

  [Test]
  public void Plan_WaypointOverhead_AcceleratesUpward() {
    _targetModel.Position = new Vector3(0, 1, 0);
    _targetModel.Velocity = new Vector3(0, 1, -1);
    _targetModel.Acceleration = new Vector3(0, 1, 0);
    Assert.AreEqual(0, _controller.Plan().x);
    Assert.AreEqual(5, _controller.Plan().y);
  }
}
