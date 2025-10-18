using System.Linq;
using UnityEngine;

// The linear extrapolator class predicts the trajectory of an agent by linearly extrapolating its
// current position and velocity.
public class LinearExtrapolator : IPredictorLegacy {
  public LinearExtrapolator(in Agent agent) : base(agent) {}

  // Predict the state at the given time.
  public override PredictorStateLegacy Predict(float time) {
    Vector3 position = _state.Position + _state.Velocity * time;
    return new PredictorStateLegacy(position, _state.Velocity, _state.Acceleration);
  }
}
