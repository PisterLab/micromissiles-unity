using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class Coordinates2Tests {
  private const float Epsilon = 0.0001f;

  [Test]
  public void TestCartesianToPolarOrigin() {
    Vector2 cartesian = Vector2.zero;
    Assert.That(Coordinates2.ConvertCartesianToPolar(cartesian).x, Is.EqualTo(0).Within(Epsilon));
  }

  [Test]
  public void TestCartesianToPolarXAxis() {
    Vector2 cartesian = new Vector2(5, 0);
    Vector2 expectedPolar = new Vector2(5.0f, 0.0f);
    Vector2 actualPolar = Coordinates2.ConvertCartesianToPolar(cartesian);
    Assert.That(actualPolar.x, Is.EqualTo(expectedPolar.x).Within(Epsilon));
    Assert.That(actualPolar.y, Is.EqualTo(expectedPolar.y).Within(Epsilon));
  }

  [Test]
  public void TestCartesianToPolarYAxis() {
    Vector2 cartesian = new Vector2(0, 5);
    Vector2 expectedPolar = new Vector2(5.0f, 90.0f);
    Vector2 actualPolar = Coordinates2.ConvertCartesianToPolar(cartesian);
    Assert.That(actualPolar.x, Is.EqualTo(expectedPolar.x).Within(Epsilon));
    Assert.That(actualPolar.y, Is.EqualTo(expectedPolar.y).Within(Epsilon));
  }

  [Test]
  public void TestPolarToCartesianOrigin() {
    Vector2 expectedCartesian = Vector2.zero;
    Vector2[] polarInputs = new[] { new Vector2(0, 0), new Vector2(0, 45), new Vector2(0, 90),
                                    new Vector2(0, 180), new Vector2(0, 360) };

    foreach (var polar in polarInputs) {
      Vector2 actualCartesian = Coordinates2.ConvertPolarToCartesian(polar);
      Assert.That(actualCartesian.x, Is.EqualTo(expectedCartesian.x).Within(Epsilon));
      Assert.That(actualCartesian.y, Is.EqualTo(expectedCartesian.y).Within(Epsilon));
    }
  }

  [Test]
  public void TestPolarToCartesianXAxis() {
    Vector2 polar = new Vector2(10, 0);
    Vector2 expectedCartesian = new Vector2(10.0f, 0.0f);
    Vector2 actualCartesian = Coordinates2.ConvertPolarToCartesian(polar);
    Assert.That(actualCartesian.x, Is.EqualTo(expectedCartesian.x).Within(Epsilon));
    Assert.That(actualCartesian.y, Is.EqualTo(expectedCartesian.y).Within(Epsilon));
  }

  [Test]
  public void TestPolarToCartesianYAxis() {
    Vector2 polar = new Vector2(10, 90);
    Vector2 expectedCartesian = new Vector2(0.0f, 10.0f);
    Vector2 actualCartesian = Coordinates2.ConvertPolarToCartesian(polar);
    Assert.That(actualCartesian.x, Is.EqualTo(expectedCartesian.x).Within(Epsilon));
    Assert.That(actualCartesian.y, Is.EqualTo(expectedCartesian.y).Within(Epsilon));
  }

  [Test]
  public void TestPolarToCartesian30() {
    Vector2 polar = new Vector2(20, 30);
    Vector2 expectedCartesian = new Vector2(10 * Mathf.Sqrt(3), 10);
    Vector2 actualCartesian = Coordinates2.ConvertPolarToCartesian(polar);
    Assert.That(actualCartesian.x, Is.EqualTo(expectedCartesian.x).Within(Epsilon));
    Assert.That(actualCartesian.y, Is.EqualTo(expectedCartesian.y).Within(Epsilon));
  }

  [Test]
  public void TestPolarToCartesian45() {
    Vector2 polar = new Vector2(10 * Mathf.Sqrt(2), 45);
    Vector2 expectedCartesian = new Vector2(10.0f, 10.0f);
    Vector2 actualCartesian = Coordinates2.ConvertPolarToCartesian(polar);
    Assert.That(actualCartesian.x, Is.EqualTo(expectedCartesian.x).Within(Epsilon));
    Assert.That(actualCartesian.y, Is.EqualTo(expectedCartesian.y).Within(Epsilon));
  }
}

public class Coordinates3Tests {
  private const float Epsilon = 0.0001f;

  [Test]
  public void TestCartesianToSphericalOrigin() {
    Vector3 cartesian = Vector3.zero;
    Assert.That(Coordinates3.ConvertCartesianToSpherical(cartesian).x,
                Is.EqualTo(0).Within(Epsilon));
  }

  [Test]
  public void TestCartesianToSpherical() {
    Vector3 cartesian = new Vector3(2, -1, 3);
    Vector3 expectedSpherical = new Vector3(Mathf.Sqrt(14), 33.690068f, -15.501360f);
    Vector3 actualSpherical = Coordinates3.ConvertCartesianToSpherical(cartesian);
    Assert.That(actualSpherical.x, Is.EqualTo(expectedSpherical.x).Within(Epsilon));
    Assert.That(actualSpherical.y, Is.EqualTo(expectedSpherical.y).Within(Epsilon));
    Assert.That(actualSpherical.z, Is.EqualTo(expectedSpherical.z).Within(Epsilon));
  }

  [Test]
  public void TestSphericalToCartesianOrigin() {
    Vector3 spherical = new Vector3(0, 45, 90);
    Vector3 expectedCartesian = Vector3.zero;
    Vector3 actualCartesian = Coordinates3.ConvertSphericalToCartesian(spherical);
    Assert.That(actualCartesian.x, Is.EqualTo(expectedCartesian.x).Within(Epsilon));
    Assert.That(actualCartesian.y, Is.EqualTo(expectedCartesian.y).Within(Epsilon));
    Assert.That(actualCartesian.z, Is.EqualTo(expectedCartesian.z).Within(Epsilon));
  }

  [Test]
  public void TestSphericalToCartesian() {
    Vector3 spherical = new Vector3(10, 60, 30);
    Vector3 expectedCartesian = new Vector3(7.5f, 5.0f, 4.3301270189221945f);
    Vector3 actualCartesian = Coordinates3.ConvertSphericalToCartesian(spherical);
    Assert.That(actualCartesian.x, Is.EqualTo(expectedCartesian.x).Within(Epsilon));
    Assert.That(actualCartesian.y, Is.EqualTo(expectedCartesian.y).Within(Epsilon));
    Assert.That(actualCartesian.z, Is.EqualTo(expectedCartesian.z).Within(Epsilon));
  }

  [Test]
  public void TestCylindricalToCartesianOrigin() {
    Vector3 cylindrical = new Vector3(0, 45, 0);
    Vector3 expectedCartesian = Vector3.zero;
    Vector3 actualCartesian = Coordinates3.ConvertCylindricalToCartesian(cylindrical);
    Assert.That(actualCartesian.x, Is.EqualTo(expectedCartesian.x).Within(Epsilon));
    Assert.That(actualCartesian.y, Is.EqualTo(expectedCartesian.y).Within(Epsilon));
    Assert.That(actualCartesian.z, Is.EqualTo(expectedCartesian.z).Within(Epsilon));
  }

  [Test]
  public void TestCylindricalToCartesian() {
    Vector3 cylindrical = new Vector3(10, 60, 2);
    Vector3 expectedCartesian = new Vector3(8.660254f, 2.0f, 5.0f);
    Vector3 actualCartesian = Coordinates3.ConvertCylindricalToCartesian(cylindrical);
    Assert.That(actualCartesian.x, Is.EqualTo(expectedCartesian.x).Within(Epsilon));
    Assert.That(actualCartesian.y, Is.EqualTo(expectedCartesian.y).Within(Epsilon));
    Assert.That(actualCartesian.z, Is.EqualTo(expectedCartesian.z).Within(Epsilon));
  }

  [Test]
  public void TestCartesianToCylindricalOrigin() {
    Vector3 cartesian = Vector3.zero;
    Assert.That(Coordinates3.ConvertCartesianToCylindrical(cartesian).x,
                Is.EqualTo(0).Within(Epsilon));
  }

  [Test]
  public void TestCartesianToCylindrical() {
    Vector3 cartesian = new Vector3(2, -1, 3);
    Vector3 expectedCylindrical = new Vector3(Mathf.Sqrt(13), 33.6900675f, -1);
    Vector3 actualCylindrical = Coordinates3.ConvertCartesianToCylindrical(cartesian);
    Assert.That(actualCylindrical.x, Is.EqualTo(expectedCylindrical.x).Within(Epsilon));
    Assert.That(actualCylindrical.y, Is.EqualTo(expectedCylindrical.y).Within(Epsilon));
    Assert.That(actualCylindrical.z, Is.EqualTo(expectedCylindrical.z).Within(Epsilon));
  }
}
