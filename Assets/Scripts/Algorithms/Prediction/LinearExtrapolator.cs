using System.Linq;
using UnityEngine;

// The linear extrapolator class predicts the trajectory of an agent by linearly extrapolating its
// current position and velocity.
public class LinearExtrapolator : PredictorBase {
  // Predict the future state of the hierarchical object by linearly extrapolating its current
  // position and velocity.
  public override PredictorState Predict(IHierarchical hierarchical, float time) {
    var position = hierarchical.Position + hierarchical.Velocity * time;
    return new PredictorState {
      Position = position,
      Velocity = hierarchical.Velocity,
      Acceleration = hierarchical.Acceleration,
    };
  }
}
