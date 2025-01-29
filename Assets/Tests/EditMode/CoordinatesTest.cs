using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class Coordinates2Test {
  [Test]
  public void TestCartesianToPolarOrigin() {
    Vector2 cartesian = Vector2.zero;
    Assert.AreEqual(0, Coordinates2.ConvertCartesianToPolar(cartesian).x);
  }

  [Test]
  public void TestCartesianToPolarXAxis() {
    Vector2 cartesian = new Vector2(5, 0);
    Vector2 expectedPolar = new Vector2(5.0f, 0.0f);
    Assert.AreEqual(expectedPolar, Coordinates2.ConvertCartesianToPolar(cartesian));
  }

  [Test]
  public void TestCartesianToPolarYAxis() {
    Vector2 cartesian = new Vector2(0, 5);
    Vector2 expectedPolar = new Vector2(5.0f, 90.0f);
    Assert.AreEqual(expectedPolar, Coordinates2.ConvertCartesianToPolar(cartesian));
  }

  [Test]
  public void TestPolarToCartesianOrigin() {
    Vector2 expectedCartesian = Vector2.zero;
    Vector2 polar1 = new Vector2(0, 0);
    Assert.AreEqual(expectedCartesian, Coordinates2.ConvertPolarToCartesian(polar1));
    Vector2 polar2 = new Vector2(0, 45);
    Assert.AreEqual(expectedCartesian, Coordinates2.ConvertPolarToCartesian(polar2));
    Vector2 polar3 = new Vector2(0, 90);
    Assert.AreEqual(expectedCartesian, Coordinates2.ConvertPolarToCartesian(polar3));
    Vector2 polar4 = new Vector2(0, 180);
    Assert.AreEqual(expectedCartesian, Coordinates2.ConvertPolarToCartesian(polar4));
    Vector2 polar5 = new Vector2(0, 360);
    Assert.AreEqual(expectedCartesian, Coordinates2.ConvertPolarToCartesian(polar5));
  }

  [Test]
  public void TestPolarToCartesianXAxis() {
    Vector2 polar = new Vector2(10, 0);
    Vector2 expectedCartesian = new Vector2(10.0f, 0.0f);
    Assert.AreEqual(expectedCartesian, Coordinates2.ConvertPolarToCartesian(polar));
  }

  [Test]
  public void TestPolarToCartesianYAxis() {
    Vector2 polar = new Vector2(10, 90);
    Vector2 expectedCartesian = new Vector2(0.0f, 10.0f);
    Assert.AreEqual(expectedCartesian, Coordinates2.ConvertPolarToCartesian(polar));
  }

  [Test]
  public void TestPolarToCartesian30() {
    Vector2 polar = new Vector2(20, 30);
    Vector2 expectedCartesian = new Vector2(10 * Mathf.Sqrt(3), 10);
    Assert.AreEqual(expectedCartesian, Coordinates2.ConvertPolarToCartesian(polar));
  }

  [Test]
  public void TestPolarToCartesian45() {
    Vector2 polar = new Vector2(10 * Mathf.Sqrt(2), 45);
    Vector2 expectedCartesian = new Vector2(10.0f, 10.0f);
    Assert.AreEqual(expectedCartesian, Coordinates2.ConvertPolarToCartesian(polar));
  }
}

public class Coordinates3Test {
  [Test]
  public void TestCartesianToSphericalOrigin() {
    Vector3 cartesian = Vector3.zero;
    Assert.AreEqual(0, Coordinates3.ConvertCartesianToSpherical(cartesian).x);
  }

  [Test]
  public void TestCartesianToSpherical() {
    Vector3 cartesian = new Vector3(2, -1, 3);
    Vector3 expectedSpherical = new Vector3(Mathf.Sqrt(14), 33.690068f, -15.501360f);
    Assert.AreEqual(expectedSpherical, Coordinates3.ConvertCartesianToSpherical(cartesian));
  }

  [Test]
  public void TestSphericalToCartesianOrigin() {
    Vector3 spherical = new Vector3(0, 45, 90);
    Vector3 expectedCartesian = Vector3.zero;
    Assert.AreEqual(expectedCartesian, Coordinates3.ConvertSphericalToCartesian(spherical));
  }

  [Test]
  public void TestSphericalToCartesian() {
    Vector3 spherical = new Vector3(10, 60, 30);
    Vector3 expectedCartesian = new Vector3(7.5f, 5.0f, 4.3301270189221945f);
    Assert.True(expectedCartesian == Coordinates3.ConvertSphericalToCartesian(spherical));
  }
}
