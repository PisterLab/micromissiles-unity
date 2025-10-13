using UnityEngine;

// Ideal movement.
//
// Threats can be modeled as moving ideally, i.e., without the influence of drag or gravity.
public class IdealMovement : AerialMovement {
  public IdealMovement(IAgent agent) : base(agent) {}

  // Determine the next movement for the agent by using the agent's controller to calculate the
  // acceleration input.
  // Ideal movement implies no drag or gravity.
  public override void Update(double deltaTime) {
    // Determine the agent's acceleration input using its controller.
    var accelerationInput = Vector3.zero;
    // Ignore drag and gravity.
    Agent.AccelerationInput = accelerationInput;
  }
}
