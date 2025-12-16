using UnityEngine;

// The augmented proportional navigation controller applies an input that is proportional to the
// line-of-sight's rotation rate and adds a term to compensate for the target's acceleration.
public class ApnController : PnController {
  public ApnController(IAgent agent, float gain) : base(agent, gain) {}

  // Controller-dependent implementation of the control law.
  protected override Vector3 Plan(in Transformation relativeTransformation) {
    Vector3 accelerationInput = base.Plan(relativeTransformation);

    // Add a feedforward term proportional to the target's acceleration.
    Vector3 targetAcceleration = relativeTransformation.Acceleration.Cartesian;
    Vector3 normalTargetAcceleration = Vector3.ProjectOnPlane(targetAcceleration, Agent.Forward);
    return accelerationInput + Gain / 2 * normalTargetAcceleration;
  }
}
