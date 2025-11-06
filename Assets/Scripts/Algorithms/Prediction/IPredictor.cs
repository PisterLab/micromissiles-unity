// Interface for a predictor.
//
// The predictor predicts the trajectories of hierarchical objects.
public interface IPredictor {
  // Predict the future state of the hierarchical object.
  PredictorState Predict(IHierarchical hierarchical, float time);
}
