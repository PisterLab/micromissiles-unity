using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;

public class Coordinates3Tests {
  private const float _epsilon = 1e-4f;

  [Test]
  public void ConvertCartesianToSpherical_Origin_ReturnsZeroDistance() {
    var cartesian = Vector3.zero;
    var spherical = Coordinates3.ConvertCartesianToSpherical(cartesian);
    Assert.AreEqual(0, spherical.x, _epsilon);
    Assert.IsFalse(float.IsNaN(spherical.y));
    Assert.IsFalse(float.IsNaN(spherical.z));
  }

  [Test]
  public void ConvertCartesianToSpherical_ReturnsCorrectly() {
    var cartesian = new Vector3(2, -1, 3);
    var expectedSpherical = new Vector3(Mathf.Sqrt(14), 33.690068f, -15.501360f);
    var actualSpherical = Coordinates3.ConvertCartesianToSpherical(cartesian);
    Assert.That(actualSpherical,
                Is.EqualTo(expectedSpherical).Using(Vector3EqualityComparer.Instance));
  }

  [Test]
  [TestCase(0f, 0f)]
  [TestCase(45f, 0f)]
  [TestCase(90f, 0f)]
  [TestCase(180f, 0f)]
  [TestCase(360f, 0f)]
  [TestCase(45f, 0f)]
  [TestCase(45f, 45f)]
  [TestCase(45f, 90f)]
  [TestCase(45f, 180f)]
  [TestCase(45f, 360f)]
  public void ConvertSphericalToCartesian_ZeroDistance_ReturnsOrigin(float theta, float phi) {
    var spherical = new Vector3(0, theta, phi);
    var expectedCartesian = Vector3.zero;
    var actualCartesian = Coordinates3.ConvertSphericalToCartesian(spherical);
    Assert.That(actualCartesian,
                Is.EqualTo(expectedCartesian).Using(Vector3EqualityComparer.Instance));
  }

  [Test]
  public void ConvertSphericalToCartesian_ReturnsCorrectly() {
    var spherical = new Vector3(10, 60, 30);
    var expectedCartesian = new Vector3(7.5f, 5.0f, 4.3301270189221945f);
    var actualCartesian = Coordinates3.ConvertSphericalToCartesian(spherical);
    Assert.That(actualCartesian,
                Is.EqualTo(expectedCartesian).Using(Vector3EqualityComparer.Instance));
  }

  [Test]
  public void ConvertCartesianToCylindrical_Origin_ReturnsZeroDistanceZeroHeight() {
    var cartesian = Vector3.zero;
    var cylindrical = Coordinates3.ConvertCartesianToCylindrical(cartesian);
    Assert.AreEqual(0, cylindrical.x, _epsilon);
    Assert.IsFalse(float.IsNaN(cylindrical.y));
    Assert.AreEqual(0, cylindrical.z, _epsilon);
  }

  [Test]
  public void ConvertCartesianToCylindrical_ReturnsCorrectly() {
    var cartesian = new Vector3(2, -1, 3);
    var expectedCylindrical = new Vector3(Mathf.Sqrt(13), 33.6900675f, -1);
    var actualCylindrical = Coordinates3.ConvertCartesianToCylindrical(cartesian);
    Assert.That(actualCylindrical,
                Is.EqualTo(expectedCylindrical).Using(Vector3EqualityComparer.Instance));
  }

  [Test]
  [TestCase(0f)]
  [TestCase(45f)]
  [TestCase(90f)]
  [TestCase(180f)]
  [TestCase(360f)]
  public void ConvertCylindricalToCartesian_ZeroDistance_ZeroHeight_ReturnsOrigin(float theta) {
    var cylindrical = new Vector3(0, theta, 0);
    var expectedCartesian = Vector3.zero;
    var actualCartesian = Coordinates3.ConvertCylindricalToCartesian(cylindrical);
    Assert.That(actualCartesian,
                Is.EqualTo(expectedCartesian).Using(Vector3EqualityComparer.Instance));
  }

  [Test]
  public void ConvertCylindricalToCartesian_ReturnsCorrectly() {
    var cylindrical = new Vector3(10, 60, 2);
    var expectedCartesian = new Vector3(8.660254f, 2.0f, 5.0f);
    var actualCartesian = Coordinates3.ConvertCylindricalToCartesian(cylindrical);
    Assert.That(actualCartesian,
                Is.EqualTo(expectedCartesian).Using(Vector3EqualityComparer.Instance));
  }
}
