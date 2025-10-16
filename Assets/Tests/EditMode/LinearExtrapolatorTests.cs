using NUnit.Framework;
using UnityEngine;

public class LinearExtrapolatorTests {
  private LinearExtrapolator _predictor;
  private FixedHierarchical _hierarchical;

  [SetUp]
  public void SetUp() {
    _predictor = new LinearExtrapolator();
    _hierarchical =
        new FixedHierarchical(position: new Vector3(10, 0, 5), velocity: new Vector3(1, -2, 2),
                              acceleration: new Vector3(-1, 1, 2));
  }

  [Test]
  public void Predict_AtTimeZero_ReturnsCurrentState() {
    var predictedState = _predictor.Predict(_hierarchical, time: 0f);
    Assert.AreEqual(_hierarchical.Position, predictedState.Position);
    Assert.AreEqual(_hierarchical.Velocity, predictedState.Velocity);
    Assert.AreEqual(_hierarchical.Acceleration, predictedState.Acceleration);
  }

  [Test]
  public void Predict_InThePast_ReturnsExtrapolatedState() {
    var predictedState = _predictor.Predict(_hierarchical, time: -5f);
    Assert.AreEqual(new Vector3(5, 10, -5), predictedState.Position);
    Assert.AreEqual(_hierarchical.Velocity, predictedState.Velocity);
    Assert.AreEqual(_hierarchical.Acceleration, predictedState.Acceleration);
  }

  [Test]
  public void Predict_InTheFuture_ReturnsExtrapolatedState() {
    var predictedState = _predictor.Predict(_hierarchical, time: 10f);
    Assert.AreEqual(new Vector3(20, -20, 25), predictedState.Position);
    Assert.AreEqual(_hierarchical.Velocity, predictedState.Velocity);
    Assert.AreEqual(_hierarchical.Acceleration, predictedState.Acceleration);
  }
}
