using System;
using UnityEngine;

public abstract class Sensor : MonoBehaviour {
  protected Agent _agent;

  protected virtual void Start() {
    _agent = GetComponent<Agent>();
  }

  public abstract SensorOutput SenseWaypoint(Vector3 waypointPosition);

  /// <summary>
  /// Main sensing method to gather information about a target agent.
  /// </summary>
  /// <param name="target">The agent to sense.</param>
  /// <returns>SensorOutput containing position and velocity data.</returns>
  /// <remarks>
  /// Implementers should:
  /// 1. Call SensePosition to get position data.
  /// 2. Call SenseVelocity to get velocity data.
  /// 3. Combine results into a SensorOutput struct.
  /// </remarks>
  public abstract SensorOutput Sense(Agent target);

  /// <summary>
  /// Calculates the relative position of the target agent.
  /// </summary>
  /// <param name="target">The agent to sense.</param>
  /// <returns>PositionOutput containing range, azimuth, and elevation.</returns>
  /// <remarks>
  /// Implementers should calculate:
  /// - range: Distance to the target (in unity units).
  /// - azimuth: Horizontal angle to the target (in degrees).
  ///   Positive is clockwise from the forward direction.
  /// - elevation: Vertical angle to the target (in degrees).
  ///   Positive is above the horizontal plane.
  /// </remarks>
  protected abstract PositionOutput SensePosition(Agent target);

  protected virtual PositionOutput ComputePositionSensorOutput(Vector3 relativePosition) {
    PositionOutput positionSensorOutput = new PositionOutput();

    // Calculate the distance (range) to the target
    positionSensorOutput.range = relativePosition.magnitude;

    // Calculate azimuth (horizontal angle relative to forward)
    positionSensorOutput.azimuth =
        Vector3.SignedAngle(transform.forward, relativePosition, transform.up);

    // Calculate elevation (vertical angle relative to forward)
    Vector3 flatRelativePosition = Vector3.ProjectOnPlane(relativePosition, transform.up);
    positionSensorOutput.elevation =
        Vector3.SignedAngle(flatRelativePosition, relativePosition, transform.right);

    return positionSensorOutput;
  }

  /// <summary>
  /// Calculates the relative velocity of the target agent.
  /// </summary>
  /// <param name="target">The agent to sense.</param>
  /// <returns>VelocityOutput containing range rate, azimuth rate, and elevation rate.</returns>
  /// <remarks>
  /// Implementers should calculate:
  /// - range: Radial velocity (closing speed) in units/second.
  ///   Positive means the target is moving away.
  /// - azimuth: Rate of change of azimuth in degrees/second.
  ///   Positive means the target is moving clockwise.
  /// - elevation: Rate of change of elevation in degrees/second.
  ///   Positive means the target is moving upwards.
  /// </remarks>
  protected abstract VelocityOutput SenseVelocity(Agent target);

  protected virtual VelocityOutput ComputeVelocitySensorOutput(Vector3 relativePosition,
                                                               Vector3 relativeVelocity) {
    VelocityOutput velocitySensorOutput = new VelocityOutput();

    // Calculate range rate (radial velocity)
    velocitySensorOutput.range = Vector3.Dot(relativeVelocity, relativePosition.normalized);

    // Project relative velocity onto a plane perpendicular to relative position
    Vector3 tangentialVelocity =
        Vector3.ProjectOnPlane(relativeVelocity, relativePosition.normalized);

    // Calculate azimuth rate
    Vector3 horizontalVelocity = Vector3.ProjectOnPlane(tangentialVelocity, transform.up);
    velocitySensorOutput.azimuth =
        Vector3.Dot(horizontalVelocity, transform.right) / relativePosition.magnitude;

    // Calculate elevation rate
    Vector3 verticalVelocity = Vector3.Project(tangentialVelocity, transform.up);
    velocitySensorOutput.elevation = verticalVelocity.magnitude / relativePosition.magnitude;
    if (Vector3.Dot(verticalVelocity, transform.up) < 0) {
      velocitySensorOutput.elevation *= -1;
    }

    return velocitySensorOutput;
  }
}

public struct SensorOutput {
  public PositionOutput position;
  public VelocityOutput velocity;
}

public struct PositionOutput {
  public float range;
  public float azimuth;
  public float elevation;
}

public struct VelocityOutput {
  public float range;
  public float azimuth;
  public float elevation;
}
