using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

// Configuration for an interceptor origin (launch platform).
// Supports both static origins (shore batteries) and moving origins (naval assets).
//
// An interceptor origin represents a platform capable of launching interceptors,
// such as:
// - Naval vessels (aircraft carriers, destroyers, cruisers)
// - Shore-based installations (Aegis Ashore, Patriot batteries)
// - Mobile platforms (TEL vehicles)
//
// Each origin has:
// - A unique identifier for reference
// - Initial position and optional velocity for movement
// - Launch capacity constraints
// - Supported interceptor types
// - Optional randomization for position and velocity
[Serializable]
public class InterceptorOriginConfig {
  // Unique identifier for this origin (e.g., "CVN-68-Nimitz", "Aegis-Ashore-1").
  // Used for manual assignment and tracking.
  public string id;

  // Initial position of the origin at simulation start (in world coordinates).
  // For moving origins, this represents the starting position.
  public Vector3 initial_position;

  // Velocity vector for moving origins (in meters per second).
  // Use Vector3.zero for static installations.
  // For naval formations, all ships typically have the same velocity.
  public Vector3 velocity;

  // Maximum number of interceptors this origin can have active simultaneously.
  // Represents launch capacity constraints (e.g., VLS cells, reload limitations).
  public int max_interceptors;

  // List of interceptor model files that this origin can launch.
  // Different platforms support different interceptor types.
  // Examples: ["sm2.json", "sm6.json"] for destroyers, ["hydra70.json"] for aircraft.
  public List<string> interceptor_types;

  // Standard deviation for randomizing initial position and velocity.
  // Enables formation spread and realistic deployment variations.
  // If not specified, no randomization is applied.
  public StandardDeviation standard_deviation;

  // Current number of allocated interceptors. Used for capacity management.
  // This tracks how many interceptors are currently assigned to this origin.
  [JsonIgnore]
  private int _allocated_interceptors = 0;

  // Default constructor for JSON deserialization.
  public InterceptorOriginConfig() {
    interceptor_types = new List<string>();
    standard_deviation = new StandardDeviation();
  }

  // Checks if this origin supports launching the specified interceptor type.
  // Parameters:
  //   interceptorType: Model filename of the interceptor (e.g., "sm2.json")
  // Returns: True if this origin can launch the specified interceptor type
  public bool SupportsInterceptorType(string interceptorType) {
    return interceptor_types.Contains(interceptorType);
  }

  // Checks if this origin has available capacity to launch additional interceptors.
  // Returns: True if capacity is available, false if at maximum capacity
  public bool HasCapacity() {
    return _allocated_interceptors < max_interceptors;
  }

  // Gets the number of interceptors that can still be allocated to this origin.
  // Returns: Available capacity (max_interceptors - allocated_interceptors)
  public int GetAvailableCapacity() {
    return max_interceptors - _allocated_interceptors;
  }

  // Allocates an interceptor to this origin, reducing available capacity.
  // This should be called when an interceptor is launched from this origin.
  // Returns: True if allocation successful, false if no capacity available
  public bool AllocateInterceptor() {
    if (HasCapacity()) {
      ++_allocated_interceptors;
      return true;
    }
    return false;
  }

  // Releases an interceptor from this origin, increasing available capacity.
  // This should be called when an interceptor mission is complete or interceptor is destroyed.
  public void ReleaseInterceptor() {
    if (_allocated_interceptors > 0) {
      --_allocated_interceptors;
    }
  }

  // Resets the allocation counter. Useful for simulation resets or testing.
  public void ResetAllocations() {
    _allocated_interceptors = 0;
  }

  // Gets a string representation of this origin configuration for debugging.
  // Returns: Formatted string with key origin information
  public override string ToString() {
    return $"InterceptorOrigin[{id}] at {initial_position}, " +
           $"capacity: {_allocated_interceptors}/{max_interceptors}, " +
           $"types: [{string.Join(", ", interceptor_types)}]";
  }

  // Creates a randomized version of this origin configuration.
  // Applies standard deviation to position and velocity for formation spread.
  // Returns: New origin config with randomized position and velocity
  public InterceptorOriginConfig CreateRandomizedVersion() {
    var randomizedConfig =
        new InterceptorOriginConfig { id = id, max_interceptors = max_interceptors,
                                      interceptor_types = new List<string>(interceptor_types),
                                      standard_deviation = standard_deviation };

    // Apply randomization to position
    if (standard_deviation?.position != null) {
      Vector3 positionNoise = Utilities.GenerateRandomNoise(standard_deviation.position);
      randomizedConfig.initial_position = initial_position + positionNoise;
    } else {
      randomizedConfig.initial_position = initial_position;
    }

    // Apply randomization to velocity
    if (standard_deviation?.velocity != null) {
      Vector3 velocityNoise = Utilities.GenerateRandomNoise(standard_deviation.velocity);
      randomizedConfig.velocity = velocity + velocityNoise;
    } else {
      randomizedConfig.velocity = velocity;
    }

    return randomizedConfig;
  }

  // Validates this origin configuration for common errors.
  // Returns: List of validation error messages, empty if valid
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

// Enumeration of available origin assignment strategies.
// These strategies determine how interceptors are assigned to origins when multiple options are
// available.
[JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
public enum OriginAssignmentStrategy {
  // Assign interceptors to the closest available origin that supports the interceptor type.
  // Accounts for origin movement when calculating distances.
  CLOSEST,

  // Distribute interceptor assignments evenly across all capable origins.
  // Helps prevent overloading any single platform and maintains flexibility.
  LOAD_BALANCED,

  // Select origins based on their capability match with the interceptor type.
  // Prefers specialized platforms (e.g., long-range interceptors from shore batteries).
  CAPABILITY_BASED,

  // Use manually specified origin assignments from configuration.
  // Allows precise control over interceptor deployment.
  MANUAL
}
