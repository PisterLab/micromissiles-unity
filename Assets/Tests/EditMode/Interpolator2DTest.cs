using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using Is = NUnit.Framework.Is;

public class Interpolator2DTest {
  // [Test]
  // public void TestEmptyAndInvalidLines() {
  //   string[] csvLines = {
  //     "",              // empty
  //     " , ",           // whitespace
  //     "abc,123,45,67"  // invalid because "abc" is not float
  //   };
  //   Interpolator2D interpolator = new Interpolator2D(csvLines);

  //   // This should result in zero valid data points
  //   Assert.AreEqual(0, interpolatorCount(interpolator), "No valid lines should have been parsed.");

  //   // Expect the error log before calling the method that produces it
  //   LogAssert.Expect(LogType.Error, "No data points available for interpolation.");
  //   List<float> result = interpolator.Interpolate(1, 1);
  //   Assert.AreEqual(0, result.Count, "Interpolator returned data from an empty dataset?");
  // }

  // // Helper method to count how many data points the Interpolator2D is holding
  // private int interpolatorCount(Interpolator2D interpolator) {
  //   var fieldInfo = typeof(Interpolator2D)
  //                       .GetField("_data", System.Reflection.BindingFlags.NonPublic |
  //                                              System.Reflection.BindingFlags.Instance);

  //   // Get the raw value and use IList.Count
  //   var dataList = fieldInfo.GetValue(interpolator) as System.Collections.IList;
  //   return dataList?.Count ?? 0;
  // }

  // [Test]
  // public void TestSingleDataPoint() {
  //   string[] csvLines = {
  //     "0,0,45,2"  // x=0, y=0, angle=45, time=2
  //   };
  //   Interpolator2D interpolator = new Interpolator2D(csvLines);

  //   // Query a random far point
  //   List<float> result = interpolator.Interpolate(100, 200);
  //   Assert.AreEqual(2, result.Count);
  //   Assert.AreEqual(45f, result[0], 1e-5f);
  //   Assert.AreEqual(2f, result[1], 1e-5f);
  // }

  // [Test]
  // public void TestInsufficientColumns() {
  //   string[] csvLines = {
  //     "100.0,200.0"  // only x,y
  //   };
  //   Interpolator2D interpolator = new Interpolator2D(csvLines);

  //   // Interpolate from the single data point we have
  //   List<float> result = interpolator.Interpolate(100.0f, 200.0f);

  //   // We expect the 'Data' portion to be empty in this scenario
  //   Assert.NotNull(result);
  //   Assert.AreEqual(0, result.Count, "We expected zero data columns for this line.");
  // }

  // [Test]
  // public void TestOutOfRangeQuery() {
  //   // minimal data covering some small region
  //   string[] csvLines = { "0,0,45,2", "10,10,30,3", "20,5,25,4" };
  //   Interpolator2D interpolator = new Interpolator2D(csvLines);

  //   // Query something very far from all data points, e.g. (1000,1000)
  //   List<float> result = interpolator.Interpolate(1000, 1000);

  //   // The nearest is probably (20,5), so we expect angle=25, time=4
  //   Assert.AreEqual(2, result.Count);
  //   Assert.AreEqual(25f, result[0], 1e-5f);
  //   Assert.AreEqual(4f, result[1], 1e-5f);
  // }

  // [Test]
  // public void TestInterpolateVector2Success() {
  //   string[] csvLines = { "1,1,10,20", "1,2,30,40", "2,1,50,60", "2,2,70,80" };
  //   Interpolator2D interpolator = new Interpolator2D(csvLines);

  //   Vector2 testPoint = new Vector2(1.5f, 1.5f);
  //   List<float> result = interpolator.Interpolate(testPoint);

  //   Assert.AreEqual(2, result.Count, "Should return 2 interpolated values");

  //   // Check if the result matches any of the possible nearest point pairs
  //   bool isValidPair =
  //       (result[0] == 10f && result[1] == 20f) || (result[0] == 30f && result[1] == 40f) ||
  //       (result[0] == 50f && result[1] == 60f) || (result[0] == 70f && result[1] == 80f);

  //   Assert.IsTrue(isValidPair, "Result should match one of the nearest point pairs");
  // }

  // [Test]
  // public void TestInterpolateVector2Error() {
  //   string[] csvLines = {};  // Empty dataset
  //   Interpolator2D interpolator = new Interpolator2D(csvLines);

  //   Vector2 testPoint = new Vector2(1.0f, 1.0f);

  //   LogAssert.Expect(LogType.Error, "No data points available for interpolation.");
  //   List<float> result = interpolator.Interpolate(testPoint);
  //   Assert.AreEqual(0, result.Count, "Should return empty list when no data is available");
  // }
}
