using System.Linq;
using UnityEngine;

// The linear extrapolator class predicts the trajectory of an agent by linearly extrapolating its
// current position and velocity.
public class LinearExtrapolator : IPredictor {
  public LinearExtrapolator(in Agent agent) : base(agent) {}

  // Predict the state at the given time.
  public override PredictorState Predict(float time) {
    if (_states.Count == 0) {
      Debug.LogError("No states available to extrapolate.");
      return new PredictorState();
    }

    PredictorState lastState = _states.LastOrDefault();
    PredictorState predictedState = new PredictorState();
    predictedState.Position = lastState.Position + lastState.Velocity * (time - lastState.Time);
    predictedState.Velocity = lastState.Velocity;
    return predictedState;
  }
}
