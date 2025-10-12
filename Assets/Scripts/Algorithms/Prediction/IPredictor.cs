// Interface for a predictor.
//
// The predictor predicts the trajectories of hierarchical objects.
public interface IPredictor {
  PredictorState CurrentState { get; }
  PredictorState PredictedState { get; }

  void Predict(float time);
}
