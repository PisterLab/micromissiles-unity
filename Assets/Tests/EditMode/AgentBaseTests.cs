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

    Transformation relativeTransformation = _agent.GetRelativeTransformation(_target);
    Assert.AreEqual(relativeTransformation.Position.Cartesian,
                    _target.Position - _agent.transform.position);
    Assert.AreEqual(relativeTransformation.Position.Range, 20f, _epsilon);
    Assert.AreEqual(relativeTransformation.Position.Azimuth, 0f, _epsilon);
    Assert.AreEqual(relativeTransformation.Position.Elevation, 0f, _epsilon);
    Assert.AreEqual(relativeTransformation.Velocity.Cartesian, _target.Velocity - _agent.Velocity);
    Assert.AreEqual(relativeTransformation.Velocity.Range, -1f, _epsilon);
    Assert.AreEqual(relativeTransformation.Velocity.Azimuth, 0f, _epsilon);
    Assert.AreEqual(relativeTransformation.Velocity.Elevation, 1f, _epsilon);
  }

  [Test]
  public void GetRelativeTransformation_TargetAtStarboard() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 0, 0);
    _target =
        new FixedHierarchical(position: new Vector3(20, 0, 0), velocity: new Vector3(0, 0, 20));

    Transformation relativeTransformation = _agent.GetRelativeTransformation(_target);
    Assert.AreEqual(relativeTransformation.Position.Cartesian,
                    _target.Position - _agent.transform.position);
    Assert.AreEqual(relativeTransformation.Position.Range, 20f, _epsilon);
    Assert.AreEqual(relativeTransformation.Position.Azimuth, Mathf.PI / 2, _epsilon);
    Assert.AreEqual(relativeTransformation.Position.Elevation, 0f, _epsilon);
    Assert.AreEqual(relativeTransformation.Velocity.Cartesian, _target.Velocity - _agent.Velocity);
    Assert.AreEqual(relativeTransformation.Velocity.Range, 0f, _epsilon);
    Assert.AreEqual(relativeTransformation.Velocity.Azimuth, -1f, _epsilon);
    Assert.AreEqual(relativeTransformation.Velocity.Elevation, 0f, _epsilon);
  }

  [Test]
  public void GetRelativeTransformation_TargetWithElevation() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 0, 0);
    _target =
        new FixedHierarchical(position: new Vector3(0, 20, 0), velocity: new Vector3(0, 0, 20));

    Transformation relativeTransformation = _agent.GetRelativeTransformation(_target);
    Assert.AreEqual(relativeTransformation.Position.Cartesian,
                    _target.Position - _agent.transform.position);
    Assert.AreEqual(relativeTransformation.Position.Range, 20f, _epsilon);
    Assert.AreEqual(relativeTransformation.Position.Azimuth, 0f, _epsilon);
    Assert.AreEqual(relativeTransformation.Position.Elevation, Mathf.PI / 2, _epsilon);
    Assert.AreEqual(relativeTransformation.Velocity.Cartesian, _target.Velocity - _agent.Velocity);
    Assert.AreEqual(relativeTransformation.Velocity.Range, 0f, _epsilon);
    Assert.AreEqual(relativeTransformation.Velocity.Azimuth, 0f, _epsilon);
    Assert.AreEqual(relativeTransformation.Velocity.Elevation, -1f, _epsilon);
  }
}
