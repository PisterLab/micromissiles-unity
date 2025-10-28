using UnityEngine;

// The linear extrapolator class predicts the trajectory of an agent by linearly extrapolating its
// current position and velocity.
public class LinearExtrapolator : PredictorBase {
  public LinearExtrapolator(IHierarchical hierarchical) : base(hierarchical) {}

  // Predict the future state of the hierarchical object by linearly extrapolating its current
  // position and velocity.
  public override PredictorState Predict(float time) {
    if (Hierarchical == null) {
      return new PredictorState();
    }

    Vector3 position = Hierarchical.Position + Hierarchical.Velocity * time;
    return new PredictorState {
      Position = position,
      Velocity = Hierarchical.Velocity,
      Acceleration = Hierarchical.Acceleration,
    };
  }
}
