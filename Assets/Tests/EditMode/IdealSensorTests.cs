using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using System.Linq;

public class IdealSensorTests : SensorTestBase {
  [Test]
  public void Sense_Target_At_Boresight() {
    // Create sensor
    IdealSensor sensor = new GameObject("Sensor").AddComponent<IdealSensor>();
    sensor.gameObject.AddComponent<Rigidbody>();
    sensor.transform.position = new Vector3(0, 0, 0);

    // Create target
    DummyAgent target = new GameObject("Target").AddComponent<DummyAgent>();
    target.SetPosition(new Vector3(0, 0, 20));
    Rigidbody rb = target.gameObject.AddComponent<Rigidbody>();
    rb.linearVelocity = new Vector3(0, 20, -1);

    // Sense the target
    SensorOutput sensorOutput = sensor.Sense(target);

    // Assert
    Assert.AreEqual(sensorOutput.position.range, 20);
    Assert.AreEqual(sensorOutput.position.azimuth, 0);
    Assert.AreEqual(sensorOutput.position.elevation, 0);
    Assert.AreEqual(sensorOutput.velocity.range, -1);
    Assert.AreEqual(sensorOutput.velocity.azimuth, 0);
    Assert.AreEqual(sensorOutput.velocity.elevation, 1);
  }

  [Test]
  public void Sense_Target_At_Starboard() {
    // Create sensor
    IdealSensor sensor = new GameObject("Sensor").AddComponent<IdealSensor>();
    sensor.gameObject.AddComponent<Rigidbody>();
    sensor.transform.position = new Vector3(0, 0, 0);

    // Create target
    DummyAgent target = new GameObject("Target").AddComponent<DummyAgent>();
    target.SetPosition(new Vector3(20, 0, 0));
    Rigidbody rb = target.gameObject.AddComponent<Rigidbody>();
    rb.linearVelocity = new Vector3(0, 0, 20);

    // Sense the target
    SensorOutput sensorOutput = sensor.Sense(target);

    // Assert
    Assert.AreEqual(sensorOutput.position.range, 20);
    Assert.AreEqual(sensorOutput.position.azimuth, Mathf.PI / 2);
    Assert.AreEqual(sensorOutput.position.elevation, 0);
    Assert.AreEqual(sensorOutput.velocity.range, 0);
    Assert.AreEqual(sensorOutput.velocity.azimuth, -1);
    Assert.AreEqual(sensorOutput.velocity.elevation, 0);
  }

  [Test]
  public void Sense_Target_At_Elevation() {
    // Create sensor
    IdealSensor sensor = new GameObject("Sensor").AddComponent<IdealSensor>();
    sensor.gameObject.AddComponent<Rigidbody>();
    sensor.transform.position = new Vector3(0, 0, 0);

    // Create target
    DummyAgent target = new GameObject("Target").AddComponent<DummyAgent>();
    target.SetPosition(new Vector3(0, 20, 0));
    Rigidbody rb = target.gameObject.AddComponent<Rigidbody>();
    rb.linearVelocity = new Vector3(0, 0, 20);

    // Sense the target
    SensorOutput sensorOutput = sensor.Sense(target);

    // Assert
    Assert.AreEqual(sensorOutput.position.range, 20);
    Assert.AreEqual(sensorOutput.position.azimuth, 0);
    Assert.AreEqual(sensorOutput.position.elevation, Mathf.PI / 2);
    Assert.AreEqual(sensorOutput.velocity.range, 0);
    Assert.AreEqual(sensorOutput.velocity.azimuth, 0);
    Assert.AreEqual(sensorOutput.velocity.elevation, -1);
  }
}
