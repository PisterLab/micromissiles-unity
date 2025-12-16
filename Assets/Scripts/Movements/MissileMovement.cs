using UnityEngine;

// Missile movement.
//
// A missile is defined by having multiple phases during its flight: ready phases (before it is
// launched), boost phases (when its burner is on), midcourse phases (after the burner has completed
// burning), and terminal phases (when the agent is homing in on the target).
//
// We define three additional states for our simulator: initialized state (when the missile has been
// created), ballistic state (when the missile is subject to only drag and gravity), and terminated
// state (when the missile has finished its flight).
//
// The missile is responsible for determining when it enters the boost phase.
public class MissileMovement : AerialMovement {
  // Flight phase of the agent.
  private Simulation.FlightPhase _flightPhase;

  // Boost time in seconds relative to the agent's creation time.
  private float _boostTime;

  public Simulation.FlightPhase FlightPhase {
    get { return _flightPhase; }
    set {
      _flightPhase = value;
      if (FlightPhase == Simulation.FlightPhase.Boost) {
        _boostTime = Agent.ElapsedTime;
      }
    }
  }

  public MissileMovement(IAgent agent) : base(agent) {
    FlightPhase = Simulation.FlightPhase.Initialized;
  }

  // Determine the agent's actual acceleration input given its intended acceleration input by
  // applying physics and other constraints.
  public override Vector3 Act(in Vector3 accelerationInput) {
    // Step through the flight phases.
    if (FlightPhase == Simulation.FlightPhase.Boost &&
        Agent.ElapsedTime > _boostTime + (Agent.StaticConfig?.BoostConfig?.BoostTime ?? 0)) {
      FlightPhase = Simulation.FlightPhase.Midcourse;
    }

    // Act according to the flight phase.
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
        return ActTerminal(accelerationInput);
      }
      case Simulation.FlightPhase.Ballistic: {
        return ActBallistic(accelerationInput);
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
    Vector3 limitedAccelerationInput = LimitAccelerationInput(accelerationInput);
    return CalculateNetAccelerationInput(limitedAccelerationInput);
  }

  // In the boost phase, the boost acceleration is added to the control acceleration input.
  private Vector3 ActBoost(in Vector3 accelerationInput) {
    Vector3 accelerationInputWithGroundAvoidance = AvoidGround(accelerationInput);
    Vector3 limitedAccelerationInput = LimitAccelerationInput(accelerationInputWithGroundAvoidance);

    // Determine the boost acceleration.
    float boostAcceleration =
        (Agent.StaticConfig?.BoostConfig?.BoostAcceleration ?? 0) * Constants.kGravity;
    Vector3 totalAccelerationInput = boostAcceleration * Agent.Forward + limitedAccelerationInput;
    return CalculateNetAccelerationInput(totalAccelerationInput);
  }

  // In the midcourse phase, the agent accelerates according to the control acceleration input but
  // is subject to drag and gravity.
  private Vector3 ActMidCourse(in Vector3 accelerationInput) {
    Vector3 accelerationInputWithGroundAvoidance = AvoidGround(accelerationInput);
    Vector3 limitedAccelerationInput = LimitAccelerationInput(accelerationInputWithGroundAvoidance);
    return CalculateNetAccelerationInput(limitedAccelerationInput);
  }

  // In the terminal phase, the agent is homing in on the target and is still subject to drag and
  // gravity.
  private Vector3 ActTerminal(in Vector3 accelerationInput) {
    // Currently, the agent acts the same in the terminal phase as in the midcourse phase.
    return ActMidCourse(accelerationInput);
  }

  // In the ballistic phase, the agent is subject to only drag and gravity.
  private Vector3 ActBallistic(in Vector3 accelerationInput) {
    return ActMidCourse(accelerationInput: Vector3.zero);
  }

  // Adjust the acceleration input to avoid the ground.
  private Vector3 AvoidGround(in Vector3 accelerationInput) {
    const float groundProximityThresholdFactor = 5f;

    Vector3 agentPosition = Agent.Position;
    Vector3 agentVelocity = Agent.Velocity;
    float altitude = agentPosition.y;
    float groundProximityThreshold =
        Mathf.Abs(agentVelocity.y) * groundProximityThresholdFactor +
        0.5f * Constants.kGravity * groundProximityThresholdFactor * groundProximityThresholdFactor;
    if (agentVelocity.y < 0 && altitude < groundProximityThreshold) {
      // Add some upward acceleration to avoid the ground.
      float blendFactor = 1 - (altitude / groundProximityThreshold);
      return accelerationInput + blendFactor * Agent.MaxNormalAcceleration() * Agent.Up;
    }
    return accelerationInput;
  }

  // Calculate the acceleration input with drag and gravity.
  private Vector3 CalculateNetAccelerationInput(in Vector3 accelerationInput) {
    Vector3 gravity = Physics.gravity;
    float airDrag = CalculateDrag();
    float liftInducedDrag = CalculateLiftInducedDrag(accelerationInput + gravity);
    float dragAcceleration = -(airDrag + liftInducedDrag);
    return accelerationInput + gravity + dragAcceleration * Agent.Forward;
  }
}
