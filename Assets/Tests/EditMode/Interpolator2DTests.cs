using Is = NUnit.Framework.Is;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

public class NearestNeighborInterpolator2DTests {
  // Helper method to count how many data points the 2D interpolator is holding.
  private static int GetNumDataPoints(NearestNeighborInterpolator2D interpolator) {
    var fieldInfo = typeof(NearestNeighborInterpolator2D)
                        .GetField("_data", System.Reflection.BindingFlags.NonPublic |
                                               System.Reflection.BindingFlags.Instance);

    // Get the raw value and use IList.Count.
    var dataList = fieldInfo.GetValue(interpolator) as System.Collections.IList;
    return dataList?.Count ?? 0;
  }

  [Test]
  public void TestEmptyAndInvalidLines() {
    string[] csvLines = {
      "",              // Empty.
      " , ",           // Whitespace.
      "abc,123,45,67"  // Invalid because "abc" is not float.
    };
    NearestNeighborInterpolator2D interpolator = new NearestNeighborInterpolator2D(csvLines);

    // This should result in zero valid data points.
    Assert.AreEqual(0, GetNumDataPoints(interpolator), "No valid lines should have been parsed.");

    // Expect the error log before calling the method that produces it.
    LogAssert.Expect(LogType.Error, "No data points available for interpolation.");
    Interpolator2DDataPoint result = interpolator.Interpolate(1, 1);
    Assert.AreEqual(0, result.Data.Count, "Interpolator returned data from an empty dataset.");
  }

  [Test]
  public void TestSingleDataPoint() {
    string[] csvLines = { "0,0,45,2" };
    NearestNeighborInterpolator2D interpolator = new NearestNeighborInterpolator2D(csvLines);

    // Query a random far point.
    Interpolator2DDataPoint result = interpolator.Interpolate(100, 200);
    Assert.AreEqual(new Vector2(0, 0), result.Coordinates);
    Assert.AreEqual(2, result.Data.Count);
    Assert.AreEqual(45f, result.Data[0]);
    Assert.AreEqual(2f, result.Data[1]);
  }

  [Test]
  public void TestInsufficientColumns() {
    string[] csvLines = { "100.0,200.0" };
    NearestNeighborInterpolator2D interpolator = new NearestNeighborInterpolator2D(csvLines);

    // Interpolate from the single data point.
    Interpolator2DDataPoint result = interpolator.Interpolate(100.0f, 200.0f);
    Assert.NotNull(result);
    Assert.AreEqual(new Vector2(100.0f, 200.0f), result.Coordinates);
    Assert.AreEqual(0, result.Data.Count, "Expected zero data columns for this line.");
  }

  [Test]
  public void TestOutOfRangeQuery() {
    // minimal data covering some small region.
    string[] csvLines = { "0,0,45,2", "10,10,30,3", "20,5,25,4" };
    NearestNeighborInterpolator2D interpolator = new NearestNeighborInterpolator2D(csvLines);

    // Query something very far from all data points, e.g., (1000, 1000).
    Interpolator2DDataPoint result = interpolator.Interpolate(1000, 1000);

    // The nearest neighbor is (20, 5), so we expect (25, 4).
    Assert.AreEqual(new Vector2(20, 5), result.Coordinates);
    Assert.AreEqual(2, result.Data.Count);
    Assert.AreEqual(25f, result.Data[0]);
    Assert.AreEqual(4f, result.Data[1]);
  }

  [Test]
  public void TestInterpolateVector2Success() {
    string[] csvLines = { "1,1,10,20", "1,2,30,40", "2,1,50,60", "2,2,70,80" };
    NearestNeighborInterpolator2D interpolator = new NearestNeighborInterpolator2D(csvLines);

    Vector2 testPoint = new Vector2(1.5f, 1.5f);
    Interpolator2DDataPoint result = interpolator.Interpolate(testPoint);
    Assert.AreEqual(2, result.Data.Count);

    // Check if the result matches any of the possible nearest point pairs.
    bool isValidPair = (result.Data[0] == 10f && result.Data[1] == 20f) ||
                       (result.Data[0] == 30f && result.Data[1] == 40f) ||
                       (result.Data[0] == 50f && result.Data[1] == 60f) ||
                       (result.Data[0] == 70f && result.Data[1] == 80f);
    Assert.IsTrue(isValidPair, "Result should match one of the nearest point pairs.");
  }

  [Test]
  public void TestInterpolateVector2Error() {
    // Empty dataset.
    string[] csvLines = {};
    NearestNeighborInterpolator2D interpolator = new NearestNeighborInterpolator2D(csvLines);

    Vector2 testPoint = new Vector2(1.0f, 1.0f);
    LogAssert.Expect(LogType.Error, "No data points available for interpolation.");
    Interpolator2DDataPoint result = interpolator.Interpolate(testPoint);
    Assert.AreEqual(0, result.Data.Count, "Should return empty list when no data is available.");
  }
}
