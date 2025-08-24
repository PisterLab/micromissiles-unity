using NUnit.Framework;
using UnityEngine;

// Unit tests for launcher-aware launch planning functionality.
// Tests that launch planning algorithms correctly account for launcher positions
// rather than assuming launch from (0,0,0).
//
// KEY PRINCIPLE: The intercept position should be where both interceptor and target meet.
// For successful intercepts, this should be very close to the predicted target position.
// Launcher-aware planning ensures interceptors launch from the correct starting position
// to reach this intercept point.
[TestFixture]
public class LauncherAwareLaunchPlanningTest : TestBase {
  private LauncherConfig _staticLauncher;
  private LauncherConfig _movingLauncher;
  private MockPredictor _mockPredictor;
  private MockLaunchAnglePlanner _mockLaunchAnglePlanner;

  [SetUp]
  public void SetUp() {
    // Set up test origins
    _staticLauncher = new LauncherConfig {
      id = "Static-Test-Launcher", initial_position = new Vector3(1000, 0, 2000),
      velocity = Vector3.zero, max_interceptors = 10,
      interceptor_types = new System.Collections.Generic.List<string> { "test.json" }
    };

    _movingLauncher = new LauncherConfig {
      id = "Moving-Test-Launcher", initial_position = new Vector3(500, 0, 1000),
      velocity = new Vector3(0, 0, -10),  // Moving 10 m/s in -Z direction
      max_interceptors = 5,
      interceptor_types = new System.Collections.Generic.List<string> { "test.json" }
    };

    // Set up mock components
    _mockPredictor = new MockPredictor();
    _mockLaunchAnglePlanner = new MockLaunchAnglePlanner();
  }

  // Helper method to create a mock Launcher for testing
  private Launcher CreateMockOriginObject(LauncherConfig config,
                                                   float currentTime = 0f) {
    // Create a GameObject to serve as the mock origin object
    GameObject mockOriginGameObject = new GameObject($"Mock_{config.id}");

    // Calculate position accounting for time and velocity
    Vector3 currentPosition = config.initial_position + config.velocity * currentTime;
    mockOriginGameObject.transform.position = currentPosition;

    // Add the Launcher component
    Launcher originObject = mockOriginGameObject.AddComponent<Launcher>();
    mockOriginGameObject.AddComponent<Rigidbody>();
    originObject.SetLauncherConfig(config);

    return originObject;
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

    // Plan launch from static origin using mock origin object
    Launcher staticOriginObject = CreateMockOriginObject(_staticLauncher);
    LaunchPlan plan = planner.Plan(staticOriginObject);

    Assert.AreEqual(
        LaunchPlan.NoLaunch, plan,
        "Planner should return NoLaunch for an intercept geometrically behind the origin.");
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

    // Plan launch from moving origin at t=10 seconds using mock origin object
    float planningTime = 10f;
    Launcher movingOriginObject = CreateMockOriginObject(_movingLauncher, planningTime);
    LaunchPlan plan = planner.Plan(movingOriginObject);

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
    Vector3 originPosition = new Vector3(0, 0, 1000);  // Launcher behind the threat's target
    Vector3 threatInitialPos = new Vector3(0, 0, 2000);
    Vector3 threatVelocity = new Vector3(0, 0, -100);  // Threat moving toward (0,0,0)

    var origin = new LauncherConfig {
      id = "Behind-Threat-Launcher", initial_position = originPosition, velocity = Vector3.zero,
      max_interceptors = 1,
      interceptor_types = new System.Collections.Generic.List<string> { "test.json" }
    };

    _mockPredictor.SetThreatTrajectory(threatInitialPos, threatVelocity);
    _mockLaunchAnglePlanner.SetMockResponse(45f, 5f);

    // Use mock origin object instead of config and time
    Launcher originObject = CreateMockOriginObject(origin);
    LaunchPlan plan = planner.Plan(originObject);

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

    var origin = new LauncherConfig {
      id = "Convergence-Test-Launcher", initial_position = originPosition, velocity = Vector3.zero,
      max_interceptors = 1,
      interceptor_types = new System.Collections.Generic.List<string> { "test.json" }
    };

    _mockPredictor.SetThreatTrajectory(threatInitialPos, threatVelocity);
    _mockLaunchAnglePlanner.SetMockConvergentResponse();  // Make it converge after a few iterations

    // Use mock origin object instead of config and time
    Launcher originObject = CreateMockOriginObject(origin);
    LaunchPlan plan = planner.Plan(originObject);

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
