using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;

// Tests for the iterative launch planner algorithm.
//
// KEY PRINCIPLE: In a successful intercept scenario, the final intercept position
// should equal (or be very close to) the predicted target position. Both the
// interceptor and target arrive at the same point at the same time.
// The interpolation table provides launch parameters (angle, time) to achieve this.
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
    Vector3 expected = new Vector3(1, 100, 0);
    Assert.That(Vector3.Distance(plan.InterceptPosition, expected), Is.LessThan(0.01f),
                $"Expected {expected}, but was {plan.InterceptPosition}");
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
    Vector3 expected = new Vector3(1, 99, 0);
    Assert.That(Vector3.Distance(plan.InterceptPosition, expected), Is.LessThan(0.01f),
                $"Expected {expected}, but was {plan.InterceptPosition}");
  }

  [Test]
  public void TestInterceptAcrossMultipleDataPoints() {
    Agent agent = GenerateAgent(position: new Vector3(126, 1, 0), velocity: new Vector3(-5, 0, 0));
    LinearExtrapolator predictor = new LinearExtrapolator(agent);
    IterativeLaunchPlanner planner = new IterativeLaunchPlanner(_launchAnglePlanner, predictor);
    LaunchPlan plan = planner.Plan();
    Assert.IsTrue(plan.ShouldLaunch);
    Assert.AreEqual(20, plan.LaunchAngle);
    Vector3 expected = new Vector3(61, 1, 0);
    Assert.That(Vector3.Distance(plan.InterceptPosition, expected), Is.LessThan(0.01f),
                $"Expected {expected}, but was {plan.InterceptPosition}");
  }

  [Test]
  public void TestLaunchDivergingFromOrigin() {
    Agent agent = GenerateAgent(position: new Vector3(0, 1, -80), velocity: new Vector3(0, 0, -1));
    LinearExtrapolator predictor = new LinearExtrapolator(agent);
    IterativeLaunchPlanner planner = new IterativeLaunchPlanner(_launchAnglePlanner, predictor);
    LaunchPlan plan = planner.Plan();
    Assert.IsFalse(plan.ShouldLaunch,
                   $"Should not launch at threat moving away from origin. Plan: {plan}");
  }

  [Test]
  public void TestNoLaunchInterceptBehindOrigin() {
    // Test case: Threat is approaching, but any valid intercept point would be behind the origin.
    // The planner should recognize this as an invalid launch geometry.
    Agent agent = GenerateAgent(position: new Vector3(0, 1, 200), velocity: new Vector3(0, 0, -20));
    LinearExtrapolator predictor = new LinearExtrapolator(agent);
    IterativeLaunchPlanner planner = new IterativeLaunchPlanner(_launchAnglePlanner, predictor);

    LaunchPlan plan = planner.Plan();

    // The logic should prevent a launch where the intercept point is behind the origin
    // relative to the threat's approach.
    Assert.IsFalse(
        plan.ShouldLaunch,
        $"Should not launch when intercept point is geometrically behind the origin. Plan: {plan}");
  }

  [Test]
  public void TestNoLaunchTooFarFromInterceptPoint() {
    Agent agent = GenerateAgent(position: new Vector3(1, 2000, 0), velocity: new Vector3(0, -1, 0));
    LinearExtrapolator predictor = new LinearExtrapolator(agent);
    IterativeLaunchPlanner planner = new IterativeLaunchPlanner(_launchAnglePlanner, predictor);
    LaunchPlan plan = planner.Plan();
    UnityEngine.Debug.Log(
        $"TestNoLaunchTooFarFromInterceptPoint: ShouldLaunch={plan.ShouldLaunch}, Angle={plan.LaunchAngle}, Position={plan.InterceptPosition}");
    Assert.IsFalse(plan.ShouldLaunch);
  }

  // ========== NEW BACKWARDS/SIDEWAYS LAUNCH PREVENTION TESTS ==========

  [Test]
  public void TestNoLaunchThreatBehindOrigin() {
    // Test case: Threat is behind the origin and moving away - should not launch
    Agent agent =
        GenerateAgent(position: new Vector3(0, 1, -100), velocity: new Vector3(0, 0, -50));
    LinearExtrapolator predictor = new LinearExtrapolator(agent);
    IterativeLaunchPlanner planner = new IterativeLaunchPlanner(_launchAnglePlanner, predictor);
    LaunchPlan plan = planner.Plan();
    Assert.IsFalse(plan.ShouldLaunch,
                   $"Should not launch at threat moving away behind origin, but got: {plan}");
  }

  [Test]
  public void TestNoLaunchExtremeSidewaysGeometry() {
    // Test case: Threat moving straight down, but far to the side - should detect sideways launch
    Agent agent =
        GenerateAgent(position: new Vector3(5000, 1, 100), velocity: new Vector3(0, 0, -30));
    LinearExtrapolator predictor = new LinearExtrapolator(agent);
    IterativeLaunchPlanner planner = new IterativeLaunchPlanner(_launchAnglePlanner, predictor);
    LaunchPlan plan = planner.Plan();

    // This should either not launch or the intercept geometry should be reasonable
    if (plan.ShouldLaunch) {
      Vector3 interceptorDirection = plan.InterceptPosition - Vector3.zero;
      Vector3 threatDirection = agent.GetVelocity().normalized;

      // Check that it's not an extreme sideways launch
      Vector3 perpendicularComponent =
          Vector3.ProjectOnPlane(interceptorDirection, threatDirection);
      Vector3 parallelComponent = Vector3.Project(interceptorDirection, threatDirection);

      float sidewaysRatio = perpendicularComponent.magnitude / (parallelComponent.magnitude + 1f);
      Assert.IsTrue(
          sidewaysRatio < 3f,
          $"Launch geometry too sideways: ratio={sidewaysRatio}, Intercept: {plan.InterceptPosition}, Threat Vel: {agent.GetVelocity()}");
    }
  }

  [Test]
  public void TestValidLaunchFastApproachingThreat() {
    // Test case: Fast threat approaching from far away - should allow valid intercept
    // This simulates the Brahmos scenario: fast missile from far away
    Agent agent =
        GenerateAgent(position: new Vector3(0, 50, 10000), velocity: new Vector3(0, 0, -500));
    LinearExtrapolator predictor = new LinearExtrapolator(agent);
    IterativeLaunchPlanner planner = new IterativeLaunchPlanner(_launchAnglePlanner, predictor);
    LaunchPlan plan = planner.Plan();

    // This should potentially launch (depending on interpolation data) and geometry should be
    // reasonable
    if (plan.ShouldLaunch) {
      Vector3 threatDirection = agent.GetVelocity().normalized;
      Vector3 interceptorDirection = plan.InterceptPosition - Vector3.zero;

      // Verify it's not flagged as backwards when it shouldn't be
      float alignment = Vector3.Dot(interceptorDirection.normalized, threatDirection);

      // Even if alignment is high, it should be allowed if geometry makes sense
      Vector3 threatToIntercept = plan.InterceptPosition - agent.GetPosition();
      float threatToInterceptAlignment = Vector3.Dot(threatToIntercept.normalized, threatDirection);

      Assert.IsTrue(
          threatToInterceptAlignment > -0.5f,
          $"Threat should not be diverging strongly from intercept. Alignment: {threatToInterceptAlignment}, Intercept: {plan.InterceptPosition}");
    }
  }

  [Test]
  public void TestValidLaunchFromOrigin() {
    // Test the origin-aware version with a reasonable scenario
    Agent agent =
        GenerateAgent(position: new Vector3(100, 50, 500), velocity: new Vector3(0, 0, -100));
    LinearExtrapolator predictor = new LinearExtrapolator(agent);
    IterativeLaunchPlanner planner = new IterativeLaunchPlanner(_launchAnglePlanner, predictor);

    // Create a mock origin config for testing
    InterceptorOriginConfig origin =
        new InterceptorOriginConfig { id = "test-origin", initial_position = new Vector3(50, 0, 0),
                                      velocity = Vector3.zero, max_interceptors = 10,
                                      interceptor_types = new List<string> { "test.json" } };

    // Create mock origin object for testing instead of using old Plan signature
    GameObject mockOriginGameObject = new GameObject("Mock_test-origin");
    mockOriginGameObject.transform.position = origin.initial_position;
    InterceptorOriginObject originObject =
        mockOriginGameObject.AddComponent<InterceptorOriginObject>();
    originObject.SetOriginConfig(origin);

    LaunchPlan plan = planner.Plan(originObject);

    // Verify that if launch is allowed, geometry is reasonable
    if (plan.ShouldLaunch) {
      // Calculate position directly instead of using obsolete method
      Vector3 originPos = origin.initial_position + origin.velocity * 0f;
      Vector3 interceptorDirection = plan.InterceptPosition - originPos;
      Vector3 threatDirection = agent.GetVelocity().normalized;

      // Basic sanity checks on the geometry
      Assert.IsTrue(interceptorDirection.magnitude > 0, "Interceptor should have a direction");

      // Check it's not a backwards launch relative to origin
      Vector3 threatToOrigin = originPos - agent.GetPosition();
      float approachAlignment = Vector3.Dot(threatToOrigin.normalized, threatDirection);

      if (approachAlignment < -0.5f) {  // If threat approaching origin
        Vector3 originToIntercept = plan.InterceptPosition - originPos;
        float interceptBehindOrigin = Vector3.Dot(originToIntercept.normalized, -threatDirection);
        Assert.IsTrue(
            interceptBehindOrigin < 0.7f,
            $"Intercept should not be significantly behind origin. Intercept: {plan.InterceptPosition}, BehindComponent: {interceptBehindOrigin}");
      }
    }
  }

  [Test]
  public void TestNoLaunchThreatDivergingFromIntercept() {
    // Test case: Threat is moving away from where the intercept would occur
    Agent agent = GenerateAgent(position: new Vector3(50, 1, 80), velocity: new Vector3(20, 0, 0));
    LinearExtrapolator predictor = new LinearExtrapolator(agent);
    IterativeLaunchPlanner planner = new IterativeLaunchPlanner(_launchAnglePlanner, predictor);
    LaunchPlan plan = planner.Plan();

    // This should either not launch, or if it does, threat shouldn't be strongly diverging
    if (plan.ShouldLaunch) {
      Vector3 threatToIntercept = plan.InterceptPosition - agent.GetPosition();
      Vector3 threatDirection = agent.GetVelocity().normalized;
      float divergence = Vector3.Dot(threatToIntercept.normalized, threatDirection);

      Assert.IsTrue(
          divergence > -0.8f,
          $"Threat should not be strongly diverging from intercept point. Divergence: {divergence}, Intercept: {plan.InterceptPosition}");
    }
  }
}
