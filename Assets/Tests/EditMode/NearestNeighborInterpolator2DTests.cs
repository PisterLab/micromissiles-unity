using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

public class NearestNeighborInterpolator2DTests {
  private const float _epsilon = 1e-3f;

  [Test]
  public void Interpolate_InvalidPoints_ReturnsEmptyResult() {
    LogAssert.ignoreFailingMessages = true;
    string[] csvLines = {
      "",              // Empty.
      " , ",           // Whitespace.
      "abc,123,45,67"  // Invalid because "abc" is not float.
    };
    var interpolator = new NearestNeighborInterpolator2D(csvLines);
    Assert.AreEqual(0, interpolator.Data.Count);
    Interpolator2DDataPoint result = interpolator.Interpolate(1, 1);
    Assert.AreEqual(0, result.Data.Count);
  }

  [Test]
  public void Interpolate_SinglePoint_ReturnsSinglePoint() {
    string[] csvLines = { "0,0,45,2" };
    var interpolator = new NearestNeighborInterpolator2D(csvLines);
    Interpolator2DDataPoint result = interpolator.Interpolate(100, 200);
    Assert.AreEqual(new Vector2(0, 0), result.Coordinates);
    Assert.AreEqual(2, result.Data.Count);
    Assert.AreEqual(45f, result.Data[0], _epsilon);
    Assert.AreEqual(2f, result.Data[1], _epsilon);
  }

  [Test]
  public void Interpolate_InsufficientColumns_ReturnsEmptyResult() {
    string[] csvLines = { "100.0,200.0" };
    var interpolator = new NearestNeighborInterpolator2D(csvLines);
    Interpolator2DDataPoint result = interpolator.Interpolate(100, 200);
    Assert.NotNull(result);
    Assert.That(result.Coordinates,
                Is.EqualTo(new Vector2(100f, 200f)).Using(Vector2EqualityComparer.Instance));
    Assert.AreEqual(0, result.Data.Count);
  }

  [Test]
  public void Interpolate_QueryOutOfRange_ReturnsClosestPoint() {
    string[] csvLines = { "0,0,45,2", "10,10,30,3", "20,5,25,4" };
    var interpolator = new NearestNeighborInterpolator2D(csvLines);
    Interpolator2DDataPoint result = interpolator.Interpolate(1000, 1000);
    Assert.AreEqual(new Vector2(20, 5), result.Coordinates);
    Assert.AreEqual(2, result.Data.Count);
    Assert.AreEqual(25f, result.Data[0], _epsilon);
    Assert.AreEqual(4f, result.Data[1], _epsilon);
  }

  [Test]
  public void Interpolate_BetweenDataPoints_ReturnsOnePoint() {
    string[] csvLines = { "1,1,10,20", "1,2,30,40", "2,1,50,60", "2,2,70,80" };
    var interpolator = new NearestNeighborInterpolator2D(csvLines);
    Interpolator2DDataPoint result = interpolator.Interpolate(1.5f, 1.5f);
    Assert.AreEqual(2, result.Data.Count);
    var validData =
        new List<List<float>> { new List<float> { 10f, 20f }, new List<float> { 30f, 40f },
                                new List<float> { 50f, 60f }, new List<float> { 70f, 80f } };
    var isValidPair = validData.Any(v => v.SequenceEqual(result.Data));
    Assert.IsTrue(isValidPair);
  }

  [Test]
  public void Interpolate_NoDataPoints_ReturnsEmptyResult() {
    LogAssert.ignoreFailingMessages = true;
    string[] csvLines = {};
    var interpolator = new NearestNeighborInterpolator2D(csvLines);
    Interpolator2DDataPoint result = interpolator.Interpolate(1, 1);
    Assert.AreEqual(0, result.Data.Count);
  }
}
