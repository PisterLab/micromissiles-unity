using UnityEngine;

// Ideal movement.
//
// Threats can be modeled as moving ideally, i.e., without the influence of drag or gravity.
public class IdealMovement : AerialMovement {
  public IdealMovement(IAgent agent) : base(agent) {}

  // Determine the agent's actual acceleration input given its intended acceleration input by
  // applying physics and other constraints. Ideal movement implies no drag or gravity, but the
  // agent may be limited by its own maximum forward and normal accelerations.
  public override Vector3 Act(in Vector3 accelerationInput) {
    return LimitAccelerationInput(accelerationInput);
  }
}
