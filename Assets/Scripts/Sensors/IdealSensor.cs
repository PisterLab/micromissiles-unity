using UnityEngine;

// Ideal sensor.
//
// The ideal sensor provides perfect, noise-free measurements of the relative transformation between
// the sensing agent and a target. This sensor is useful for testing and establishing baseline
// behavior before adding realistic sensor noise and limitations.
public class IdealSensor : SensorBase {
  public IdealSensor(IAgent agent) : base(agent) {}

  // Sense the target hierarchical object.
  public override SensorOutput Sense(IHierarchical hierarchical) {
    Transformation relativeTransformation = Agent.GetRelativeTransformation(hierarchical);
    return GenerateSensorOutput(relativeTransformation);
  }

  // Sense the target agent.
  public override SensorOutput Sense(IAgent agent) {
    Transformation relativeTransformation = Agent.GetRelativeTransformation(agent);
    return GenerateSensorOutput(relativeTransformation);
  }

  // Sense the waypoint.
  public override SensorOutput Sense(in Vector3 waypoint) {
    Transformation relativeTransformation = Agent.GetRelativeTransformation(waypoint);
    return GenerateSensorOutput(relativeTransformation);
  }

  // Generate the sensor output from the relative transformation.
  private SensorOutput GenerateSensorOutput(in Transformation relativeTransformation) {
    return new SensorOutput {
      Position = relativeTransformation.Position,
      Velocity = relativeTransformation.Velocity,
    };
  }
}
