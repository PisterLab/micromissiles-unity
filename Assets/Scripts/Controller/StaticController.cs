using UnityEngine;

// The static controller always returns zero acceleration.
public class StaticController : ControllerBase {
  public StaticController(IAgent agent) : base(agent) {}

  // Controller-dependent implementation of the control law.
  protected override Vector3 Plan(in Transformation relativeTransformation) {
    return Vector3.zero;
  }
}
