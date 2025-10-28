using UnityEngine;

public class IdealSensorLegacy : SensorLegacy {
  public override SensorOutput Sense(Agent agent) {
    // Adapt the relative transformation to the agent for the sensor output.
    Transformation relativeTransformation = _agent.GetRelativeTransformation(agent);
    return new SensorOutput {
      Position = relativeTransformation.Position,
      Velocity = relativeTransformation.Velocity,
    };
  }

  public override SensorOutput SenseWaypoint(Vector3 waypoint) {
    // Adapt the agent's relative transformation to the waypoint for the sensor output.
    Transformation relativeTransformation = _agent.GetRelativeTransformationToWaypoint(waypoint);
    return new SensorOutput {
      Position = relativeTransformation.Position,
      Velocity = relativeTransformation.Velocity,
    };
  }
}
