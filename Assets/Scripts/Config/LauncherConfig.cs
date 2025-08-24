using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

// Configuration for a launcher (launch platform).
// Supports both static launchers (shore batteries) and moving launchers (naval assets).
//
// A launcher represents a platform capable of launching interceptors,
// such as:
// - Naval vessels (e.g. aircraft carriers, destroyers, cruisers)
// - Shore-based installations (e.g. Aegis Ashore, Patriot batteries)
// - Mobile platforms (e.g. transporter erector launcher (TEL) vehicles)
//
// Each launcher has:
// - A unique identifier for reference
// - Initial position and optional velocity for movement
// - Launch capacity constraints
// - Supported interceptor types
// - Optional randomization for position and velocity
[Serializable]
public class LauncherConfig {
  // Unique identifier for this launcher (e.g., "CVN-68-Nimitz", "Aegis-Ashore-1").
  // Used for manual assignment and tracking.
  public string id;

  // Type of launcher platform, determines which prefab to instantiate.
  // Examples: "Ship", "ShoreBattery", "MobileLauncher"
  // If not specified, defaults to velocity-based detection for backward compatibility.
  public string type;

  // Initial position of the launcher at simulation start (in world coordinates).
  // For moving launchers, this represents the starting position.
  public Vector3 initial_position;

  // Velocity vector for moving launchers (in meters per second).
  // Use Vector3.zero for static installations.
  // For naval formations, all ships typically have the same velocity.
  public Vector3 velocity;

  // Maximum number of interceptors this launcher can have active simultaneously.
  // Represents launch capacity constraints (e.g., VLS cells, reload limitations).
  public int max_interceptors;

  // List of interceptor model files that this launcher can launch.
  // Different platforms support different interceptor types.
  // Examples: ["sm2.json", "sm6.json"] for destroyers, ["hydra70.json"] for aircraft.
  public List<string> interceptor_types;

  // Standard deviation for randomizing initial position and velocity.
  // Enables formation spread and realistic deployment variations.
  // If not specified, no randomization is applied.
  public StandardDeviation standard_deviation;

  // Current number of allocated interceptors. Used for capacity management.
  // This tracks how many interceptors are currently assigned to this launcher.
  [JsonIgnore]
  private int _allocatedInterceptors = 0;

  // Default constructor for JSON deserialization.
  public LauncherConfig() {
    interceptor_types = new List<string>();
    standard_deviation = new StandardDeviation();
  }

  // Checks if this launcher supports launching the specified interceptor type.
  /// <param name="interceptorType">Model filename of the interceptor (e.g., "sm2.json")</param>
  /// <returns>True if this launcher can launch the specified interceptor type</returns>
  public bool SupportsInterceptorType(string interceptorType) {
    return interceptor_types.Contains(interceptorType);
  }

  // Checks if this launcher has available capacity to launch additional interceptors.
  /// <returns>True if capacity is available, false if at maximum capacity</returns>
  public bool HasCapacity() {
    return _allocatedInterceptors < max_interceptors;
  }

  // Gets the number of interceptors that can still be allocated to this launcher.
  /// <returns>Available capacity (max_interceptors - allocated_interceptors)</returns>
  public int GetAvailableCapacity() {
    return max_interceptors - _allocatedInterceptors;
  }

  // Allocates an interceptor to this launcher, reducing available capacity.
  // This should be called when an interceptor is launched from this launcher.
  /// <returns>True if allocation successful, false if no capacity available</returns>
  public bool AllocateInterceptor() {
    if (HasCapacity()) {
      ++_allocatedInterceptors;
      return true;
    }
    return false;
  }

  // Releases an interceptor from this launcher, increasing available capacity.
  // This should be called when an interceptor mission is complete or interceptor is destroyed.
  public void ReleaseInterceptor() {
    if (_allocatedInterceptors > 0) {
      --_allocatedInterceptors;
    }
  }

  // Resets the allocation counter. Useful for simulation resets or testing.
  public void ResetAllocations() {
    _allocatedInterceptors = 0;
  }

  // Gets a string representation of this launcher configuration for debugging.
  /// <returns>Formatted string with key launcher information</returns>
  public override string ToString() {
    return $"Launcher[{id}] at {initial_position}, " +
           $"capacity: {_allocatedInterceptors}/{max_interceptors}, " +
           $"types: [{string.Join(", ", interceptor_types)}]";
  }

  // Creates a randomized version of this launcher configuration.
  // Applies standard deviation to position and velocity for formation spread.
  /// <returns>New launcher config with randomized position and velocity</returns>
  public LauncherConfig CreateRandomizedVersion() {
    var randomizedConfig =
        new LauncherConfig { id = $"{id}-{Guid.NewGuid().ToString()[..8]}", max_interceptors = max_interceptors,
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

  // Validates this launcher configuration for common errors.
  /// <returns>List of validation error messages, empty if valid</returns>
  public List<string> Validate() {
    var errors = new List<string>();

    if (string.IsNullOrEmpty(id)) {
      errors.Add("Launcher ID cannot be null or empty");
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

// Enumeration of available launcher assignment strategies.
// These strategies determine how interceptors are assigned to launchers when multiple options are
// available.
[JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
public enum LauncherAssignmentStrategy {
  // Assign interceptors to the closest available launcher that supports the interceptor type.
  // Accounts for launcher movement when calculating distances.
  CLOSEST,

  // Distribute interceptor assignments evenly across all capable launchers.
  // Helps prevent overloading any single platform and maintains flexibility.
  LOAD_BALANCED,

  // Select launchers based on their capability match with the interceptor type.
  // Prefers specialized platforms (e.g., long-range interceptors from shore batteries).
  CAPABILITY_BASED,

  // Use manually specified launcher assignments from configuration.
  // Allows precise control over interceptor deployment.
  MANUAL
}