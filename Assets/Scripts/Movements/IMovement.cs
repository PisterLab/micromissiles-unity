using UnityEngine;

// Interface for a movement behavior.
//
// The movement behavior determines how the agent navigates through the environment, such as whether
// it is aerial or ground-based. The movement behavior also considers drag and gravity.
public interface IMovement {
  IAgent Agent { get; init; }

  // Determine the agent's actual acceleration input given its intended acceleration input by
  // applying physics and other constraints.
  Vector3 Act(in Vector3 accelerationInput);
}
