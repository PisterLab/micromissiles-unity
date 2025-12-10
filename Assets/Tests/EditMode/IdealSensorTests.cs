using NUnit.Framework;
using UnityEngine;

public class IdealSensorTests : TestBase {
  private const float _epsilon = 1e-3f;

  private AgentBase _agent;
  private IdealSensor _sensor;

  [SetUp]
  public void SetUp() {
    _agent = new GameObject("Agent").AddComponent<AgentBase>();
    Rigidbody agentRb = _agent.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(_agent, "Awake");
    _agent.Position = new Vector3(x: -5, y: 2, z: -5);
    _agent.Velocity = new Vector3(x: 2, y: 4, z: 10);
    _sensor = new IdealSensor(_agent);
  }

  [Test]
  public void Sense_TargetAtSamePosition_HandlesGracefully() {
    var target = new FixedHierarchical(position: _agent.Position, velocity: new Vector3(5, 0, 0));
    SensorOutput sensorOutput = _sensor.Sense(target);
    Assert.AreEqual(0f, sensorOutput.Position.Range, _epsilon);
  }

  [Test]
  public void Sense_TargetDirectlyAbove_ReturnsCorrectElevation() {
    var target =
        new FixedHierarchical(position: _agent.Position + Vector3.up * 100, velocity: Vector3.zero);
    SensorOutput sensorOutput = _sensor.Sense(target);
    Assert.AreEqual(Mathf.PI / 2, sensorOutput.Position.Elevation, _epsilon);
  }

  [Test]
  public void Sense_Hierarchical_ReturnsCorrectPositionVelocityAndAcceleration() {
    var target =
        new FixedHierarchical(position: new Vector3(10, 2, 20), velocity: new Vector3(-12, 20, -1));
    Transformation relativeTransformation = _agent.GetRelativeTransformation(target);
    SensorOutput sensorOutput = _sensor.Sense(target);
    Assert.AreEqual(relativeTransformation.Position, sensorOutput.Position);
    Assert.AreEqual(relativeTransformation.Velocity, sensorOutput.Velocity);
    Assert.AreEqual(relativeTransformation.Acceleration, sensorOutput.Acceleration);
  }

  [Test]
  public void Sense_Waypoint_ReturnsCorrectPositionVelocityAndAcceleration() {
    var waypoint = new Vector3(-12, 20, -1);
    Transformation relativeTransformation = _agent.GetRelativeTransformation(waypoint);
    SensorOutput sensorOutput = _sensor.Sense(waypoint);
    Assert.AreEqual(relativeTransformation.Position, sensorOutput.Position);
    Assert.AreEqual(relativeTransformation.Velocity, sensorOutput.Velocity);
    Assert.AreEqual(relativeTransformation.Acceleration, sensorOutput.Acceleration);
  }
}
