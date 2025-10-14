using UnityEngine;

// Base implementation of a sensor.
public abstract class SensorBase : ISensor {
  // Agent to which the sensor belongs.
  public IAgent Agent { get; set; }

  public SensorBase(IAgent agent) {
    Agent = agent;
  }

  // Sense the target hierarchical object.
  public abstract SensorOutput Sense(IHierarchical hierarchical);

  // Sense the waypoint.
  public abstract SensorOutput Sense(in Vector3 waypoint);
}
