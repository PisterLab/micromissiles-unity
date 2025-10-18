using UnityEngine;

public class IdealSensorLegacy : SensorLegacy {
  public override SensorOutput Sense(Agent agent) {
    SensorOutput agentSensorOutput = new SensorOutput();

    // Adapt the relative transformation to the agent for the sensor output.
    Transformation relativeTransformation = _agent.GetRelativeTransformation(agent);
    agentSensorOutput.Position = relativeTransformation.Position;
    agentSensorOutput.Velocity = relativeTransformation.Velocity;

    return agentSensorOutput;
  }

  public override SensorOutput SenseWaypoint(Vector3 waypoint) {
    SensorOutput waypointSensorOutput = new SensorOutput();

    // Adapt the agent's relative transformation to the waypoint for the sensor output.
    Transformation relativeTransformation = _agent.GetRelativeTransformationToWaypoint(waypoint);
    waypointSensorOutput.Position = relativeTransformation.Position;
    waypointSensorOutput.Velocity = relativeTransformation.Velocity;

    return waypointSensorOutput;
  }
}
