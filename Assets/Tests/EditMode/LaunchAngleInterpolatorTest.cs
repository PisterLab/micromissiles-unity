using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class LaunchAngleInterpolatorTest {
  [Test]
  public void TestDataPoint() {
    float deltaX = 9074.84f;
    float deltaY = 97.7306f;
    LaunchAngleOutput expectedLaunchAngle =
        new LaunchAngleOutput(launchAngle: 22f, timeToPosition: 21.32f);
    LaunchAngleInterpolator interpolator = new LaunchAngleInterpolator();
    Assert.AreEqual(interpolator.CalculateLaunchAngle(deltaX, deltaY), expectedLaunchAngle);
  }

  [Test]
  public void TestNearestNeighbor() {
    float deltaX = 9076f;
    float deltaY = 94f;
    LaunchAngleOutput expectedLaunchAngle =
        new LaunchAngleOutput(launchAngle: 22f, timeToPosition: 21.32f);
    LaunchAngleInterpolator interpolator = new LaunchAngleInterpolator();
    Assert.AreEqual(interpolator.CalculateLaunchAngle(deltaX, deltaY), expectedLaunchAngle);
  }
}
