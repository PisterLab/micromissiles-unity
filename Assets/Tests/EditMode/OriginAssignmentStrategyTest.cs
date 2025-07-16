using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Unit tests for origin assignment strategies and multiple origin management.
/// Tests the logic for selecting appropriate interceptor origins based on different strategies.
/// </summary>
[TestFixture]
public class OriginAssignmentStrategyTest : TestBase {
  private List<InterceptorOriginConfig> _testOrigins;
  private InterceptorOriginManager _originManager;

  [SetUp]
  public void SetUp() {
    // Set up a test scenario with multiple origins representing a carrier strike group
    _testOrigins = new List<InterceptorOriginConfig> {
      // Aircraft Carrier - High capacity, multiple interceptor types
      new InterceptorOriginConfig {
        id = "CVN-68-Nimitz", initial_position = new Vector3(0, 0, 5000),
        velocity = new Vector3(0, 0, -12),  // 12 m/s forward
        max_interceptors = 24,
        interceptor_types =
            new List<string> { "hydra70.json", "micromissile.json", "f18_missile.json" }
      },
      // Destroyer - Medium capacity, specialized interceptors
      new InterceptorOriginConfig {
        id = "DDG-51-Burke", initial_position = new Vector3(-2000, 0, 4000),
        velocity = new Vector3(0, 0, -12),  // Same formation speed
        max_interceptors = 8, interceptor_types = new List<string> { "sm2.json", "sm6.json" }
      },
      // Cruiser - Medium capacity, long-range interceptors
      new InterceptorOriginConfig {
        id = "CG-47-Ticonderoga", initial_position = new Vector3(2000, 0, 4000),
        velocity = new Vector3(0, 0, -12),  // Same formation speed
        max_interceptors = 12,
        interceptor_types = new List<string> { "sm2.json", "sm6.json", "tomahawk.json" }
      },
      // Shore Battery - Static, high capacity
      new InterceptorOriginConfig {
        id = "Aegis-Ashore-1", initial_position = new Vector3(10000, 100, 0),
        velocity = Vector3.zero,  // Static installation
        max_interceptors = 48, interceptor_types = new List<string> { "sm3.json", "thaad.json" }
      }
    };

    _originManager = new InterceptorOriginManager(_testOrigins);
  }

  [Test]
  public void TestClosestOriginStrategy_SingleTarget() {
    // Test CLOSEST strategy selects the nearest available origin
    Vector3 threatPosition = new Vector3(1000, 0, 3000);
    string requestedInterceptorType = "sm2.json";

    InterceptorOriginConfig selectedOrigin = _originManager.SelectOrigin(
        threatPosition, requestedInterceptorType, OriginAssignmentStrategy.CLOSEST,
        0f  // Current time
    );

    Assert.IsNotNull(selectedOrigin);
    // CG-47-Ticonderoga should be closest to this threat position
    // Distance from (1000,0,3000) to CG-47 at (2000,0,4000) = ~1414m
    // Distance from (1000,0,3000) to DDG-51 at (-2000,0,4000) = ~3162m
    Assert.AreEqual("CG-47-Ticonderoga", selectedOrigin.id);
    Assert.IsTrue(selectedOrigin.SupportsInterceptorType(requestedInterceptorType));
  }

  [Test]
  public void TestClosestOriginStrategy_DDGCloser() {
    // Test CLOSEST strategy when DDG-51 is actually closer
    Vector3 threatPosition = new Vector3(-3000, 0, 3500);
    string requestedInterceptorType = "sm2.json";

    InterceptorOriginConfig selectedOrigin = _originManager.SelectOrigin(
        threatPosition, requestedInterceptorType, OriginAssignmentStrategy.CLOSEST,
        0f  // Current time
    );

    Assert.IsNotNull(selectedOrigin);
    // DDG-51-Burke should be closest to this threat position
    // Distance from (-3000,0,3500) to DDG-51 at (-2000,0,4000) = ~1118m
    // Distance from (-3000,0,3500) to CG-47 at (2000,0,4000) = ~5099m
    Assert.AreEqual("DDG-51-Burke", selectedOrigin.id);
    Assert.IsTrue(selectedOrigin.SupportsInterceptorType(requestedInterceptorType));
  }

  [Test]
  public void TestClosestOriginStrategy_NoCompatibleOrigin() {
    // Test CLOSEST strategy when no origin supports the requested interceptor type
    Vector3 threatPosition = new Vector3(0, 0, 4000);
    string unsupportedInterceptorType = "nonexistent_missile.json";

    InterceptorOriginConfig selectedOrigin = _originManager.SelectOrigin(
        threatPosition, unsupportedInterceptorType, OriginAssignmentStrategy.CLOSEST, 0f);

    Assert.IsNull(selectedOrigin,
                  "Should return null when no origin supports the interceptor type");
  }

  [Test]
  public void TestClosestOriginStrategy_MovingOrigins() {
    // Test CLOSEST strategy accounts for origin movement over time
    Vector3 threatPosition = new Vector3(0, 0, 2000);
    string interceptorType = "hydra70.json";
    float currentTime = 100f;  // 100 seconds into simulation

    InterceptorOriginConfig selectedOrigin = _originManager.SelectOrigin(
        threatPosition, interceptorType, OriginAssignmentStrategy.CLOSEST, currentTime);

    Assert.IsNotNull(selectedOrigin);
    Assert.AreEqual("CVN-68-Nimitz", selectedOrigin.id);

    // Verify that the distance calculation used the origin's position at the specified time
    Vector3 expectedCarrierPosition = new Vector3(0, 0, 5000 - 12 * 100);  // 5000 - 1200 = 3800
    float expectedDistance = Vector3.Distance(expectedCarrierPosition, threatPosition);
    float actualDistance = selectedOrigin.GetDistanceToTarget(threatPosition, currentTime);

    Assert.AreEqual(expectedDistance, actualDistance, 0.1f);
  }

  [Test]
  public void TestLoadBalancedStrategy() {
    // Test LOAD_BALANCED strategy distributes assignments evenly across origins
    string interceptorType = "sm2.json";  // Supported by DDG-51 and CG-47
    Vector3 threatPosition = new Vector3(0, 0, 3000);

    // Make multiple assignments and verify they're distributed
    List<string> selectedOriginIds = new List<string>();

    for (int i = 0; i < 6; i++) {
      InterceptorOriginConfig selectedOrigin = _originManager.SelectOrigin(
          threatPosition, interceptorType, OriginAssignmentStrategy.LOAD_BALANCED, 0f);

      Assert.IsNotNull(selectedOrigin);
      selectedOriginIds.Add(selectedOrigin.id);

      // Simulate interceptor allocation
      selectedOrigin.AllocateInterceptor();
    }

    // Both DDG-51 and CG-47 should have been selected
    Assert.Contains("DDG-51-Burke", selectedOriginIds);
    Assert.Contains("CG-47-Ticonderoga", selectedOriginIds);

    // Assignments should be relatively balanced
    int ddgAssignments = selectedOriginIds.FindAll(id => id == "DDG-51-Burke").Count;
    int cgAssignments = selectedOriginIds.FindAll(id => id == "CG-47-Ticonderoga").Count;

    Assert.LessOrEqual(Mathf.Abs(ddgAssignments - cgAssignments), 2,
                       "Load balancing should distribute assignments relatively evenly");
  }

  [Test]
  public void TestCapabilityBasedStrategy() {
    // Test CAPABILITY_BASED strategy selects best-suited origin for interceptor type
    Vector3 threatPosition = new Vector3(5000, 0, 8000);  // Distant threat

    // Request long-range interceptor - should prefer shore battery with SM3/THAAD
    string longRangeInterceptor = "sm3.json";
    InterceptorOriginConfig selectedOrigin = _originManager.SelectOrigin(
        threatPosition, longRangeInterceptor, OriginAssignmentStrategy.CAPABILITY_BASED, 0f);

    Assert.IsNotNull(selectedOrigin);
    Assert.AreEqual("Aegis-Ashore-1", selectedOrigin.id,
                    "Should select shore battery for long-range interceptor");

    // Request carrier-based interceptor - should prefer carrier
    string carrierInterceptor = "f18_missile.json";
    selectedOrigin = _originManager.SelectOrigin(threatPosition, carrierInterceptor,
                                                 OriginAssignmentStrategy.CAPABILITY_BASED, 0f);

    Assert.IsNotNull(selectedOrigin);
    Assert.AreEqual("CVN-68-Nimitz", selectedOrigin.id,
                    "Should select carrier for carrier-specific interceptor");
  }

  [Test]
  public void TestManualStrategy() {
    // Test MANUAL strategy uses pre-specified origin assignments
    Vector3 threatPosition = new Vector3(0, 0, 4000);
    string interceptorType = "sm2.json";
    string manuallySpecifiedOriginId = "CG-47-Ticonderoga";

    InterceptorOriginConfig selectedOrigin =
        _originManager.SelectOrigin(threatPosition, interceptorType,
                                    OriginAssignmentStrategy.MANUAL, 0f, manuallySpecifiedOriginId);

    Assert.IsNotNull(selectedOrigin);
    Assert.AreEqual(manuallySpecifiedOriginId, selectedOrigin.id);
  }

  [Test]
  public void TestManualStrategy_InvalidOriginId() {
    // Test MANUAL strategy with invalid origin ID
    Vector3 threatPosition = new Vector3(0, 0, 4000);
    string interceptorType = "sm2.json";
    string invalidOriginId = "NonExistent-Origin";

    InterceptorOriginConfig selectedOrigin = _originManager.SelectOrigin(
        threatPosition, interceptorType, OriginAssignmentStrategy.MANUAL, 0f, invalidOriginId);

    Assert.IsNull(selectedOrigin, "Should return null for invalid manually specified origin");
  }

  [Test]
  public void TestOriginCapacityConstraints() {
    // Test that origins with no remaining capacity are not selected
    Vector3 threatPosition = new Vector3(-1000, 0, 3000);
    string interceptorType = "sm2.json";

    // Exhaust DDG-51 capacity
    var ddgOrigin = _testOrigins.Find(o => o.id == "DDG-51-Burke");
    for (int i = 0; i < ddgOrigin.max_interceptors; i++) {
      ddgOrigin.AllocateInterceptor();
    }
    Assert.IsFalse(ddgOrigin.HasCapacity());

    // Request assignment - should select CG-47 instead of exhausted DDG-51
    InterceptorOriginConfig selectedOrigin = _originManager.SelectOrigin(
        threatPosition, interceptorType, OriginAssignmentStrategy.CLOSEST, 0f);

    Assert.IsNotNull(selectedOrigin);
    Assert.AreNotEqual("DDG-51-Burke", selectedOrigin.id,
                       "Should not select origin with exhausted capacity");
    Assert.AreEqual("CG-47-Ticonderoga", selectedOrigin.id,
                    "Should select next closest origin with capacity");
  }

  [Test]
  public void TestMultipleOriginFormationMovement() {
    // Test that formation movement is correctly calculated for all origins
    float timeOffset = 60f;  // 1 minute into simulation

    foreach (var origin in _testOrigins) {
      Vector3 currentPosition = origin.GetCurrentPosition(timeOffset);

      if (origin.velocity.magnitude > 0) {
        // Moving origins should have moved
        Vector3 expectedPosition = origin.initial_position + origin.velocity * timeOffset;
        Assert.AreEqual(
            expectedPosition, currentPosition,
            $"Origin {origin.id} should be at expected position after {timeOffset} seconds");
      } else {
        // Static origins should not have moved
        Assert.AreEqual(origin.initial_position, currentPosition,
                        $"Static origin {origin.id} should remain at initial position");
      }
    }
  }

  [Test]
  public void TestOriginSelection_PriorityOrder() {
    // Test that origin selection follows expected priority order
    Vector3 centralThreatPosition = new Vector3(0, 0, 4500);  // Equidistant from naval origins
    string commonInterceptorType = "sm2.json";                // Supported by DDG and CG

    // Test multiple selections to verify consistent priority
    List<string> selections = new List<string>();
    for (int i = 0; i < 10; i++) {
      InterceptorOriginConfig selectedOrigin = _originManager.SelectOrigin(
          centralThreatPosition, commonInterceptorType, OriginAssignmentStrategy.CLOSEST, 0f);
      selections.Add(selectedOrigin.id);
    }

    // All selections should be consistent (deterministic)
    string firstSelection = selections[0];
    Assert.IsTrue(selections.TrueForAll(s => s == firstSelection),
                  "Origin selection should be deterministic for identical inputs");
  }

  [Test]
  public void TestOriginManager_UpdateMovingOrigins() {
    // Test bulk update of all moving origin positions
    float deltaTime = 30f;

    // Get initial positions
    var initialPositions = new Dictionary<string, Vector3>();
    foreach (var origin in _testOrigins) {
      initialPositions[origin.id] = origin.GetCurrentPosition(0f);
    }

    // Update all moving origins
    _originManager.UpdateMovingOrigins(deltaTime);

    // Verify positions updated correctly
    foreach (var origin in _testOrigins) {
      Vector3 newPosition = origin.GetCurrentPosition(deltaTime);
      Vector3 expectedPosition = initialPositions[origin.id] + origin.velocity * deltaTime;

      Assert.AreEqual(expectedPosition, newPosition,
                      $"Origin {origin.id} position should be correctly updated");
    }
  }

  [Test]
  public void TestOriginManager_GetAvailableOrigins() {
    // Test filtering of available origins by interceptor type and capacity
    string interceptorType = "hydra70.json";

    List<InterceptorOriginConfig> availableOrigins =
        _originManager.GetAvailableOrigins(interceptorType);

    Assert.AreEqual(1, availableOrigins.Count, "Only carrier should support hydra70");
    Assert.AreEqual("CVN-68-Nimitz", availableOrigins[0].id);

    // Exhaust carrier capacity and verify it's no longer available
    var carrier = availableOrigins[0];
    for (int i = 0; i < carrier.max_interceptors; i++) {
      carrier.AllocateInterceptor();
    }

    availableOrigins = _originManager.GetAvailableOrigins(interceptorType);
    Assert.AreEqual(0, availableOrigins.Count,
                    "No origins should be available when capacity is exhausted");
  }
}
