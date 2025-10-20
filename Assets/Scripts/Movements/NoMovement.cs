using UnityEngine;

// No movement.
//
// The agent is stationary and does not respond to any acceleration input.
public class NoMovement : MovementBase {
  public NoMovement(IAgent agent) : base(agent) {}

  public override Vector3 Act(in Vector3 accelerationInput) {
    return Vector3.zero;
  }
}
