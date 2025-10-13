using UnityEngine;

// Rocket movement.
//
// A rocket is defined by having three modes during its flight: ready mode (before it is launched),
// boost mode (when its burner is on), and midcourse mode (after the burner has completed burning).
public class RocketMovement : AerialMovement {
  // Flight phase of the agent.
  public Simulation.FlightPhase FlightPhase { get; set; }

  public RocketMovement(IAgent agent) : base(agent) {
    FlightPhase = Simulation.FlightPhase.Initialized;
  }

  // Determine the agent's actual acceleration input given its intended acceleration input by
  // applying physics and other constraints.
  public override Vector3 Act(in Vector3 accelerationInput) {
    switch (FlightPhase) {
      case Simulation.FlightPhase.Initialized: {
        // In the initialized phase, the agent is not subject to any acceleration.
        return Vector3.zero;
      }
      case Simulation.FlightPhase.Ready: {
        return ActReady(accelerationInput);
      }
      case Simulation.FlightPhase.Boost: {
        return ActBoost(accelerationInput);
      }
      case Simulation.FlightPhase.Midcourse: {
        return ActMidCourse(accelerationInput);
      }
      case Simulation.FlightPhase.Terminal: {
        //
        return ActMidCourse(accelerationInput);
      }
      case Simulation.FlightPhase.Terminated: {
        // In the terminated phase, the agent is not subject to any acceleration.
        return Vector3.zero;
      }
      default: {
        return Vector3.zero;
      }
    }
  }

  // In the ready phase, the agent is subject to drag and gravity but has not boosted yet.
  private Vector3 ActReady(in Vector3 accelerationInput) {
    var limitedAccelerationInput = LimitAccelerationInput(accelerationInput);
    return CalculateNetAccelerationInput(limitedAccelerationInput);
  }

  // In the boost phase, the boost acceleration is added to the control acceleration input.
  private Vector3 ActBoost(in Vector3 accelerationInput) {
    var limitedAccelerationInput = LimitAccelerationInput(accelerationInput);

    // Determine the boost acceleration.
    var boostAcceleration =
        (Agent.StaticConfig?.BoostConfig?.BoostAcceleration ?? 0) * Constants.kGravity;
    var totalAccelerationInput =
        boostAcceleration * Agent.transform.forward + limitedAccelerationInput;
    return CalculateNetAccelerationInput(totalAccelerationInput);
  }

  // In the midcourse phase, the agent accelerates according to the control acceleration input but
  // is subject to drag and gravity.
  private Vector3 ActMidCourse(in Vector3 accelerationInput) {
    var limitedAccelerationInput = LimitAccelerationInput(accelerationInput);
    return CalculateNetAccelerationInput(limitedAccelerationInput);
  }

  // Calculate the acceleration input with drag and gravity.
  private Vector3 CalculateNetAccelerationInput(in Vector3 accelerationInput) {
    var gravity = Physics.gravity;
    var airDrag = CalculateDrag();
    var liftInducedDrag = CalculateLiftInducedDrag(accelerationInput + Physics.gravity);
    var dragAcceleration = -(airDrag + liftInducedDrag);
    return accelerationInput + gravity + dragAcceleration * Agent.transform.forward;
  }
}
