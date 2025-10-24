using UnityEngine;

// Interface for a controller.
//
// The controller determines the acceleration input given the agent's current state and its intended
// target or waypoint.
public interface IController {
  IAgent Agent { get; set; }

  // Plan the next optimal control to intercept the target.
  Vector3 Plan();

  // Plan the next optimal control to the waypoint.
  Vector3 Plan(in Vector3 waypoint);
}
