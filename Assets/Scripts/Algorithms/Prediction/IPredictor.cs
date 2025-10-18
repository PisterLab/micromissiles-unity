// Interface for a predictor.
//
// The predictor predicts the trajectories of hierarchical objects.
public interface IPredictor {
  IHierarchical Hierarchical { get; init; }

  // Predict the future state of the hierarchical object.
  PredictorState Predict(float time);
}
