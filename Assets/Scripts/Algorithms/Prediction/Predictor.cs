using UnityEngine;

public class PredictorStateLegacy {
  // Position.
  public Vector3 Position { get; }

  // Velocity.
  public Vector3 Velocity { get; }

  // Acceleration.
  public Vector3 Acceleration { get; }

  public PredictorStateLegacy() {}
  public PredictorStateLegacy(Agent agent) {
    Position = agent.GetPosition();
    Velocity = agent.GetVelocity();
    Acceleration = agent.GetAcceleration();
  }
  public PredictorStateLegacy(Vector3 position, Vector3 velocity, Vector3 acceleration) {
    Position = position;
    Velocity = velocity;
    Acceleration = acceleration;
  }
}

// The predictor class is an interface for predicting the trajectories of agents.
public abstract class IPredictorLegacy {
  // Agent state.
  protected PredictorStateLegacy _state;

  public IPredictorLegacy(Agent agent) {
    _state = new PredictorStateLegacy(agent);
  }

  // Predict the state at the given time.
  public abstract PredictorStateLegacy Predict(float time);
}
