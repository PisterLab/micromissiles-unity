using UnityEngine;

// The waypoint controller steers the agent to the target using direct linear guidance.
public class WaypointController : ControllerBase {
  public WaypointController(IAgent agent) : base(agent) {}

  // Controller-dependent implementation of the control law.
  protected override Vector3 Plan(in Transformation relativeTransformation) {
    var relativePosition = relativeTransformation.Position.Cartesian;

    // To reach the waypoint as fast as possible, use the maximum forward acceleration.
    var forward = Agent.transform.forward;
    var forwardAccelerationInput = forward * Agent.MaxForwardAcceleration();

    // Steer as hard as possible towards the waypoint.
    var normalAcceleration = Vector3.ProjectOnPlane(relativePosition, forward).normalized;
    var normalAccelerationInput = normalAcceleration * Agent.MaxNormalAcceleration();
    return forwardAccelerationInput + normalAccelerationInput;
  }
}
