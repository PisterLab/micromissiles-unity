using UnityEngine;

public class PredictorState {
  // Position.
  public Vector3 Position { get; }

  // Velocity.
  public Vector3 Velocity { get; }

  // Acceleration.
  public Vector3 Acceleration { get; }

  public PredictorState() {}
  public PredictorState(Agent agent) {
    Position = agent.GetPosition();
    Velocity = agent.GetVelocity();
    Acceleration = agent.GetAcceleration();
  }
  public PredictorState(Vector3 position, Vector3 velocity, Vector3 acceleration) {
    Position = position;
    Velocity = velocity;
    Acceleration = acceleration;
  }
}

// The predictor class is an interface for predicting the trajectories of agents.
public abstract class IPredictor {
  // Agent state.
  protected PredictorState _state;

  public IPredictor(in Agent agent) {
    _state = new PredictorState(agent);
  }

  // Predict the state at the given time.
  public abstract PredictorState Predict(float time);
}
