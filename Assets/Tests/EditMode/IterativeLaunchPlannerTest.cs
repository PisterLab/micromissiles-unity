using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;

public class IterativeLaunchPlannerTest {
  private class DummyLaunchAngleDataInterpolator : LaunchAngleDataInterpolator {
    public DummyLaunchAngleDataInterpolator() : base() {}

    // Generate the list of launch angle data points to interpolate.
    protected override List<LaunchAngleDataPoint> GenerateData() {
      return new List<LaunchAngleDataPoint> {
        new LaunchAngleDataPoint(new LaunchAngleInput(distance: 1, altitude: 100),
                                 new LaunchAngleOutput(launchAngle: 90, timeToPosition: 10)),
        new LaunchAngleDataPoint(new LaunchAngleInput(distance: 60, altitude: 1),
                                 new LaunchAngleOutput(launchAngle: 20, timeToPosition: 13)),
        new LaunchAngleDataPoint(new LaunchAngleInput(distance: 80, altitude: 1),
                                 new LaunchAngleOutput(launchAngle: 15, timeToPosition: 16)),
        new LaunchAngleDataPoint(new LaunchAngleInput(distance: 100, altitude: 1),
                                 new LaunchAngleOutput(launchAngle: 10, timeToPosition: 20)),
      };
    }
  }

  private static ILaunchAnglePlanner _launchAnglePlanner = new DummyLaunchAngleDataInterpolator();

  public static Agent GenerateAgent(in Vector3 position, in Vector3 velocity) {
    Agent agent = new GameObject().AddComponent<DummyAgent>();
    Rigidbody rb = agent.gameObject.AddComponent<Rigidbody>();
    agent.transform.position = position;
    rb.linearVelocity = velocity;
    return agent;
  }

  [Test]
  public void TestInterceptAtDataPoint() {
    Agent agent = GenerateAgent(position: new Vector3(1, 110, 0), velocity: new Vector3(0, -1, 0));
    LinearExtrapolator predictor = new LinearExtrapolator(agent);
    IterativeLaunchPlanner planner = new IterativeLaunchPlanner(_launchAnglePlanner, predictor);
    LaunchPlan plan = planner.Plan();
    Assert.IsTrue(plan.ShouldLaunch);
    Assert.AreEqual(90, plan.LaunchAngle);
    Assert.AreEqual(new Vector3(1, 100, 0), plan.InterceptPosition);
  }

  [Test]
  public void TestInterceptAroundDataPoint() {
    Agent agent =
        GenerateAgent(position: new Vector3(1, 110, 0), velocity: new Vector3(0, -1.1f, 0));
    LinearExtrapolator predictor = new LinearExtrapolator(agent);
    IterativeLaunchPlanner planner = new IterativeLaunchPlanner(_launchAnglePlanner, predictor);
    LaunchPlan plan = planner.Plan();
    Assert.IsTrue(plan.ShouldLaunch);
    Assert.AreEqual(90, plan.LaunchAngle);
    Assert.AreEqual(new Vector3(1, 99, 0), plan.InterceptPosition);
  }

  [Test]
  public void TestInterceptAcrossMultipleDataPoints() {
    Agent agent = GenerateAgent(position: new Vector3(126, 1, 0), velocity: new Vector3(-5, 0, 0));
    LinearExtrapolator predictor = new LinearExtrapolator(agent);
    IterativeLaunchPlanner planner = new IterativeLaunchPlanner(_launchAnglePlanner, predictor);
    LaunchPlan plan = planner.Plan();
    Assert.IsTrue(plan.ShouldLaunch);
    Assert.AreEqual(20, plan.LaunchAngle);
    Assert.AreEqual(new Vector3(61, 1, 0), plan.InterceptPosition);
  }

  [Test]
  public void TestLaunchDivergingFromOrigin() {
    Agent agent = GenerateAgent(position: new Vector3(0, 1, -80), velocity: new Vector3(0, 0, -1));
    LinearExtrapolator predictor = new LinearExtrapolator(agent);
    IterativeLaunchPlanner planner = new IterativeLaunchPlanner(_launchAnglePlanner, predictor);
    LaunchPlan plan = planner.Plan();
    Assert.IsFalse(plan.ShouldLaunch);
  }

  [Test]
  public void TestNoLaunchDivergingFromInterceptPoint() {
    Agent agent = GenerateAgent(position: new Vector3(1, 105, 0), velocity: new Vector3(0, 1, 0));
    LinearExtrapolator predictor = new LinearExtrapolator(agent);
    IterativeLaunchPlanner planner = new IterativeLaunchPlanner(_launchAnglePlanner, predictor);
    LaunchPlan plan = planner.Plan();
    Assert.IsFalse(plan.ShouldLaunch);
  }

  [Test]
  public void TestNoLaunchTooFarFromInterceptPoint() {
    Agent agent = GenerateAgent(position: new Vector3(1, 2000, 0), velocity: new Vector3(0, -1, 0));
    LinearExtrapolator predictor = new LinearExtrapolator(agent);
    IterativeLaunchPlanner planner = new IterativeLaunchPlanner(_launchAnglePlanner, predictor);
    LaunchPlan plan = planner.Plan();
    Assert.IsFalse(plan.ShouldLaunch);
  }
}
