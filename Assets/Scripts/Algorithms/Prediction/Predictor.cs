using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class PredictorState {
  // Snapshot time.
  public float Time;

  // Position.
  public Vector3 Position;

  // Velocity.
  public Vector3 Velocity;

  // Acceleration.
  public Vector3 Acceleration;

  public PredictorState() {}
  public PredictorState(Agent agent, float time) {
    Time = time;
    Position = agent.GetPosition();
    Velocity = agent.GetVelocity();
    Acceleration = agent.GetAcceleration();
  }
}

// The predictor class is an interface for predicting the trajectories of agents.
public abstract class IPredictor {
  // Agent for which to predict the trajectory.
  protected Agent _agent;

  // List of agent states.
  protected List<PredictorState> _states = new List<PredictorState>();

  public IPredictor(in Agent agent) {
    _agent = agent;
  }

  // Get the agent.
  public Agent Agent {
    get { return _agent; }
  }

  // Take a snapshot of the agent state.
  public void SnapState(float time) {
    _states.Add(new PredictorState(_agent, time));
  }

  // Predict the state at the given time.
  public abstract PredictorState Predict(float time);
}
