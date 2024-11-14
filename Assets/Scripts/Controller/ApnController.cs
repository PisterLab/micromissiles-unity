using System;
using UnityEngine;

// The augmented proportional navigation controller applies augmented proportional navigation to
// steer the agent towards its target.
public class ApnController : PnController {
  public ApnController(Agent agent, float navigationGain) : base(agent, navigationGain) {}

  protected override Vector3 PlanImpl(in Transformation relativeTransformation) {
    Vector3 pnAccelerationInput = base.PlanImpl(relativeTransformation);

    // Add a feedforward term proportional to the target's acceleration.
    Vector3 targetAcceleration = relativeTransformation.acceleration.cartesian;
    Vector3 normalTargetAcceleration =
        Vector3.ProjectOnPlane(targetAcceleration, _agent.transform.forward);
    Vector3 accelerationInput =
        pnAccelerationInput + _navigationGain / 2 * normalTargetAcceleration;
    return accelerationInput;
  }
}
