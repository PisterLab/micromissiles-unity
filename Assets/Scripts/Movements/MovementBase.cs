using System;
using UnityEngine;

// Base implementation of a movement behavior.
public abstract class MovementBase : IMovement {
  // Agent to which the movement behavior is assigned.
  private IAgent _agent;

  public IAgent Agent {
    get => _agent;
    set => _agent = value;
  }

  public MovementBase(IAgent agent) {
    _agent = agent;
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
    float maxReferenceNormalAcceleration =
        (Agent.StaticConfig?.AccelerationConfig?.MaxReferenceNormalAcceleration ?? 0) *
        Constants.kGravity;
    float referenceSpeed = Agent.StaticConfig?.AccelerationConfig?.ReferenceSpeed ?? 1;
    return Mathf.Pow(Agent.Speed / referenceSpeed, 2) * maxReferenceNormalAcceleration;
  }

  // Limit acceleration input to the agent's maximum forward and normal accelerations.
  protected Vector3 LimitAccelerationInput(in Vector3 accelerationInput) {
    Vector3 forwardAccelerationInput = Vector3.Project(accelerationInput, Agent.transform.forward);
    Vector3 normalAccelerationInput = accelerationInput - forwardAccelerationInput;

    // Limit the forward and the normal acceleration magnitude.
    forwardAccelerationInput =
        Vector3.ClampMagnitude(forwardAccelerationInput, MaxForwardAcceleration());
    normalAccelerationInput =
        Vector3.ClampMagnitude(normalAccelerationInput, MaxNormalAcceleration());
    return forwardAccelerationInput + normalAccelerationInput;
  }
}
