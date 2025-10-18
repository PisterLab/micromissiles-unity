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
  public void ConvertCartesianToPolar_OnXAxis_ReturnsZeroAzimuth() {
    var cartesian = new Vector2(5, 0);
    var expectedPolar = new Vector2(5.0f, 0.0f);
    Vector2 actualPolar = Coordinates2.ConvertCartesianToPolar(cartesian);
    Assert.That(actualPolar, Is.EqualTo(expectedPolar).Using(Vector2EqualityComparer.Instance));
  }

  [Test]
  public void ConvertCartesianToPolar_OnYAxis_ReturnsNinetyAzimuth() {
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
  public void ConvertPolarToCartesian_ZeroAzimuth_ReturnsOnXAxis() {
    var polar = new Vector2(10, 0);
    var expectedCartesian = new Vector2(10.0f, 0.0f);
    Vector2 actualCartesian = Coordinates2.ConvertPolarToCartesian(polar);
    Assert.That(actualCartesian,
                Is.EqualTo(expectedCartesian).Using(Vector2EqualityComparer.Instance));
  }

  [Test]
  public void ConvertPolarToCartesian_NinetyAzimuth_ReturnsOnYAxis() {
    Vector2 polar = new Vector2(10, 90);
    Vector2 expectedCartesian = new Vector2(0.0f, 10.0f);
    Vector2 actualCartesian = Coordinates2.ConvertPolarToCartesian(polar);
    Assert.That(actualCartesian,
                Is.EqualTo(expectedCartesian).Using(Vector2EqualityComparer.Instance));
  }

  [Test]
  public void ConvertPolarToCartesian_ThirtyAzimuth() {
    var polar = new Vector2(20, 30);
    var expectedCartesian = new Vector2(10 * Mathf.Sqrt(3), 10);
    Vector2 actualCartesian = Coordinates2.ConvertPolarToCartesian(polar);
    Assert.That(actualCartesian,
                Is.EqualTo(expectedCartesian).Using(Vector2EqualityComparer.Instance));
  }

  [Test]
  public void ConvertPolarToCartesian_FortyFiveAzimuth() {
    var polar = new Vector2(10 * Mathf.Sqrt(2), 45);
    var expectedCartesian = new Vector2(10.0f, 10.0f);
    Vector2 actualCartesian = Coordinates2.ConvertPolarToCartesian(polar);
    Assert.That(actualCartesian,
                Is.EqualTo(expectedCartesian).Using(Vector2EqualityComparer.Instance));
  }
}
