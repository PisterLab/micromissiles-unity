using UnityEngine;

public class IdealSensor : Sensor {
  public override SensorOutput Sense(Agent agent) {
    SensorOutput agentSensorOutput = new SensorOutput();

    // Adapt the relative transformation to the agent for the sensor output.
    Transformation relativeTransformation = _agent.GetRelativeTransformation(agent);
    agentSensorOutput.position = relativeTransformation.position;
    agentSensorOutput.velocity = relativeTransformation.velocity;

    return agentSensorOutput;
  }

  public override SensorOutput SenseWaypoint(Vector3 waypoint) {
    SensorOutput waypointSensorOutput = new SensorOutput();

    // Adapt the agent's relative transformation to the waypoint for the sensor output.
    Transformation relativeTransformation = _agent.GetRelativeTransformationToWaypoint(waypoint);
    waypointSensorOutput.position = relativeTransformation.position;
    waypointSensorOutput.velocity = relativeTransformation.velocity;

    return waypointSensorOutput;
  }
}
