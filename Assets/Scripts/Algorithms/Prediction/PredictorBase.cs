// Base implementation of a predictor.
public abstract class PredictorBase : IPredictor {
  // Predict the future state of the hierarchical object.
  public abstract PredictorState Predict(IHierarchical hierarchical, float time);
}
