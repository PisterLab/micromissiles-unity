using System;
using UnityEngine;

public abstract class Sensor : MonoBehaviour {
  protected Agent _agent;

  protected virtual void Start() {
    _agent = GetComponent<Agent>();
  }

  /// Main sensing method to gather information about a target agent.
  /// Returns the sensor output containing the sensed position and velocity.
  public abstract SensorOutput Sense(Agent agent);

  /// Main sensing method to gather information about a waypoint.
  /// Returns the sensor output containing the sensed position and velocity.
  public abstract SensorOutput SenseWaypoint(Vector3 waypoint);
}

public struct SensorOutput {
  public PositionTransformation position;
  public VelocityTransformation velocity;
}
