using NUnit.Framework;
using UnityEngine;

public class IdealSensorTests : TestBase {
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
  public void Sense_ReturnsSameAsRelativeTransformation() {
    var target =
        new FixedHierarchical(position: new Vector3(10, 2, 20), velocity: new Vector3(-12, 20, -1));
    var relativeTransformation = _agent.GetRelativeTransformation(target);
    var sensorOutput = _sensor.Sense(target);
    Assert.AreEqual(relativeTransformation.Position, sensorOutput.Position);
    Assert.AreEqual(relativeTransformation.Velocity, sensorOutput.Velocity);
  }

  [Test]
  public void Sense_WaypointReturnsSameAsRelativeTransformation() {
    var target = new Vector3(-12, 20, -1);
    var relativeTransformation = _agent.GetRelativeTransformation(target);
    var sensorOutput = _sensor.Sense(target);
    Assert.AreEqual(relativeTransformation.Position, sensorOutput.Position);
    Assert.AreEqual(relativeTransformation.Velocity, sensorOutput.Velocity);
  }
}
