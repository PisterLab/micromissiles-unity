using NUnit.Framework;
using UnityEngine;

public class AgentBaseTests : TestBase {
  private const float _epsilon = 1e-3f;

  private AgentBase _agent;
  private FixedHierarchical _target;

  [SetUp]
  public void SetUp() {
    _agent = new GameObject("Agent").AddComponent<AgentBase>();
    Rigidbody agentRb = _agent.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(_agent, "Awake");
  }

  [Test]
  public void GetRelativeTransformation_TargetAtBoresight() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 0, 0);
    _target =
        new FixedHierarchical(position: new Vector3(0, 0, 20), velocity: new Vector3(0, 20, -1));

    var relativeTransformation = _agent.GetRelativeTransformation(_target);
    Assert.AreEqual(relativeTransformation.position.cartesian, _target.Position - _agent.Position);
    Assert.AreEqual(relativeTransformation.position.range, 20f, _epsilon);
    Assert.AreEqual(relativeTransformation.position.azimuth, 0f, _epsilon);
    Assert.AreEqual(relativeTransformation.position.elevation, 0f, _epsilon);
    Assert.AreEqual(relativeTransformation.velocity.cartesian, _target.Velocity - _agent.Velocity);
    Assert.AreEqual(relativeTransformation.velocity.range, -1f, _epsilon);
    Assert.AreEqual(relativeTransformation.velocity.azimuth, 0f, _epsilon);
    Assert.AreEqual(relativeTransformation.velocity.elevation, 1f, _epsilon);
  }

  [Test]
  public void GetRelativeTransformation_TargetAtStarboard() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 0, 0);
    _target =
        new FixedHierarchical(position: new Vector3(20, 0, 0), velocity: new Vector3(0, 0, 20));

    var relativeTransformation = _agent.GetRelativeTransformation(_target);
    Assert.AreEqual(relativeTransformation.position.cartesian, _target.Position - _agent.Position);
    Assert.AreEqual(relativeTransformation.position.range, 20f, _epsilon);
    Assert.AreEqual(relativeTransformation.position.azimuth, Mathf.PI / 2, _epsilon);
    Assert.AreEqual(relativeTransformation.position.elevation, 0f, _epsilon);
    Assert.AreEqual(relativeTransformation.velocity.cartesian, _target.Velocity - _agent.Velocity);
    Assert.AreEqual(relativeTransformation.velocity.range, 0f, _epsilon);
    Assert.AreEqual(relativeTransformation.velocity.azimuth, -1f, _epsilon);
    Assert.AreEqual(relativeTransformation.velocity.elevation, 0f, _epsilon);
  }

  [Test]
  public void GetRelativeTransformation_TargetWithElevation() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 0, 0);
    _target =
        new FixedHierarchical(position: new Vector3(0, 20, 0), velocity: new Vector3(0, 0, 20));

    var relativeTransformation = _agent.GetRelativeTransformation(_target);
    Assert.AreEqual(relativeTransformation.position.cartesian, _target.Position - _agent.Position);
    Assert.AreEqual(relativeTransformation.position.range, 20f, _epsilon);
    Assert.AreEqual(relativeTransformation.position.azimuth, 0f, _epsilon);
    Assert.AreEqual(relativeTransformation.position.elevation, Mathf.PI / 2, _epsilon);
    Assert.AreEqual(relativeTransformation.velocity.cartesian, _target.Velocity - _agent.Velocity);
    Assert.AreEqual(relativeTransformation.velocity.range, 0f, _epsilon);
    Assert.AreEqual(relativeTransformation.velocity.azimuth, 0f, _epsilon);
    Assert.AreEqual(relativeTransformation.velocity.elevation, -1f, _epsilon);
  }
}
