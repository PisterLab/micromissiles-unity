using UnityEngine;

// Base implementation of a controller.
public abstract class ControllerBase : IController {
  // Agent that the controller is steering.
  public IAgent Agent { get; init; }

  public ControllerBase(IAgent agent) {
    Agent = agent;
  }

  // Plan the next optimal control to intercept the target.
  public Vector3 Plan() {
    if (Agent.HierarchicalAgent == null || Agent.HierarchicalAgent.TargetModel == null) {
      return Vector3.zero;
    }

    Transformation relativeTransformation =
        Agent.GetRelativeTransformation(Agent.HierarchicalAgent.TargetModel);
    return Plan(relativeTransformation);
  }

  // Plan the next optimal control to the waypoint.
  public Vector3 Plan(in Vector3 waypoint) {
    Transformation relativeTransformation = Agent.GetRelativeTransformation(waypoint);
    return Plan(relativeTransformation);
  }

  // Controller-dependent implementation of the control law.
  protected abstract Vector3 Plan(in Transformation relativeTransformation);
}
