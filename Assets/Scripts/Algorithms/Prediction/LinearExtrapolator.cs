using System.Linq;
using UnityEngine;

// The linear extrapolator class predicts the trajectory of an agent by linearly extrapolating its
// current position and velocity.
public class LinearExtrapolator : IPredictor {
  public LinearExtrapolator(in Agent agent) : base(agent) {}

  // Predict the state at the given time.
  public override PredictorState Predict(float time) {
    Vector3 position = _state.Position + _state.Velocity * time;
    return new PredictorState(position, _state.Velocity, _state.Acceleration);
  }
}
