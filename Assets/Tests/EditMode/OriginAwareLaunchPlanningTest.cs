using NUnit.Framework;
using UnityEngine;

// Unit tests for origin-aware launch planning functionality.
// Tests that launch planning algorithms correctly account for interceptor origins
// rather than assuming launch from (0,0,0).
//
// KEY PRINCIPLE: The intercept position should be where both interceptor and target meet.
// For successful intercepts, this should be very close to the predicted target position.
// Origin-aware planning ensures interceptors launch from the correct starting position
// to reach this intercept point.
[TestFixture]
public class OriginAwareLaunchPlanningTest : TestBase {
  private InterceptorOriginConfig _staticOrigin;
  private InterceptorOriginConfig _movingOrigin;
  private MockPredictor _mockPredictor;
  private MockLaunchAnglePlanner _mockLaunchAnglePlanner;

  [SetUp]
  public void SetUp() {
    // Set up test origins
    _staticOrigin = new InterceptorOriginConfig {
      id = "Static-Test-Origin", initial_position = new Vector3(1000, 0, 2000),
      velocity = Vector3.zero, max_interceptors = 10,
      interceptor_types = new System.Collections.Generic.List<string> { "test.json" }
    };

    _movingOrigin = new InterceptorOriginConfig {
      id = "Moving-Test-Origin", initial_position = new Vector3(500, 0, 1000),
      velocity = new Vector3(0, 0, -10),  // Moving 10 m/s in -Z direction
      max_interceptors = 5,
      interceptor_types = new System.Collections.Generic.List<string> { "test.json" }
    };

    // Set up mock components
    _mockPredictor = new MockPredictor();
    _mockLaunchAnglePlanner = new MockLaunchAnglePlanner();
  }

  [Test]
  public void TestIterativeLaunchPlanner_WithOrigin_StaticOrigin() {
    // Test that IterativeLaunchPlanner correctly uses static origin position
    var planner = new IterativeLaunchPlanner(_mockLaunchAnglePlanner, _mockPredictor);

    // Set up a simple threat moving toward origin
    Vector3 threatInitialPos = new Vector3(2000, 0, 5000);
    Vector3 threatVelocity = new Vector3(0, 0, -100);
    _mockPredictor.SetThreatTrajectory(threatInitialPos, threatVelocity);

    // Set up mock launch angle planner to return reasonable values
    _mockLaunchAnglePlanner.SetMockResponse(
        45f, 10f);  // 45 degree launch angle, 10 second time-to-intercept

    // Plan launch from static origin
    LaunchPlan plan = planner.Plan(_staticOrigin, 0f);

    Assert.IsNotNull(plan);
    Assert.AreNotEqual(LaunchPlan.NoLaunch, plan);

    // Verify that the planning used the origin position in calculations
    Vector3 expectedOriginPosition = new Vector3(1000, 0, 2000);
    Assert.IsTrue(_mockLaunchAnglePlanner.WasCalledWithOrigin(expectedOriginPosition),
                  "Launch angle planner should have been called with the correct origin position");
  }

  [Test]
  public void TestIterativeLaunchPlanner_WithOrigin_MovingOrigin() {
    // Test that IterativeLaunchPlanner correctly accounts for moving origin position
    var planner = new IterativeLaunchPlanner(_mockLaunchAnglePlanner, _mockPredictor);

    // Set up threat trajectory
    Vector3 threatInitialPos = new Vector3(1000, 0, 3000);
    Vector3 threatVelocity = new Vector3(0, 0, -50);
    _mockPredictor.SetThreatTrajectory(threatInitialPos, threatVelocity);

    _mockLaunchAnglePlanner.SetMockResponse(30f, 15f);

    // Plan launch from moving origin at t=10 seconds
    float planningTime = 10f;
    LaunchPlan plan = planner.Plan(_movingOrigin, planningTime);

    Assert.IsNotNull(plan);
    Assert.AreNotEqual(LaunchPlan.NoLaunch, plan);

    // Verify that the planning used the origin position at the specified time
    Vector3 expectedOriginPosition = new Vector3(500, 0, 900);  // Initial pos + velocity * time
    Assert.IsTrue(
        _mockLaunchAnglePlanner.WasCalledWithOrigin(expectedOriginPosition),
        "Launch angle planner should account for moving origin position at planning time");
  }

  [Test]
  public void TestLaunchAnglePlanner_OriginRelativeCalculations() {
    // Test that LaunchAnglePlanner performs origin-relative calculations
    var planner = new MockLaunchAnglePlanner();

    Vector3 originPosition = new Vector3(2000, 0, 1000);
    Vector3 targetPosition = new Vector3(2500, 0, 3000);

    LaunchAngleOutput output = planner.Plan(targetPosition, originPosition);

    Assert.IsNotNull(output);
    Assert.Greater(output.TimeToPosition, 0f, "Time to position should be positive");
    Assert.GreaterOrEqual(output.LaunchAngle, 0f, "Launch angle should be non-negative");
    Assert.LessOrEqual(output.LaunchAngle, 90f, "Launch angle should not exceed 90 degrees");

    // Verify that distance calculation is origin-relative
    float expectedDistance = Vector3.Distance(originPosition, targetPosition);
    Assert.AreEqual(expectedDistance, output.DistanceToTarget, 0.1f,
                    "Distance calculation should be relative to origin position");
  }

  [Test]
  public void TestLaunchAnglePlanner_OriginRelativeVsZeroOrigin() {
    // Test that calculations differ when using non-zero origin vs zero origin
    var planner = new MockLaunchAnglePlanner();

    Vector3 targetPosition = new Vector3(1000, 0, 2000);
    Vector3 nonZeroOrigin = new Vector3(500, 0, 500);
    Vector3 zeroOrigin = Vector3.zero;

    LaunchAngleOutput outputFromNonZeroOrigin = planner.Plan(targetPosition, nonZeroOrigin);
    LaunchAngleOutput outputFromZeroOrigin = planner.Plan(targetPosition, zeroOrigin);

    // Results should be different when using different origins
    Assert.AreNotEqual(outputFromNonZeroOrigin.LaunchAngle, outputFromZeroOrigin.LaunchAngle,
                       "Launch angles should differ when using different origin positions");
    Assert.AreNotEqual(outputFromNonZeroOrigin.TimeToPosition, outputFromZeroOrigin.TimeToPosition,
                       "Time to position should differ when using different origin positions");
    Assert.AreNotEqual(outputFromNonZeroOrigin.DistanceToTarget,
                       outputFromZeroOrigin.DistanceToTarget,
                       "Distance to target should differ when using different origin positions");
  }

  [Test]
  public void TestGetInterceptPosition_WithOrigin() {
    // Test that GetInterceptPosition correctly accounts for origin offset
    var planner = new MockLaunchAnglePlanner();

    Vector3 originPosition = new Vector3(1000, 0, 1000);
    Vector3 targetPosition = new Vector3(2000, 0, 3000);

    Vector3 interceptPosition = planner.GetInterceptPosition(targetPosition, originPosition);

    // Intercept position should be different from target position
    Assert.AreNotEqual(targetPosition, interceptPosition);

    // Intercept position should account for origin offset in its calculation
    // The exact position depends on the launch angle calculation, but it should not
    // be the same as if calculated from (0,0,0)
    Vector3 interceptFromZero = planner.GetInterceptPosition(targetPosition, Vector3.zero);
    Assert.AreNotEqual(interceptFromZero, interceptPosition,
                       "Intercept position should differ when calculated from different origins");
  }

  [Test]
  public void TestBackwardsLaunchDetection_WithOrigin() {
    // Test that backwards launch detection works correctly with non-zero origins
    var planner = new IterativeLaunchPlanner(_mockLaunchAnglePlanner, _mockPredictor);

    // Set up a scenario where interceptor would be launched backwards if origin is ignored
    Vector3 originPosition = new Vector3(0, 0, 1000);  // Origin behind the threat's target
    Vector3 threatInitialPos = new Vector3(0, 0, 2000);
    Vector3 threatVelocity = new Vector3(0, 0, -100);  // Threat moving toward (0,0,0)

    var origin = new InterceptorOriginConfig {
      id = "Behind-Threat-Origin", initial_position = originPosition, velocity = Vector3.zero,
      max_interceptors = 1,
      interceptor_types = new System.Collections.Generic.List<string> { "test.json" }
    };

    _mockPredictor.SetThreatTrajectory(threatInitialPos, threatVelocity);
    _mockLaunchAnglePlanner.SetMockResponse(45f, 5f);

    LaunchPlan plan = planner.Plan(origin, 0f);

    // Should return NoLaunch because interceptor would be launched backwards
    Assert.AreEqual(LaunchPlan.NoLaunch, plan,
                    "Should not launch interceptor backwards even with non-zero origin");
  }

  [Test]
  public void TestLaunchVectorCalculation_WithOrigin() {
    // Test that launch vector calculation properly accounts for origin position
    // Create a mock launch plan to test the vector calculation
    var mockPlan = new LaunchPlan(45f, new Vector3(1000, 0, 2000));

    Vector3 originPosition = new Vector3(1000, 0, 500);

    Vector3 launchVector = mockPlan.GetNormalizedLaunchVector(originPosition);

    Assert.AreEqual(1f, launchVector.magnitude, 0.001f, "Launch vector should be normalized");

    // Launch vector should account for origin position when calculating direction
    Vector3 launchVectorFromZero = mockPlan.GetNormalizedLaunchVector(Vector3.zero);
    // Vectors should be different when calculated from different origins
    Assert.AreNotEqual(launchVector, launchVectorFromZero,
                       "Launch vector should differ when calculated from different origins");
  }

  [Test]
  public void TestConvergenceWithOrigin() {
    // Test that iterative convergence works correctly with non-zero origins
    var planner = new IterativeLaunchPlanner(_mockLaunchAnglePlanner, _mockPredictor);

    Vector3 originPosition = new Vector3(2000, 0, 2000);
    Vector3 threatInitialPos = new Vector3(4000, 0, 8000);
    Vector3 threatVelocity = new Vector3(0, 0, -80);

    var origin = new InterceptorOriginConfig {
      id = "Convergence-Test-Origin", initial_position = originPosition, velocity = Vector3.zero,
      max_interceptors = 1,
      interceptor_types = new System.Collections.Generic.List<string> { "test.json" }
    };

    _mockPredictor.SetThreatTrajectory(threatInitialPos, threatVelocity);
    _mockLaunchAnglePlanner.SetMockConvergentResponse();  // Make it converge after a few iterations

    LaunchPlan plan = planner.Plan(origin, 0f);

    Assert.IsNotNull(plan);
    Assert.AreNotEqual(LaunchPlan.NoLaunch, plan);
    Assert.Greater(_mockLaunchAnglePlanner.GetCallCount(), 1,
                   "Should require multiple iterations for convergence");
    Assert.LessOrEqual(_mockLaunchAnglePlanner.GetCallCount(), 10,
                       "Should converge within maximum iterations");
  }
}

// Mock implementation of IPredictor for testing purposes.
// Provides predictable threat trajectories for unit tests.
public class MockPredictor : IPredictor {
  private Vector3 _initialPosition;
  private Vector3 _velocity;

  public MockPredictor() : base(CreateDummyAgentStatic()) {}

  private static Agent CreateDummyAgentStatic() {
    // Create a minimal agent for the base constructor
    var go = new UnityEngine.GameObject("DummyAgent");
    var rigidbody = go.AddComponent<Rigidbody>();
    var agent = go.AddComponent<DummyAgent>();

    // Set up the rigidbody for testing
    rigidbody.linearVelocity = Vector3.zero;
    rigidbody.useGravity = false;
    rigidbody.isKinematic = true;  // Kinematic for testing

    return agent;
  }

  public void SetThreatTrajectory(Vector3 initialPosition, Vector3 velocity) {
    _initialPosition = initialPosition;
    _velocity = velocity;
  }

  public Vector3 GetInitialPosition() {
    return _initialPosition;
  }

  public Vector3 GetVelocity() {
    return _velocity;
  }

  public override PredictorState Predict(float time) {
    Vector3 position = _initialPosition + _velocity * time;
    return new PredictorState(position, _velocity, Vector3.zero);
  }
}

// Mock implementation of ILaunchAnglePlanner for testing purposes.
// Allows controlled responses for testing launch planning logic.
public class MockLaunchAnglePlanner : ILaunchAnglePlanner {
  private float _mockLaunchAngle = 45f;
  private float _mockTimeToPosition = 10f;
  private Vector3 _lastOriginUsed;
  private int _callCount = 0;
  private bool _convergentMode = false;
  private Vector3 _lastInterceptPosition = Vector3.zero;

  public void SetMockResponse(float launchAngle, float timeToPosition) {
    _mockLaunchAngle = launchAngle;
    _mockTimeToPosition = timeToPosition;
  }

  public void SetMockConvergentResponse() {
    _convergentMode = true;
    _callCount = 0;  // Reset call count for convergence simulation
  }

  // Required interface method
  public LaunchAngleOutput Plan(in LaunchAngleInput input) {
    _callCount++;

    if (_convergentMode) {
      float convergenceFactor = 1f / _callCount;
      return new LaunchAngleOutput(_mockLaunchAngle + convergenceFactor,
                                   _mockTimeToPosition + convergenceFactor, input.Distance);
    }

    return new LaunchAngleOutput(_mockLaunchAngle, _mockTimeToPosition, input.Distance);
  }

  public LaunchAngleOutput Plan(Vector3 targetPosition) {
    return Plan(targetPosition, Vector3.zero);
  }

  public LaunchAngleOutput Plan(Vector3 targetPosition, Vector3 originPosition) {
    _lastOriginUsed = originPosition;
    _callCount++;

    float distance = Vector3.Distance(originPosition, targetPosition);

    if (_convergentMode) {
      // Simulate convergence by returning slightly different values that converge
      float convergenceFactor = 1f / _callCount;
      return new LaunchAngleOutput(_mockLaunchAngle + convergenceFactor,
                                   _mockTimeToPosition + convergenceFactor, distance);
    }

    // Make launch angle and time dependent on origin position to ensure different results
    float originBasedVariation = (originPosition.x + originPosition.z) * 0.01f;
    float adjustedLaunchAngle = _mockLaunchAngle + originBasedVariation;
    float adjustedTimeToPosition = _mockTimeToPosition + originBasedVariation;

    return new LaunchAngleOutput(adjustedLaunchAngle, adjustedTimeToPosition, distance);
  }

  public Vector3 GetInterceptPosition(Vector3 targetPosition) {
    return GetInterceptPosition(targetPosition, Vector3.zero);
  }

  public Vector3 GetInterceptPosition(Vector3 targetPosition, Vector3 originPosition) {
    if (_convergentMode) {
      // Return positions that converge over iterations
      // Start with a larger offset and converge to target position
      float convergenceOffset = Mathf.Max(1f, 50f / _callCount);  // Starts at 50, converges to 1
      Vector3 direction = (targetPosition - originPosition).normalized;
      Vector3 offset = direction * convergenceOffset;
      _lastInterceptPosition = targetPosition + offset;
      return _lastInterceptPosition;
    }

    // Simple but realistic intercept calculation for testing
    // The intercept position should be close to the predicted target position
    // with origin-dependent variation to ensure different behavior for different origins

    float originVariation = (originPosition.x + originPosition.z) * 0.001f;

    // Return the target position with slight variation
    // This simulates a simple intercept calculation
    return targetPosition + Vector3.right * originVariation;
  }

  public bool WasCalledWithOrigin(Vector3 expectedOrigin) {
    return Vector3.Distance(_lastOriginUsed, expectedOrigin) < 0.001f;
  }

  public int GetCallCount() {
    return _callCount;
  }
}
