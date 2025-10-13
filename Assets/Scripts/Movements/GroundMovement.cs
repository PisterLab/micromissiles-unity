using UnityEngine;

// Ground-based movement.
//
// The agent is confined to only move along the y=0 plane.
public class GroundMovement : MovementBase {
  public GroundMovement(IAgent agent) : base(agent) {}

  public override void Update(double deltaTime) {
    // Determine the agent's acceleration input using its controller.
    var accelerationInput = Vector3.zero;
    // There is drag along the ground and ignore gravity.
    Agent.AccelerationInput = accelerationInput;
  }
}
