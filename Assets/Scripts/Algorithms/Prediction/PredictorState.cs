using UnityEngine;

// The predictor state defines the output of a predictor.
public struct PredictorState {
  // Position.
  public Vector3 Position { get; init; }

  // Velocity.
  public Vector3 Velocity { get; init; }

  // Acceleration.
  public Vector3 Acceleration { get; init; }
}
