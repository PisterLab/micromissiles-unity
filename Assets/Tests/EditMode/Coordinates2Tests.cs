using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;

public class Coordinates2Tests {
  private const float _epsilon = 1e-4f;

  [Test]
  public void ConvertCartesianToPolar_Origin_ReturnsZeroDistance() {
    var cartesian = Vector2.zero;
    Vector2 polar = Coordinates2.ConvertCartesianToPolar(cartesian);
    Assert.AreEqual(0, polar.x, _epsilon);
    Assert.IsFalse(float.IsNaN(polar.y));
  }

  [Test]
  public void ConvertCartesianToPolar_OnXAxis_ReturnsZeroTheta() {
    var cartesian = new Vector2(5, 0);
    var expectedPolar = new Vector2(5.0f, 0.0f);
    Vector2 actualPolar = Coordinates2.ConvertCartesianToPolar(cartesian);
    Assert.That(actualPolar, Is.EqualTo(expectedPolar).Using(Vector2EqualityComparer.Instance));
  }

  [Test]
  public void ConvertCartesianToPolar_OnYAxis_ReturnsNinetyTheta() {
    var cartesian = new Vector2(0, 5);
    var expectedPolar = new Vector2(5.0f, 90.0f);
    Vector2 actualPolar = Coordinates2.ConvertCartesianToPolar(cartesian);
    Assert.That(actualPolar, Is.EqualTo(expectedPolar).Using(Vector2EqualityComparer.Instance));
  }

  [Test]
  [TestCase(0f)]
  [TestCase(45f)]
  [TestCase(90f)]
  [TestCase(180f)]
  [TestCase(360f)]
  public void ConvertPolarToCartesian_ZeroDistance_ReturnsOrigin(float theta) {
    var polar = new Vector2(0, theta);
    var expectedCartesian = Vector2.zero;
    Vector2 actualCartesian = Coordinates2.ConvertPolarToCartesian(polar);
    Assert.That(actualCartesian,
                Is.EqualTo(expectedCartesian).Using(Vector2EqualityComparer.Instance));
  }

  [Test]
  public void ConvertPolarToCartesian_ZeroTheta_ReturnsOnXAxis() {
    var polar = new Vector2(10, 0);
    var expectedCartesian = new Vector2(10.0f, 0.0f);
    Vector2 actualCartesian = Coordinates2.ConvertPolarToCartesian(polar);
    Assert.That(actualCartesian,
                Is.EqualTo(expectedCartesian).Using(Vector2EqualityComparer.Instance));
  }

  [Test]
  public void ConvertPolarToCartesian_NinetyTheta_ReturnsOnYAxis() {
    Vector2 polar = new Vector2(10, 90);
    Vector2 expectedCartesian = new Vector2(0.0f, 10.0f);
    Vector2 actualCartesian = Coordinates2.ConvertPolarToCartesian(polar);
    Assert.That(actualCartesian,
                Is.EqualTo(expectedCartesian).Using(Vector2EqualityComparer.Instance));
  }

  [Test]
  public void ConvertPolarToCartesian_ThirtyTheta() {
    var polar = new Vector2(20, 30);
    var expectedCartesian = new Vector2(10 * Mathf.Sqrt(3), 10);
    Vector2 actualCartesian = Coordinates2.ConvertPolarToCartesian(polar);
    Assert.That(actualCartesian,
                Is.EqualTo(expectedCartesian).Using(Vector2EqualityComparer.Instance));
  }

  [Test]
  public void ConvertPolarToCartesian_FortyFiveTheta() {
    var polar = new Vector2(10 * Mathf.Sqrt(2), 45);
    var expectedCartesian = new Vector2(10.0f, 10.0f);
    Vector2 actualCartesian = Coordinates2.ConvertPolarToCartesian(polar);
    Assert.That(actualCartesian,
                Is.EqualTo(expectedCartesian).Using(Vector2EqualityComparer.Instance));
  }

  [Test]
  public void RoundTrip_CartesianToPolarToCartesian_ReturnsOriginal() {
    var originalCartesian = new Vector2(3.5f, 7.2f);
    Vector2 polar = Coordinates2.ConvertCartesianToPolar(originalCartesian);
    Vector2 resultCartesian = Coordinates2.ConvertPolarToCartesian(polar);
    Assert.That(resultCartesian,
                Is.EqualTo(originalCartesian).Using(Vector2EqualityComparer.Instance));
  }
}
