using UnityEngine;

// The predictor state defines the input and the output of a predictor.
public class PredictorState {
  // Position.
  public Vector3 Position { get; }

  // Velocity.
  public Vector3 Velocity { get; }

  // Acceleration.
  public Vector3 Acceleration { get; }

  public PredictorState(IHierarchical hierarchical)
      : this(hierarchical.Position, hierarchical.Velocity, hierarchical.Acceleration) {}
  public PredictorState(in Vector3 position, in Vector3 velocity, in Vector3 acceleration) {
    Position = position;
    Velocity = velocity;
    Acceleration = acceleration;
  }
}
