using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class IterativeLaunchPlannerTests : TestBase {
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
          Input = new LaunchAngleInput { Distance = 60, Altitude = 1 },
          Output = new LaunchAngleOutput { LaunchAngle = 20, TimeToPosition = 13 },
        },
        new LaunchAngleDataPoint {
          Input = new LaunchAngleInput { Distance = 80, Altitude = 1 },
          Output = new LaunchAngleOutput { LaunchAngle = 15, TimeToPosition = 16 },
        },
        new LaunchAngleDataPoint {
          Input = new LaunchAngleInput { Distance = 100, Altitude = 1 },
          Output = new LaunchAngleOutput { LaunchAngle = 10, TimeToPosition = 20 },
        },
      };
    }
  }

  private AgentBase _agent;
  private FixedHierarchical _target;
  private ILaunchAnglePlanner _launchAnglePlanner;
  private IPredictor _predictor;
  private IterativeLaunchPlanner _planner;

  [SetUp]
  public void SetUp() {
    _agent = new GameObject("Agent").AddComponent<AgentBase>();
    Rigidbody agentRb = _agent.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(_agent, "Awake");
    _target = new FixedHierarchical();
    _launchAnglePlanner = new TestLaunchAngleDataInterpolator(_agent);
    _predictor = new LinearExtrapolator(_target);
    _planner = new IterativeLaunchPlanner(_launchAnglePlanner, _predictor);
  }

  [Test]
  public void Plan_InterceptAtDataPoint_ReturnsLaunch() {
    _target.Position = new Vector3(1, 110, 0);
    _target.Velocity = new Vector3(0, -1, 0);
    LaunchPlan plan = _planner.Plan();
    Assert.IsTrue(plan.ShouldLaunch);
    Assert.AreEqual(90, plan.LaunchAngle);
    Assert.AreEqual(new Vector3(1, 100, 0), plan.InterceptPosition);
  }

  [Test]
  public void Plan_InterceptNearDataPoint_ReturnsLaunch() {
    _target.Position = new Vector3(1, 110, 0);
    _target.Velocity = new Vector3(0, -1.1f, 0);
    LaunchPlan plan = _planner.Plan();
    Assert.IsTrue(plan.ShouldLaunch);
    Assert.AreEqual(90, plan.LaunchAngle);
    Assert.AreEqual(new Vector3(1, 99, 0), plan.InterceptPosition);
  }

  [Test]
  public void Plan_InterceptBetweenDataPoints_ReturnsLaunch() {
    _target.Position = new Vector3(126, 1, 0);
    _target.Velocity = new Vector3(-5, 0, 0);
    LaunchPlan plan = _planner.Plan();
    Assert.IsTrue(plan.ShouldLaunch);
    Assert.AreEqual(20, plan.LaunchAngle);
    Assert.AreEqual(new Vector3(61, 1, 0), plan.InterceptPosition);
  }

  [Test]
  public void Plan_DivergingFromOrigin_ReturnsNoLaunch() {
    _target.Position = new Vector3(0, 1, -80);
    _target.Velocity = new Vector3(0, 0, -1);
    LaunchPlan plan = _planner.Plan();
    Assert.IsFalse(plan.ShouldLaunch);
  }

  [Test]
  public void Plan_DivergingFromInterceptPoint_ReturnsNoLaunch() {
    _target.Position = new Vector3(1, 105, 0);
    _target.Velocity = new Vector3(0, 1, 0);
    LaunchPlan plan = _planner.Plan();
    Assert.IsFalse(plan.ShouldLaunch);
  }

  [Test]
  public void Plan_TooFarFromInterceptPoint_ReturnsNoLaunch() {
    _target.Position = new Vector3(1, 2000, 0);
    _target.Velocity = new Vector3(0, -1, 0);
    LaunchPlan plan = _planner.Plan();
    Assert.IsFalse(plan.ShouldLaunch);
  }
}
