// Base implementation of a predictor.
public abstract class PredictorBase : IPredictor {
  public IHierarchical Hierarchical { get; set; }

  public PredictorBase(IHierarchical hierarchical) {
    Hierarchical = hierarchical;
  }

  // Predict the future state of the hierarchical object.
  public abstract PredictorState Predict(float time);
}
