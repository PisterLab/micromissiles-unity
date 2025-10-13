using UnityEngine;

// Rocket movement.
//
// A rocket is defined by having three modes during its flight: ready mode (before it is launched),
// boost mode (when its burner is on), and midcourse mode (after the burner has completed burning).
public class RocketMovement : AerialMovement {
  public RocketMovement(IAgent agent) : base(agent) {}

  // Determine the next movement for the agent by using the agent's controller to calculate the
  // acceleration input.
  public override void Update(double deltaTime) {
    // Step through the READY, BOOST, and MIDCOURSE phases.
    // Determine the agent's acceleration input using its controller and consider drag and gravity.
    var accelerationInput = Vector3.zero;
    Agent.AccelerationInput = accelerationInput;
  }

  private void UpdateReady(double deltaTime) {}
  private void UpdateBoost(double deltaTime) {}
  private void UpdateMidCourse(double deltaTime) {}
}
