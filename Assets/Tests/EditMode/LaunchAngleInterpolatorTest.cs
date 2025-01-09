using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System;

public class LaunchAngleInterpolatorTest {
  [Test]
  public void TestDataPoint() {
    float deltaX = 9074.84f;
    float deltaY = 97.7306f;
    LaunchAngleOutput expectedLaunchAngle =
        new LaunchAngleOutput(launchAngle: 22f, timeToPosition: 21.32f);
    LaunchAngleInterpolator interpolator = new LaunchAngleInterpolator();
    Assert.AreEqual(expectedLaunchAngle, interpolator.CalculateLaunchAngle(deltaX, deltaY));
  }

  [Test]
  public void TestNearestNeighbor() {
    float deltaX = 9076f;
    float deltaY = 94f;
    LaunchAngleOutput expectedLaunchAngle =
        new LaunchAngleOutput(launchAngle: 22f, timeToPosition: 21.32f);
    LaunchAngleInterpolator interpolator = new LaunchAngleInterpolator();
    Assert.AreEqual(expectedLaunchAngle, interpolator.CalculateLaunchAngle(deltaX, deltaY));
  }

  [Test]
  public void TestFileNotFound() {
    // Inject a mock ConfigLoader that returns an empty string
    LaunchAngleInterpolator.ConfigLoaderDelegate mockLoader = (string path) => "";

    // Expect the specific error log from LaunchAngleInterpolator
    LogAssert.Expect(LogType.Error, "Failed to load CSV file from Planning/nonexistent.csv.");

    Assert.Throws<InvalidOperationException>(() => {
      var interpolator = new LaunchAngleInterpolator("Planning/nonexistent.csv", mockLoader);
    });
  }

  [Test]
  public void TestInvalidInterpolationData() {
    // Create a mock CSV content with insufficient data
    string mockCsv = "0,0";  // Only x and y, missing angle and time
    LaunchAngleInterpolator.ConfigLoaderDelegate mockLoader = (string path) => mockCsv;

    LaunchAngleInterpolator interpolator = new LaunchAngleInterpolator(null, mockLoader);

    // Use values that will cause the interpolator to return invalid data
    float extremeX = float.MaxValue;
    float extremeY = float.MaxValue;

    Assert.Throws<InvalidOperationException>(() => {
      interpolator.CalculateLaunchAngle(extremeX, extremeY);
    }, "Interpolator returned invalid data.");
  }
}
