using System;
using UnityEngine;

// Base implementation of a movement behavior.
public abstract class MovementBase : IMovement {
  // Agent to which the movement behavior is assigned.
  public IAgent Agent { get; init; }

  public MovementBase(IAgent agent) {
    Agent = agent;
  }

  // Determine the agent's actual acceleration input given its intended acceleration input by
  // applying physics and other constraints.
  public abstract Vector3 Act(in Vector3 accelerationInput);

  // Limit acceleration input to the agent's maximum forward and normal accelerations.
  protected Vector3 LimitAccelerationInput(in Vector3 accelerationInput) {
    Vector3 forwardAccelerationInput = Vector3.Project(accelerationInput, Agent.Transform.forward);
    Vector3 normalAccelerationInput =
        Vector3.ProjectOnPlane(accelerationInput, Agent.Transform.forward);

    // Limit the forward and the normal acceleration magnitude.
    forwardAccelerationInput =
        Vector3.ClampMagnitude(forwardAccelerationInput, Agent.MaxForwardAcceleration());
    normalAccelerationInput =
        Vector3.ClampMagnitude(normalAccelerationInput, Agent.MaxNormalAcceleration());
    return forwardAccelerationInput + normalAccelerationInput;
  }
}
