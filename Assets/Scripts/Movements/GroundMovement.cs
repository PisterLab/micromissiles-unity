using UnityEngine;

// Ground-based movement.
//
// The agent is confined to only move along the x-z plane.
public class GroundMovement : MovementBase {
  public GroundMovement(IAgent agent) : base(agent) {}

  // Determine the agent's actual acceleration input given its intended acceleration input by
  // applying physics and other constraints. Ensure that the agent moves along the x-z plane and
  // ignore drag and gravity along the ground.
  public override Vector3 Act(in Vector3 accelerationInput) {
    // Ensure that there is no acceleration out of the x-z plane.
    var constrainedAccelerationInput = accelerationInput;
    constrainedAccelerationInput.y = 0;
    return LimitAccelerationInput(constrainedAccelerationInput);
  }
}
