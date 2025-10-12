using UnityEngine;

// Base implementation of a controller.
public abstract class ControllerBase : IController {
  // Plan the next optimal control to intercept the target.
  public Vector3 PlanToTarget(IAgent agent) {
    Transformation relativeTransformation =
        agent.GetRelativeTransformation(agent.HierarchicalAgent.TargetModel);
    return Plan(relativeTransformation);
  }

  // Plan the next optimal control to the waypoint.
  public Vector3 PlanToWaypoint(IAgent agent, in Vector3 waypoint) {
    Transformation relativeTransformation = agent.GetRelativeTransformation(waypoint);
    return Plan(relativeTransformation);
  }

  // Controller-dependent implementation of the control law.
  protected abstract Vector3 Plan(in Transformation relativeTransformation);
}
