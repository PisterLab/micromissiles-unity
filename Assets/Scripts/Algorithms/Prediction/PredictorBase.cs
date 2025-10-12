// Base implementation of a predictor.
public abstract class PredictorBase : IPredictor {
  // Current state.
  protected PredictorState _currentState;

  // Predicted state.
  protected PredictorState _predictedState;

  public PredictorState CurrentState => _currentState;
  public PredictorState PredictedState => _predictedState;

  public PredictorBase(IHierarchical hierarchical) {
    _currentState = new PredictorState(hierarchical);
  }

  // Predict the state at the given time.
  public abstract void Predict(float time);
}
