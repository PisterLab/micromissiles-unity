using UnityEngine;

// The waypoint controller steers the agent to the target using direct linear guidance.
public class WaypointController : ControllerBase {
  // To prevent overshooting near the waypoint, if the distance to the waypoint is less than the
  // threshold, do not apply any acceleration.
  private const float _accelerationCutoffDistance = 1.0f;

  public WaypointController(IAgent agent) : base(agent) {}

  // Controller-dependent implementation of the control law.
  protected override Vector3 Plan(in Transformation relativeTransformation) {
    Vector3 relativePosition = relativeTransformation.Position.Cartesian;
    if (relativeTransformation.Position.Range < _accelerationCutoffDistance) {
      return Vector3.zero;
    }

    // To reach the waypoint as fast as possible, use the maximum forward acceleration.
    Vector3 forward = Agent.Transform.forward;
    Vector3 forwardAccelerationInput = forward * Agent.MaxForwardAcceleration();
    if (Vector3.Dot(relativePosition, Agent.Velocity) < 0) {
      forwardAccelerationInput *= -1;
    }

    // Steer as hard as possible towards the waypoint.
    Vector3 normalAcceleration = Vector3.ProjectOnPlane(relativePosition, forward).normalized;
    Vector3 normalAccelerationInput = normalAcceleration * Agent.MaxNormalAcceleration();
    return forwardAccelerationInput + normalAccelerationInput;
  }
}
