using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using System;

public class LaunchAngleCsvInterpolatorTest {
  [Test]
  public void TestDataPoint() {
    LaunchAngleInput input = new LaunchAngleInput(distance: 9074.84f, altitude: 97.7306f);
    LaunchAngleOutput expectedOutput =
        new LaunchAngleOutput(launchAngle: 22f, timeToPosition: 21.32f);

    LaunchAngleCsvInterpolator interpolator = new LaunchAngleCsvInterpolator();
    Assert.AreEqual(expectedOutput, interpolator.Plan(input));
  }

  [Test]
  public void TestNearestNeighbor() {
    LaunchAngleInput input = new LaunchAngleInput(distance: 9076f, altitude: 94f);
    LaunchAngleOutput expectedOutput =
        new LaunchAngleOutput(launchAngle: 22f, timeToPosition: 21.32f);

    LaunchAngleCsvInterpolator interpolator = new LaunchAngleCsvInterpolator();
    Assert.AreEqual(expectedOutput, interpolator.Plan(input));
  }

  [Test]
  public void TestFileNotFound() {
    // Inject a mock ConfigLoader that returns an empty string.
    LaunchAngleCsvInterpolator.ConfigLoaderDelegate mockLoader = (string path) => "";

    LogAssert.Expect(LogType.Error, "Failed to load CSV file from Planning/nonexistent.csv.");
    Assert.Throws<InvalidOperationException>(() => {
      var interpolator = new LaunchAngleCsvInterpolator("Planning/nonexistent.csv", mockLoader);
      interpolator.Plan(new LaunchAngleInput());
    });
  }

  [Test]
  public void TestInvalidInterpolationData() {
    // Create a mock CSV with invalid data format
    string mockCsv = "invalid,csv,data\n1,2,3";  // Wrong format but not empty
    LaunchAngleCsvInterpolator.ConfigLoaderDelegate mockLoader = (string path) => mockCsv;

    var interpolator = new LaunchAngleCsvInterpolator(null, mockLoader);
    var extremeInput = new LaunchAngleInput(distance: float.MaxValue, altitude: float.MaxValue);

    var ex = Assert.Throws<InvalidOperationException>(() => { interpolator.Plan(extremeInput); });
    Assert.That(ex.Message, Is.EqualTo("Interpolator returned invalid data."));
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
    LaunchAngleOutput expectedOutput = new LaunchAngleOutput(launchAngle: 90, timeToPosition: 10);

    DummyLaunchAngleDataInterpolator interpolator = new DummyLaunchAngleDataInterpolator();
    Assert.AreEqual(expectedOutput, interpolator.Plan(input));
  }

  [Test]
  public void TestNearestNeighbor() {
    LaunchAngleInput input = new LaunchAngleInput(distance: 1, altitude: 200);
    LaunchAngleOutput expectedOutput = new LaunchAngleOutput(launchAngle: 90, timeToPosition: 10);

    DummyLaunchAngleDataInterpolator interpolator = new DummyLaunchAngleDataInterpolator();
    Assert.AreEqual(expectedOutput, interpolator.Plan(input));
  }
}
