using UnityEngine;

public class IdealSensor : Sensor {
  protected override void Start() {
    base.Start();
  }

  public override SensorOutput SenseWaypoint(Vector3 waypointPosition) {
    SensorOutput waypointSensorOutput = new SensorOutput();

    Vector3 relativePosition = waypointPosition - transform.position;
    Vector3 relativeVelocity = Vector3.zero - GetComponent<Rigidbody>().linearVelocity;

    // Sense the waypoint's position
    PositionOutput waypointPositionSensorOutput = ComputePositionSensorOutput(relativePosition);
    waypointSensorOutput.position = waypointPositionSensorOutput;

    // Sense the waypoint's velocity
    VelocityOutput waypointVelocitySensorOutput = ComputeVelocitySensorOutput(relativePosition, relativeVelocity);
    waypointSensorOutput.velocity = waypointVelocitySensorOutput;

    return waypointSensorOutput;
  }

  public override SensorOutput Sense(Agent target) {
    SensorOutput targetSensorOutput = new SensorOutput();

    // Sense the target's position
    PositionOutput targetPositionSensorOutput = SensePosition(target);
    targetSensorOutput.position = targetPositionSensorOutput;

    // Sense the target's velocity
    VelocityOutput targetVelocitySensorOutput = SenseVelocity(target);
    targetSensorOutput.velocity = targetVelocitySensorOutput;

    return targetSensorOutput;
  }

  protected override PositionOutput SensePosition(Agent target) {

    // Calculate the relative position of the target
    Vector3 relativePosition = target.transform.position - transform.position;

    return ComputePositionSensorOutput(relativePosition);
  }

  protected override VelocityOutput SenseVelocity(Agent target) {

    // Calculate relative position and velocity
    Vector3 relativePosition = target.transform.position - transform.position;
    Vector3 relativeVelocity = target.GetVelocity() - GetComponent<Rigidbody>().linearVelocity;

    return ComputeVelocitySensorOutput(relativePosition, relativeVelocity);
  }
}
