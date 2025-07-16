using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Configuration for an interceptor origin (launch platform).
/// Supports both static origins (shore batteries) and moving origins (naval assets).
/// 
/// An interceptor origin represents a platform capable of launching interceptors,
/// such as:
/// - Naval vessels (aircraft carriers, destroyers, cruisers)
/// - Shore-based installations (Aegis Ashore, Patriot batteries)
/// - Mobile platforms (TEL vehicles)
/// 
/// Each origin has:
/// - A unique identifier for reference
/// - Initial position and optional velocity for movement
/// - Launch capacity constraints
/// - Supported interceptor types
/// </summary>
[Serializable]
public class InterceptorOriginConfig {
    /// <summary>
    /// Unique identifier for this origin (e.g., "CVN-68-Nimitz", "Aegis-Ashore-1").
    /// Used for manual assignment and tracking.
    /// </summary>
    public string id;

    /// <summary>
    /// Initial position of the origin at simulation start (in world coordinates).
    /// For moving origins, this represents the starting position.
    /// </summary>
    public Vector3 initial_position;

    /// <summary>
    /// Velocity vector for moving origins (in meters per second).
    /// Use Vector3.zero for static installations.
    /// For naval formations, all ships typically have the same velocity.
    /// </summary>
    public Vector3 velocity;

    /// <summary>
    /// Maximum number of interceptors this origin can have active simultaneously.
    /// Represents launch capacity constraints (e.g., VLS cells, reload limitations).
    /// </summary>
    public int max_interceptors;

    /// <summary>
    /// List of interceptor model files that this origin can launch.
    /// Different platforms support different interceptor types.
    /// Examples: ["sm2.json", "sm6.json"] for destroyers, ["hydra70.json"] for aircraft.
    /// </summary>
    public List<string> interceptor_types;

    /// <summary>
    /// Current number of allocated interceptors. Used for capacity management.
    /// This tracks how many interceptors are currently assigned to this origin.
    /// </summary>
    [JsonIgnore]
    private int _allocated_interceptors = 0;

    /// <summary>
    /// Default constructor for JSON deserialization.
    /// </summary>
    public InterceptorOriginConfig() {
        interceptor_types = new List<string>();
    }

    /// <summary>
    /// Gets the current position of this origin at the specified time.
    /// For static origins, returns the initial position.
    /// For moving origins, calculates position based on velocity and elapsed time.
    /// </summary>
    /// <param name="currentTime">Current simulation time in seconds</param>
    /// <returns>Current position of the origin</returns>
    public Vector3 GetCurrentPosition(float currentTime) {
        return initial_position + velocity * currentTime;
    }

    /// <summary>
    /// Checks if this origin supports launching the specified interceptor type.
    /// </summary>
    /// <param name="interceptorType">Model filename of the interceptor (e.g., "sm2.json")</param>
    /// <returns>True if this origin can launch the specified interceptor type</returns>
    public bool SupportsInterceptorType(string interceptorType) {
        return interceptor_types.Contains(interceptorType);
    }

    /// <summary>
    /// Checks if this origin has available capacity to launch additional interceptors.
    /// </summary>
    /// <returns>True if capacity is available, false if at maximum capacity</returns>
    public bool HasCapacity() {
        return _allocated_interceptors < max_interceptors;
    }

    /// <summary>
    /// Gets the number of interceptors that can still be allocated to this origin.
    /// </summary>
    /// <returns>Available capacity (max_interceptors - allocated_interceptors)</returns>
    public int GetAvailableCapacity() {
        return max_interceptors - _allocated_interceptors;
    }

    /// <summary>
    /// Allocates an interceptor to this origin, reducing available capacity.
    /// This should be called when an interceptor is launched from this origin.
    /// </summary>
    /// <returns>True if allocation successful, false if no capacity available</returns>
    public bool AllocateInterceptor() {
        if (HasCapacity()) {
            _allocated_interceptors++;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Releases an interceptor from this origin, increasing available capacity.
    /// This should be called when an interceptor mission is complete or interceptor is destroyed.
    /// </summary>
    public void ReleaseInterceptor() {
        if (_allocated_interceptors > 0) {
            _allocated_interceptors--;
        }
    }

    /// <summary>
    /// Calculates the distance from this origin to a target position at the specified time.
    /// Accounts for origin movement for moving platforms.
    /// </summary>
    /// <param name="targetPosition">Position of the target</param>
    /// <param name="currentTime">Current simulation time</param>
    /// <returns>Distance in meters from origin to target</returns>
    public float GetDistanceToTarget(Vector3 targetPosition, float currentTime) {
        Vector3 currentPosition = GetCurrentPosition(currentTime);
        return Vector3.Distance(currentPosition, targetPosition);
    }

    /// <summary>
    /// Resets the allocation counter. Useful for simulation resets or testing.
    /// </summary>
    public void ResetAllocations() {
        _allocated_interceptors = 0;
    }

    /// <summary>
    /// Gets a string representation of this origin configuration for debugging.
    /// </summary>
    /// <returns>Formatted string with key origin information</returns>
    public override string ToString() {
        return $"InterceptorOrigin[{id}] at {initial_position}, " +
               $"capacity: {_allocated_interceptors}/{max_interceptors}, " +
               $"types: [{string.Join(", ", interceptor_types)}]";
    }

    /// <summary>
    /// Validates this origin configuration for common errors.
    /// </summary>
    /// <returns>List of validation error messages, empty if valid</returns>
    public List<string> Validate() {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(id)) {
            errors.Add("Origin ID cannot be null or empty");
        }

        if (max_interceptors <= 0) {
            errors.Add("Maximum interceptors must be greater than zero");
        }

        if (interceptor_types == null || interceptor_types.Count == 0) {
            errors.Add("At least one interceptor type must be specified");
        }

        // Check for duplicate interceptor types
        var uniqueTypes = new HashSet<string>(interceptor_types);
        if (uniqueTypes.Count != interceptor_types.Count) {
            errors.Add("Duplicate interceptor types detected");
        }

        return errors;
    }
}

/// <summary>
/// Enumeration of available origin assignment strategies.
/// These strategies determine how interceptors are assigned to origins when multiple options are available.
/// </summary>
[JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
public enum OriginAssignmentStrategy {
    /// Assign interceptors to the closest available origin that supports the interceptor type.
    /// Accounts for origin movement when calculating distances.
    CLOSEST,

    /// Distribute interceptor assignments evenly across all capable origins.
    /// Helps prevent overloading any single platform and maintains flexibility.
    LOAD_BALANCED,

    /// Select origins based on their capability match with the interceptor type.
    /// Prefers specialized platforms (e.g., long-range interceptors from shore batteries).
    CAPABILITY_BASED,

    /// Use manually specified origin assignments from configuration.
    /// Allows precise control over interceptor deployment.
    MANUAL
}

