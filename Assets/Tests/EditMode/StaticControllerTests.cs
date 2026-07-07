using NUnit.Framework;
using UnityEngine;

public class StaticControllerTests : TestBase {
  private AgentBase _agent;
  private AgentBase _targetModel;
  private StaticController _controller;

  [SetUp]
  public void SetUp() {
    _agent = new GameObject("Agent").AddComponent<AgentBase>();
    _agent.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(_agent, "Awake");
    _agent.Velocity = new Vector3(0, 0, 25);
    _agent.Transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, 1));

    _targetModel = new GameObject("Target").AddComponent<AgentBase>();
    _targetModel.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(_targetModel, "Awake");
    _targetModel.Position = new Vector3(100, 50, 1000);
    _targetModel.Velocity = new Vector3(-10, 0, -250);
    _targetModel.Acceleration = new Vector3(0, -1, 3);

    _agent.TargetModel = _targetModel;
    _controller = new StaticController(_agent);
  }

  [Test]
  public void Plan_WithMovingTargetModel_ReturnsZeroAcceleration() {
    Assert.AreEqual(Vector3.zero, _controller.Plan());
  }
}
