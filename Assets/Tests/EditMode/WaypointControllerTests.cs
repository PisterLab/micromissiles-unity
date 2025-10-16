using NUnit.Framework;
using UnityEngine;

public class WaypointControllerTests : TestBase {
  private AgentBase _agent;
  private WaypointController _controller;

  [SetUp]
  public void SetUp() {
    _agent = new GameObject("Agent").AddComponent<AgentBase>();
    _agent.StaticConfig = new Configs.StaticConfig() {
      AccelerationConfig =
          new Configs.AccelerationConfig() {
            MaxForwardAcceleration = 10,
            MaxReferenceNormalAcceleration = 5 / Constants.kGravity,
            ReferenceSpeed = 1,
          },
    };
    Rigidbody agentRb = _agent.gameObject.AddComponent<Rigidbody>();
    _agent.transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, 1));
    InvokePrivateMethod(_agent, "Awake");
    _agent.Velocity = new Vector3(0, 0, 1);
    _agent.HierarchicalAgent = new HierarchicalAgent(_agent);
    _controller = new WaypointController(_agent);
  }

  [Test]
  public void Plan_WithinCutoffDistance_ReturnsZero() {
    _agent.HierarchicalAgent.TargetModel =
        new FixedHierarchical(position: new Vector3(0, 0.5f, 0.5f), velocity: new Vector3(0, 1, -1),
                              acceleration: new Vector3(0, 1, 0));
    Assert.AreEqual(Vector3.zero, _controller.Plan());
  }

  [Test]
  public void Plan_UsesMaximumForwardAcceleration() {
    _agent.HierarchicalAgent.TargetModel =
        new FixedHierarchical(position: new Vector3(0, 1, 1), velocity: new Vector3(0, 1, -1),
                              acceleration: new Vector3(0, 1, 0));
    Assert.AreEqual(10, _controller.Plan().z);
  }

  [Test]
  public void Plan_UsesMaximumNormalAcceleration() {
    _agent.HierarchicalAgent.TargetModel =
        new FixedHierarchical(position: new Vector3(0, 1, 1), velocity: new Vector3(0, 1, -1),
                              acceleration: new Vector3(0, 1, 0));
    Assert.AreEqual(5, _controller.Plan().y);
  }
}
