using System;
using UnityEngine;

// The controller class is an interface between the agent and its control law.
public class IControllerLegacy {
  // Agent that the controller is controlling.
  protected Agent _agent;

  public IControllerLegacy(Agent agent) {
    _agent = agent;
  }

  // Plan the next optimal control to intercept the target.
  public Vector3 Plan() {
    Transformation relativeTransformation =
        _agent.GetRelativeTransformation(_agent.GetTargetModel());
    return PlanImpl(relativeTransformation);
  }

  // Plan the next optimal control to the waypoint.
  public Vector3 PlanToWaypoint(Vector3 waypoint) {
    Transformation relativeTransformation = _agent.GetRelativeTransformationToWaypoint(waypoint);
    return PlanImpl(relativeTransformation);
  }

  // Controller-dependent implementation of the control law.
  protected virtual Vector3 PlanImpl(in Transformation relativeTransformation) {
    return Vector3.zero;
  }
}
