using System;
using UnityEngine;

public abstract class Sensor : MonoBehaviour {
  protected Agent _agent;

  protected virtual void Start() {
    _agent = GetComponent<Agent>();
  }

  /// <summary>
  /// Main sensing method to gather information about a target agent.
  /// </summary>
  /// <param name="agent">The agent to sense.</param>
  /// <returns>SensorOutput containing position and velocity data.</returns>
  public abstract SensorOutput Sense(Agent agent);

  /// <summary>
  /// Main sensing method to gather information about a waypoint.
  /// </summary>
  /// <param name="waypoint">The waypoint to sense.</param>
  /// <returns>SensorOutput containing position and velocity data.</returns>
  public abstract SensorOutput SenseWaypoint(Vector3 waypoint);
}

public struct SensorOutput {
  public PositionTransformation position;
  public VelocityTransformation velocity;
}
