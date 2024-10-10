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
  /// - azimuth: Horizontal angle to the target (in radians).
  ///   Positive is clockwise from the forward direction.
  /// - elevation: Vertical angle to the target (in radians).
  ///   Positive is above the horizontal plane.
  /// </remarks>
  protected abstract PositionOutput SensePosition(Agent target);

  protected virtual PositionOutput ComputePositionSensorOutput(Vector3 relativePosition) {
    PositionOutput positionSensorOutput = new PositionOutput();

    // Calculate the distance (range) to the target
    positionSensorOutput.range = relativePosition.magnitude;

    Vector3 flatRelativePosition = Vector3.ProjectOnPlane(relativePosition, transform.up);
    Vector3 verticalRelativePosition = relativePosition - flatRelativePosition;

    // Calculate elevation (vertical angle relative to forward)
    positionSensorOutput.elevation =
        Mathf.Atan(verticalRelativePosition.magnitude / flatRelativePosition.magnitude);

    // Calculate azimuth (horizontal angle relative to forward)
    if (flatRelativePosition.magnitude == 0) {
      positionSensorOutput.azimuth = 0;
    } else {
      positionSensorOutput.azimuth =
          Vector3.SignedAngle(transform.forward, flatRelativePosition, transform.up) * Mathf.PI /
          180;
    }

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
  /// - azimuth: Rate of change of azimuth in radians/second.
  ///   Positive means the target is moving clockwise.
  /// - elevation: Rate of change of elevation in radians/second.
  ///   Positive means the target is moving upwards.
  /// </remarks>
  protected abstract VelocityOutput SenseVelocity(Agent target);

  protected virtual VelocityOutput ComputeVelocitySensorOutput(Vector3 relativePosition,
                                                               Vector3 relativeVelocity) {
    VelocityOutput velocitySensorOutput = new VelocityOutput();

    // Calculate range rate (radial velocity)
    velocitySensorOutput.range = Vector3.Dot(relativeVelocity, relativePosition.normalized);

    // Project relative velocity onto the sphere passing through the target.
    Vector3 tangentialVelocity = Vector3.ProjectOnPlane(relativeVelocity, relativePosition);

    // The target azimuth vector is orthogonal to the relative position vector and
    // points to the starboard of the target along the azimuth-elevation sphere.
    Vector3 targetAzimuth = Vector3.Cross(transform.up, relativePosition);
    // The target elevation vector is orthogonal to the relative position vector
    // and points upwards from the target along the azimuth-elevation sphere.
    Vector3 targetElevation = Vector3.Cross(relativePosition, transform.right);
    // If the relative position vector is parallel to the yaw or pitch axis, the
    // target azimuth vector or the target elevation vector will be undefined.
    if (targetAzimuth.magnitude == 0) {
      targetAzimuth = Vector3.Cross(targetElevation, relativePosition);
    } else if (targetElevation.magnitude == 0) {
      targetElevation = Vector3.Cross(relativePosition, targetAzimuth);
    }

    // Project the relative velocity vector on the azimuth-elevation sphere onto
    // the target azimuth vector.
    Vector3 tangentialVelocityOnAzimuth = Vector3.Project(tangentialVelocity, targetAzimuth);

    // Calculate the time derivative of the azimuth to the target.
    velocitySensorOutput.azimuth =
        tangentialVelocityOnAzimuth.magnitude / relativePosition.magnitude;
    if (Vector3.Dot(tangentialVelocityOnAzimuth, targetAzimuth) < 0) {
      velocitySensorOutput.azimuth *= -1;
    }

    // Project the velocity vector on the azimuth-elevation sphere onto the target
    // elevation vector.
    Vector3 tangentialVelocityOnElevation = Vector3.Project(tangentialVelocity, targetElevation);

    // Calculate the time derivative of the elevation to the target.
    velocitySensorOutput.elevation =
        tangentialVelocityOnElevation.magnitude / relativePosition.magnitude;
    if (Vector3.Dot(tangentialVelocityOnElevation, targetElevation) < 0) {
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
