using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

public class LaunchAngleCsvInterpolatorTest {
  [Test]
  public void TestDataPoint() {
    LaunchAngleInput input = new LaunchAngleInput(distance: 9074.84f, altitude: 97.7306f);
    LaunchAngleOutput output = new LaunchAngleOutput(launchAngle: 22f, timeToPosition: 21.32f);
    LaunchAngleCsvInterpolator interpolator = new LaunchAngleCsvInterpolator();
    Assert.AreEqual(interpolator.Plan(input), output);
  }

  [Test]
  public void TestNearestNeighbor() {
    LaunchAngleInput input = new LaunchAngleInput(distance: 9076, altitude: 94);
    LaunchAngleOutput output = new LaunchAngleOutput(launchAngle: 22f, timeToPosition: 21.32f);
    LaunchAngleCsvInterpolator interpolator = new LaunchAngleCsvInterpolator();
    Assert.AreEqual(interpolator.Plan(input), output);
  }
}

public class LaunchAngleDataInterpolatorTest {
  private class DummyLaunchAngleDataInterpolator : LaunchAngleDataInterpolator {
    public DummyLaunchAngleDataInterpolator() : base() {}

    // Generate the list of launch angle data points to interpolate.
    protected override List<LaunchAngleDataPoint> GenerateData() {
      return new List<LaunchAngleDataPoint> {
        new LaunchAngleDataPoint(new LaunchAngleInput(distance: 1, altitude: 100),
                                 new LaunchAngleOutput(launchAngle: 90, timeToPosition: 10)),
        new LaunchAngleDataPoint(new LaunchAngleInput(distance: 100, altitude: 1),
                                 new LaunchAngleOutput(launchAngle: 10, timeToPosition: 20)),
      };
    }
  }

  [Test]
  public void TestDataPoint() {
    LaunchAngleInput input = new LaunchAngleInput(distance: 1, altitude: 100);
    LaunchAngleOutput output = new LaunchAngleOutput(launchAngle: 90, timeToPosition: 10);
    DummyLaunchAngleDataInterpolator interpolator = new DummyLaunchAngleDataInterpolator();
    Assert.AreEqual(interpolator.Plan(input), output);
  }

  [Test]
  public void TestNearestNeighbor() {
    LaunchAngleInput input = new LaunchAngleInput(distance: 1, altitude: 200);
    LaunchAngleOutput output = new LaunchAngleOutput(launchAngle: 90, timeToPosition: 10);
    DummyLaunchAngleDataInterpolator interpolator = new DummyLaunchAngleDataInterpolator();
    Assert.AreEqual(interpolator.Plan(input), output);
  }
}
