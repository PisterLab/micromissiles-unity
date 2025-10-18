using System;
using UnityEngine;

// The waypoint controller steers the agent to the target using direct linear guidance.
public class WaypointController : IControllerLegacy {
  // Desired speed in m/s.
  float _desiredSpeed;

  public WaypointController(Agent agent, float desiredSpeed) : base(agent) {
    _desiredSpeed = desiredSpeed;
  }

  protected override Vector3 PlanImpl(in Transformation relativeTransformation) {
    Vector3 toWaypoint = relativeTransformation.Position.Cartesian;
    Vector3 desiredVelocity = toWaypoint.normalized * _desiredSpeed;

    // Calculate the acceleration needed to reach the desired velocity within one time step.
    Vector3 accelerationInput =
        (desiredVelocity - _agent.GetVelocity()) / (float)Time.fixedDeltaTime;
    return accelerationInput;
  }
}
