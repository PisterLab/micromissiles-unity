using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

public class LaunchAngleCsvInterpolatorTests : TestBase {
  private const float _epsilon = 0.01f;

  private AgentBase _agent;
  private LaunchAngleCsvInterpolator _interpolator;

  [SetUp]
  public void SetUp() {
    _agent = new GameObject("Agent").AddComponent<AgentBase>();
    Rigidbody agentRb = _agent.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(_agent, "Awake");
    _interpolator = new LaunchAngleCsvInterpolator(
        _agent, path: Path.Combine("Planning", "hydra70_launch_angle.csv"),
        configLoader: ConfigLoader.LoadFromStreamingAssets);
  }

  [Test]
  public void Plan_MatchesDataPoint_ReturnsSamePoint() {
    var input = new LaunchAngleInput { Distance = 9074.84f, Altitude = 97.7306f };
    var expectedOutput = new LaunchAngleOutput { LaunchAngle = 22f, TimeToPosition = 21.32f };
    Assert.AreEqual(expectedOutput, _interpolator.Plan(input));
  }

  [Test]
  public void Plan_NearDataPoint_ReturnsClosestPoint() {
    var input = new LaunchAngleInput { Distance = 9076f, Altitude = 94f };
    var expectedOutput = new LaunchAngleOutput { LaunchAngle = 22f, TimeToPosition = 21.32f };
    Assert.AreEqual(expectedOutput, _interpolator.Plan(input));
  }

  [Test]
  public void Plan_FileNotFound_ThrowsException() {
    LogAssert.ignoreFailingMessages = true;

    // Inject a mock ConfigLoader that returns an empty string.
    LaunchAngleCsvInterpolator.ConfigLoaderDelegate mockLoader = (string path) => "";
    Assert.Throws<InvalidOperationException>(() => {
      var interpolator = new LaunchAngleCsvInterpolator(_agent, path: "Planning/nonexistent.csv",
                                                        configLoader: mockLoader);
      interpolator.Plan(new LaunchAngleInput());
    });
  }

  [Test]
  public void Plan_InvalidInterpolationData_ThrowsException() {
    LogAssert.ignoreFailingMessages = true;

    // Create a mock CSV with invalid data format.
    string mockCsv = "invalid,csv,data\n1,2,3";
    LaunchAngleCsvInterpolator.ConfigLoaderDelegate mockLoader = (string path) => mockCsv;

    var interpolator = new LaunchAngleCsvInterpolator(_agent, path: null, configLoader: mockLoader);
    var input = new LaunchAngleInput { Distance = 1, Altitude = 2 };
    Assert.Throws<InvalidOperationException>(() => { interpolator.Plan(input); });
  }

  [Test]
  public void InterceptPosition_MatchesDataPoint_ReturnsSamePoint() {
    var targetPosition = new Vector3(9074.84f, 97.7306f, 0f);
    _interpolator.Plan(new LaunchAngleInput());
    Assert.That(_interpolator.InterceptPosition(targetPosition),
                Is.EqualTo(new Vector3(9074.84f, 97.7306f, 0f))
                    .Using(new Vector3EqualityComparer(_epsilon)));
  }

  [Test]
  public void InterceptPosition_NearDataPoint_ReturnsClosestPoint() {
    var targetPosition = new Vector3(9078f, 93f, 0f);
    _interpolator.Plan(new LaunchAngleInput());
    Assert.That(_interpolator.InterceptPosition(targetPosition),
                Is.EqualTo(new Vector3(9074.84f, 97.7306f, 0f))
                    .Using(new Vector3EqualityComparer(_epsilon)));
  }

  [Test]
  public void InterceptPosition_MovesWithAgentPosition() {
    var agentPosition = new Vector3(61.1f, 5055.5f, 874.9f);
    _agent.Position = agentPosition;
    var targetPosition = agentPosition + new Vector3(9074.84f, 97.7306f, 0f);
    _interpolator.Plan(new LaunchAngleInput());
    Assert.That(_interpolator.InterceptPosition(targetPosition),
                Is.EqualTo(agentPosition + new Vector3(9074.84f, 97.7306f, 0f))
                    .Using(new Vector3EqualityComparer(_epsilon)));
  }
}
