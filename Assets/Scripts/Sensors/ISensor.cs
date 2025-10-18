using UnityEngine;

// Interface for a sensor.
//
// The sensor finds the relative transformation from an agent to another agent or another waypoint.
public interface ISensor {
  IAgent Agent { get; set; }

  // Sense the target hierarchical object.
  SensorOutput Sense(IHierarchical hierarchical);

  // Sense the waypoint.
  SensorOutput Sense(in Vector3 waypoint);
}
