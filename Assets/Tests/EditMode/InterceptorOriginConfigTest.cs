using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Unit tests for InterceptorOriginConfig and related origin management functionality.
/// These tests follow Test Driven Development principles and should initially fail
/// until the corresponding implementation is completed.
/// </summary>
[TestFixture]
public class InterceptorOriginConfigTest : TestBase {

    [Test]
    public void TestInterceptorOriginConfig_GetCurrentPosition_StaticOrigin() {
        // Test that static origins (zero velocity) return initial position regardless of time
        var staticOrigin = new InterceptorOriginConfig {
            id = "Shore-Battery-Alpha",
            initial_position = new Vector3(5000, 100, 0),
            velocity = Vector3.zero,
            max_interceptors = 50,
            interceptor_types = new List<string> { "patriot.json" }
        };

        Vector3 positionAtT0 = staticOrigin.GetCurrentPosition(0f);
        Vector3 positionAtT100 = staticOrigin.GetCurrentPosition(100f);

        Assert.AreEqual(new Vector3(5000, 100, 0), positionAtT0);
        Assert.AreEqual(new Vector3(5000, 100, 0), positionAtT100);
    }

    [Test]
    public void TestInterceptorOriginConfig_GetCurrentPosition_MovingOrigin() {
        // Test that moving origins calculate position based on velocity and time
        var movingOrigin = new InterceptorOriginConfig {
            id = "DDG-51-Burke",
            initial_position = new Vector3(3000, 0, 6000),
            velocity = new Vector3(0, 0, -15), // Moving at 15 m/s in negative Z direction
            max_interceptors = 8,
            interceptor_types = new List<string> { "sm2.json" }
        };

        Vector3 positionAtT0 = movingOrigin.GetCurrentPosition(0f);
        Vector3 positionAtT10 = movingOrigin.GetCurrentPosition(10f);
        Vector3 positionAtT60 = movingOrigin.GetCurrentPosition(60f);

        Assert.AreEqual(new Vector3(3000, 0, 6000), positionAtT0);
        Assert.AreEqual(new Vector3(3000, 0, 5850), positionAtT10, "Position after 10 seconds should move 150m in -Z");
        Assert.AreEqual(new Vector3(3000, 0, 5100), positionAtT60, "Position after 60 seconds should move 900m in -Z");
    }

    [Test]
    public void TestInterceptorOriginConfig_SupportsInterceptorType() {
        // Test interceptor type compatibility checking
        var origin = new InterceptorOriginConfig {
            id = "Multi-Role-Platform",
            initial_position = Vector3.zero,
            velocity = Vector3.zero,
            max_interceptors = 15,
            interceptor_types = new List<string> { "hydra70.json", "micromissile.json", "patriot.json" }
        };

        Assert.IsTrue(origin.SupportsInterceptorType("hydra70.json"));
        Assert.IsTrue(origin.SupportsInterceptorType("micromissile.json"));
        Assert.IsTrue(origin.SupportsInterceptorType("patriot.json"));
        Assert.IsFalse(origin.SupportsInterceptorType("sm2.json"));
        Assert.IsFalse(origin.SupportsInterceptorType("nonexistent.json"));
    }

    [Test]
    public void TestInterceptorOriginConfig_HasCapacity() {
        // Test capacity management for interceptor allocation
        var origin = new InterceptorOriginConfig {
            id = "Limited-Capacity-Origin",
            initial_position = Vector3.zero,
            velocity = Vector3.zero,
            max_interceptors = 5,
            interceptor_types = new List<string> { "hydra70.json" }
        };

        // Initially should have full capacity
        Assert.IsTrue(origin.HasCapacity());
        Assert.AreEqual(5, origin.GetAvailableCapacity());

        // Allocate some interceptors
        origin.AllocateInterceptor();
        origin.AllocateInterceptor();
        origin.AllocateInterceptor();

        Assert.IsTrue(origin.HasCapacity());
        Assert.AreEqual(2, origin.GetAvailableCapacity());

        // Allocate remaining capacity
        origin.AllocateInterceptor();
        origin.AllocateInterceptor();

        Assert.IsFalse(origin.HasCapacity());
        Assert.AreEqual(0, origin.GetAvailableCapacity());

        // Attempting to allocate beyond capacity should fail
        Assert.IsFalse(origin.AllocateInterceptor());
    }

    [Test]
    public void TestInterceptorOriginConfig_ReleaseInterceptor() {
        // Test releasing interceptors back to the pool
        var origin = new InterceptorOriginConfig {
            id = "Release-Test-Origin",
            initial_position = Vector3.zero,
            velocity = Vector3.zero,
            max_interceptors = 3,
            interceptor_types = new List<string> { "hydra70.json" }
        };

        // Allocate all capacity
        origin.AllocateInterceptor();
        origin.AllocateInterceptor();
        origin.AllocateInterceptor();
        Assert.IsFalse(origin.HasCapacity());

        // Release one interceptor
        origin.ReleaseInterceptor();
        Assert.IsTrue(origin.HasCapacity());
        Assert.AreEqual(1, origin.GetAvailableCapacity());

        // Release all interceptors
        origin.ReleaseInterceptor();
        origin.ReleaseInterceptor();
        Assert.AreEqual(3, origin.GetAvailableCapacity());

        // Releasing beyond max capacity should not exceed max
        origin.ReleaseInterceptor(); // Should not increase beyond max_interceptors
        Assert.AreEqual(3, origin.GetAvailableCapacity());
    }

    [Test]
    public void TestOriginAssignmentStrategy_Enum() {
        // Test that OriginAssignmentStrategy enum values are properly defined
        Assert.IsTrue(System.Enum.IsDefined(typeof(OriginAssignmentStrategy), OriginAssignmentStrategy.CLOSEST));
        Assert.IsTrue(System.Enum.IsDefined(typeof(OriginAssignmentStrategy), OriginAssignmentStrategy.LOAD_BALANCED));
        Assert.IsTrue(System.Enum.IsDefined(typeof(OriginAssignmentStrategy), OriginAssignmentStrategy.CAPABILITY_BASED));
        Assert.IsTrue(System.Enum.IsDefined(typeof(OriginAssignmentStrategy), OriginAssignmentStrategy.MANUAL));
    }

    [Test]
    public void TestInterceptorOriginConfig_JsonSerialization() {
        // Test that InterceptorOriginConfig can be properly serialized/deserialized
        var originalOrigin = new InterceptorOriginConfig {
            id = "Serialization-Test",
            initial_position = new Vector3(1000, 500, 2000),
            velocity = new Vector3(5, 0, -10),
            max_interceptors = 25,
            interceptor_types = new List<string> { "test1.json", "test2.json" }
        };

        // Configure JsonSerializerSettings to handle Unity Vector3 circular references
        var settings = new JsonSerializerSettings {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() }
        };

        // Serialize to JSON
        string json = JsonConvert.SerializeObject(originalOrigin, Formatting.Indented, settings);
        Assert.IsNotEmpty(json);

        // Deserialize from JSON
        var deserializedOrigin = JsonConvert.DeserializeObject<InterceptorOriginConfig>(json, settings);

        // Verify all properties are correctly deserialized
        Assert.AreEqual(originalOrigin.id, deserializedOrigin.id);
        Assert.AreEqual(originalOrigin.initial_position, deserializedOrigin.initial_position);
        Assert.AreEqual(originalOrigin.velocity, deserializedOrigin.velocity);
        Assert.AreEqual(originalOrigin.max_interceptors, deserializedOrigin.max_interceptors);
        CollectionAssert.AreEqual(originalOrigin.interceptor_types, deserializedOrigin.interceptor_types);
    }

    [Test]
    public void TestInterceptorOriginConfig_DistanceCalculation() {
        // Test distance calculation from origin to target position
        var origin = new InterceptorOriginConfig {
            id = "Distance-Test",
            initial_position = new Vector3(0, 0, 0),
            velocity = Vector3.zero,
            max_interceptors = 10,
            interceptor_types = new List<string> { "test.json" }
        };

        Vector3 targetPosition1 = new Vector3(3, 4, 0); // Should be distance 5
        Vector3 targetPosition2 = new Vector3(0, 0, 10); // Should be distance 10

        float distance1 = origin.GetDistanceToTarget(targetPosition1, 0f);
        float distance2 = origin.GetDistanceToTarget(targetPosition2, 0f);

        Assert.AreEqual(5f, distance1, 0.001f);
        Assert.AreEqual(10f, distance2, 0.001f);
    }

    [Test]
    public void TestInterceptorOriginConfig_DistanceCalculation_MovingOrigin() {
        // Test distance calculation for moving origins at different times
        var movingOrigin = new InterceptorOriginConfig {
            id = "Moving-Distance-Test",
            initial_position = new Vector3(0, 0, 0),
            velocity = new Vector3(1, 0, 0), // Moving 1 m/s in +X direction
            max_interceptors = 10,
            interceptor_types = new List<string> { "test.json" }
        };

        Vector3 fixedTarget = new Vector3(10, 0, 0);

        float distanceAtT0 = movingOrigin.GetDistanceToTarget(fixedTarget, 0f);
        float distanceAtT5 = movingOrigin.GetDistanceToTarget(fixedTarget, 5f);

        Assert.AreEqual(10f, distanceAtT0, 0.001f, "Distance at t=0 should be 10m");
        Assert.AreEqual(5f, distanceAtT5, 0.001f, "Distance at t=5 should be 5m (origin moved 5m closer)");
    }
}