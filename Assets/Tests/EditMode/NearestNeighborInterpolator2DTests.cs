using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class NearestNeighborInterpolator2DTests {
  private const float _epsilon = 1e-3f;

  [Test]
  public void Interpolate_InvalidPoints_ReturnsEmptyResult() {
    string[] csvLines = {
      "",              // Empty.
      " , ",           // Whitespace.
      "abc,123,45,67"  // Invalid because "abc" is not float.
    };
    var interpolator = new NearestNeighborInterpolator2D(csvLines);
    Assert.AreEqual(0, interpolator.Data.Count);
    var result = interpolator.Interpolate(1, 1);
    Assert.AreEqual(0, result.Data.Count);
  }

  [Test]
  public void Interpolate_SinglePoint_ReturnsSinglePoint() {
    string[] csvLines = { "0,0,45,2" };
    var interpolator = new NearestNeighborInterpolator2D(csvLines);
    var result = interpolator.Interpolate(100, 200);
    Assert.AreEqual(new Vector2(0, 0), result.Coordinates);
    Assert.AreEqual(new List<float> { 45f, 2f }, result.Data, _epsilon);
  }

  [Test]
  public void Interpolate_InsufficientColumns_ReturnsEmptyResult() {
    string[] csvLines = { "100.0,200.0" };
    var interpolator = new NearestNeighborInterpolator2D(csvLines);
    var result = interpolator.Interpolate(100f, 200f);
    Assert.NotNull(result);
    Assert.AreEqual(new Vector2(100f, 200f), result.Coordinates, _epsilon);
    Assert.AreEqual(0, result.Data.Count);
  }

  [Test]
  public void Interpolate_QueryOutOfRange_ReturnsClosestPoint() {
    string[] csvLines = { "0,0,45,2", "10,10,30,3", "20,5,25,4" };
    var interpolator = new NearestNeighborInterpolator2D(csvLines);
    var result = interpolator.Interpolate(1000, 1000);
    Assert.AreEqual(new Vector2(20, 5), result.Coordinates);
    Assert.AreEqual(new List<float> { 25f, 4f }, result.Data, _epsilon);
  }

  [Test]
  public void Interpolate_BetweenDataPoints_ReturnsOnePoint() {
    string[] csvLines = { "1,1,10,20", "1,2,30,40", "2,1,50,60", "2,2,70,80" };
    var interpolator = new NearestNeighborInterpolator2D(csvLines);
    var testPoint = new Vector2(1.5f, 1.5f);
    var result = interpolator.Interpolate(testPoint);
    Assert.AreEqual(2, result.Data.Count);
    var isValidPair =
        (result.Data == new Vector2(10f, 20f) || result.Data == new Vector2(30f, 40f) ||
         result.Data == new Vector2(50f, 60f) || result.Data == new Vector2(70f, 80f));
    Assert.IsTrue(isValidPair);
  }

  [Test]
  public void Interpolate_NoDataPoints_ReturnsEmptyResult() {
    string[] csvLines = {};
    var interpolator = new NearestNeighborInterpolator2D(csvLines);
    Interpolator2DDataPoint result = interpolator.Interpolate(new Vector2(1f, 1f));
    Assert.AreEqual(0, result.Data.Count);
  }
}
