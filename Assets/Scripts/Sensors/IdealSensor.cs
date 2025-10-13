using UnityEngine;

// Ideal sensor.
//
// The ideal sensor does not add any noise to the measurements.
public class IdealSensor : SensorBase {
  public IdealSensor(IAgent agent) : base(agent) {}

  // Sense the target hierarchical object.
  public override SensorOutput Sense(IHierarchical hierarchical) {
    var relativeTransformation = Agent.GetRelativeTransformation(hierarchical);
    return GenerateSensorOutput(relativeTransformation);
  }

  // Sense the waypoint.
  public override SensorOutput Sense(in Vector3 waypoint) {
    var relativeTransformation = Agent.GetRelativeTransformation(waypoint);
    return GenerateSensorOutput(relativeTransformation);
  }

  // Generate the sensor output from the relative transformation.
  private SensorOutput GenerateSensorOutput(in Transformation relativeTransformation) {
    var sensorOutput = new SensorOutput();
    sensorOutput.Position = relativeTransformation.Position;
    sensorOutput.Velocity = relativeTransformation.Velocity;
    return sensorOutput;
  }
}
