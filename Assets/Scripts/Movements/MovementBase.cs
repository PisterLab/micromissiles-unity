using System;
using UnityEngine;

// Base implementation of a movement behavior.
public abstract class MovementBase : IMovement {
  // Agent to which the movement behavior is assigned.
  public IAgent Agent { get; set; }

  public MovementBase(IAgent agent) {
    Agent = agent;
  }

  // Determine the agent's actual acceleration input given its intended acceleration input by
  // applying physics and other constraints.
  public abstract Vector3 Act(in Vector3 accelerationInput);

  // Get the maximum forward acceleration from the agent's static configuration.
  public float MaxForwardAcceleration() {
    return Agent.StaticConfig?.AccelerationConfig?.MaxForwardAcceleration ?? 0;
  }

  // Get the maximum normal acceleration from the agent's static configuration.
  public float MaxNormalAcceleration() {
    var maxReferenceNormalAcceleration =
        (Agent.StaticConfig?.AccelerationConfig?.MaxReferenceNormalAcceleration ?? 0) *
        Constants.kGravity;
    var referenceSpeed = Agent.StaticConfig?.AccelerationConfig?.ReferenceSpeed ?? 1;
    return Mathf.Pow(Agent.Speed / referenceSpeed, 2) * maxReferenceNormalAcceleration;
  }

  // Limit acceleration input to the agent's maximum forward and normal accelerations.
  protected Vector3 LimitAccelerationInput(in Vector3 accelerationInput) {
    var forwardAccelerationInput = Vector3.Project(accelerationInput, Agent.transform.forward);
    var normalAccelerationInput = accelerationInput - forwardAccelerationInput;

    // Limit the forward and the normal acceleration magnitude.
    forwardAccelerationInput =
        Vector3.ClampMagnitude(forwardAccelerationInput, MaxForwardAcceleration());
    normalAccelerationInput =
        Vector3.ClampMagnitude(normalAccelerationInput, MaxNormalAcceleration());
    return forwardAccelerationInput + normalAccelerationInput;
  }
}
