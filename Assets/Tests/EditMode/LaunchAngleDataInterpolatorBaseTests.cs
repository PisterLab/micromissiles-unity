using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools.Utils;

public class LaunchAngleDataInterpolatorBaseTests : TestBase {
  private class TestLaunchAngleDataInterpolator : LaunchAngleDataInterpolatorBase {
    public TestLaunchAngleDataInterpolator(IAgent agent) : base(agent) {}

    // Generate the launch angle data points to interpolate.
    protected override IEnumerable<LaunchAngleDataPoint> GenerateData() {
      return new List<LaunchAngleDataPoint> {
        new LaunchAngleDataPoint {
          Input = new LaunchAngleInput { Distance = 1, Altitude = 100 },
          Output = new LaunchAngleOutput { LaunchAngle = 90, TimeToPosition = 10 },
        },
        new LaunchAngleDataPoint {
          Input = new LaunchAngleInput { Distance = 100, Altitude = 1 },
          Output = new LaunchAngleOutput { LaunchAngle = 10, TimeToPosition = 20 },
        },
      };
    }
  }

  private AgentBase _agent;
  private TestLaunchAngleDataInterpolator _interpolator;

  [SetUp]
  public void SetUp() {
    _agent = new GameObject("Agent").AddComponent<AgentBase>();
    Rigidbody agentRb = _agent.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(_agent, "Awake");
    _interpolator = new TestLaunchAngleDataInterpolator(_agent);
  }

  [Test]
  public void Plan_MatchesDataPoint_ReturnsSamePoint() {
    var input = new LaunchAngleInput { Distance = 1, Altitude = 100 };
    var expectedOutput = new LaunchAngleOutput { LaunchAngle = 90, TimeToPosition = 10 };
    Assert.AreEqual(expectedOutput, _interpolator.Plan(input));
  }

  [Test]
  public void Plan_NearDataPoint_ReturnsClosestPoint() {
    var input = new LaunchAngleInput { Distance = 1, Altitude = 200 };
    var expectedOutput = new LaunchAngleOutput { LaunchAngle = 90, TimeToPosition = 10 };
    Assert.AreEqual(expectedOutput, _interpolator.Plan(input));
  }

  [Test]
  public void InterceptPosition_MatchesDataPoint_ReturnsSamePoint() {
    var targetPosition = new Vector3(100f, 1f, 0f);
    Assert.That(_interpolator.InterceptPosition(targetPosition),
                Is.EqualTo(new Vector3(100f, 1f, 0f)).Using(Vector3EqualityComparer.Instance));
  }

  [Test]
  public void InterceptPosition_NearDataPoint_ReturnsClosestPoint() {
    var targetPosition = new Vector3(101f, 2f, 0f);
    Assert.That(_interpolator.InterceptPosition(targetPosition),
                Is.EqualTo(new Vector3(100f, 1f, 0f)).Using(Vector3EqualityComparer.Instance));
  }

  [Test]
  public void InterceptPosition_MovesWithAgentPosition() {
    var agentPosition = new Vector3(61, 5055, 874);
    _agent.Position = agentPosition;
    var targetPosition = agentPosition + new Vector3(100f, 1f, 0f);
    Assert.That(_interpolator.InterceptPosition(targetPosition),
                Is.EqualTo(agentPosition + new Vector3(100f, 1f, 0f))
                    .Using(Vector3EqualityComparer.Instance));
  }
}
