using NUnit.Framework;
using UnityEngine;

public class AgentBaseTests : TestBase {
  private const float _epsilon = 1e-3f;

  private AgentBase _agent;
  private FixedHierarchical _target;

  [SetUp]
  public void SetUp() {
    _agent = new GameObject("Agent").AddComponent<AgentBase>();
    _agent.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(_agent, "Awake");
  }

  [Test]
  public void GetRelativeTransformation_TargetAtBoresight() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 0, 0);
    _target =
        new FixedHierarchical(position: new Vector3(0, 0, 20), velocity: new Vector3(0, 20, -1));

    Transformation relativeTransformation = _agent.GetRelativeTransformation(_target);
    Assert.AreEqual(_target.Position - _agent.Position, relativeTransformation.Position.Cartesian);
    Assert.AreEqual(20f, relativeTransformation.Position.Range, _epsilon);
    Assert.AreEqual(0f, relativeTransformation.Position.Azimuth, _epsilon);
    Assert.AreEqual(0f, relativeTransformation.Position.Elevation, _epsilon);
    Assert.AreEqual(_target.Velocity - _agent.Velocity, relativeTransformation.Velocity.Cartesian);
    Assert.AreEqual(-1f, relativeTransformation.Velocity.Range, _epsilon);
    Assert.AreEqual(0f, relativeTransformation.Velocity.Azimuth, _epsilon);
    Assert.AreEqual(1f, relativeTransformation.Velocity.Elevation, _epsilon);
  }

  [Test]
  public void GetRelativeTransformation_TargetAtStarboard() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 0, 0);
    _target =
        new FixedHierarchical(position: new Vector3(20, 0, 0), velocity: new Vector3(0, 0, 20));

    Transformation relativeTransformation = _agent.GetRelativeTransformation(_target);
    Assert.AreEqual(_target.Position - _agent.Position, relativeTransformation.Position.Cartesian);
    Assert.AreEqual(20f, relativeTransformation.Position.Range, _epsilon);
    Assert.AreEqual(Mathf.PI / 2, relativeTransformation.Position.Azimuth, _epsilon);
    Assert.AreEqual(0f, relativeTransformation.Position.Elevation, _epsilon);
    Assert.AreEqual(_target.Velocity - _agent.Velocity, relativeTransformation.Velocity.Cartesian);
    Assert.AreEqual(0f, relativeTransformation.Velocity.Range, _epsilon);
    Assert.AreEqual(-1f, relativeTransformation.Velocity.Azimuth, _epsilon);
    Assert.AreEqual(0f, relativeTransformation.Velocity.Elevation, _epsilon);
  }

  [Test]
  public void GetRelativeTransformation_TargetWithElevation() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 0, 0);
    _target =
        new FixedHierarchical(position: new Vector3(0, 20, 0), velocity: new Vector3(0, 0, 20));

    Transformation relativeTransformation = _agent.GetRelativeTransformation(_target);
    Assert.AreEqual(_target.Position - _agent.Position, relativeTransformation.Position.Cartesian);
    Assert.AreEqual(20f, relativeTransformation.Position.Range, _epsilon);
    Assert.AreEqual(0f, relativeTransformation.Position.Azimuth, _epsilon);
    Assert.AreEqual(Mathf.PI / 2, relativeTransformation.Position.Elevation, _epsilon);
    Assert.AreEqual(_target.Velocity - _agent.Velocity, relativeTransformation.Velocity.Cartesian);
    Assert.AreEqual(0f, relativeTransformation.Velocity.Range, _epsilon);
    Assert.AreEqual(0f, relativeTransformation.Velocity.Azimuth, _epsilon);
    Assert.AreEqual(-1f, relativeTransformation.Velocity.Elevation, _epsilon);
  }
}
