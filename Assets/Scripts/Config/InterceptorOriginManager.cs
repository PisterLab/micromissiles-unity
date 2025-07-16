using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Manages multiple interceptor origins and implements origin assignment strategies.
//
// This class provides centralized management of interceptor launch platforms,
// including:
// - Origin selection based on different strategies
// - Capacity management across multiple origins
// - Position tracking for moving origins
// - Load balancing and capability-based assignment
//
// The manager supports various scenarios:
// - Naval task forces with multiple ships
// - Layered defense networks with shore and ship-based systems
// - Mixed static and mobile launcher deployments
public class InterceptorOriginManager {
  private List<InterceptorOriginConfig> _origins;
  private Dictionary<string, InterceptorOriginObject> _originObjects;
  private Dictionary<OriginAssignmentStrategy,
                     Func<Vector3, string, float, string, InterceptorOriginConfig>> _strategies;
  private Dictionary<string, int> _assignmentCounts;  // For load balancing

  // Initializes a new InterceptorOriginManager with the specified origins.
  //   origins: List of interceptor origin configurations
  public InterceptorOriginManager(List<InterceptorOriginConfig> origins) {
    _origins = origins ?? new List<InterceptorOriginConfig>();
    _originObjects = new Dictionary<string, InterceptorOriginObject>();
    _assignmentCounts = new Dictionary<string, int>();

    // Initialize assignment counters for load balancing
    foreach (var origin in _origins) {
      _assignmentCounts[origin.id] = 0;
    }

    InitializeStrategies();
  }

  // Registers a runtime InterceptorOriginObject with its corresponding config.
  // This should be called when origin GameObjects are created.
  //   originObject: The runtime origin object
  public void RegisterOriginObject(InterceptorOriginObject originObject) {
    if (originObject != null && originObject.GetOriginConfig() != null) {
      _originObjects[originObject.OriginId] = originObject;
    }
  }

  // Gets the runtime InterceptorOriginObject for a given origin ID.
  //   originId: Origin ID to find
  // Returns: The runtime origin object, or null if not found
  public InterceptorOriginObject GetOriginObject(string originId) {
    return _originObjects.TryGetValue(originId, out var originObject) ? originObject : null;
  }

  // Gets all registered runtime origin objects.
  // Returns: Collection of runtime origin objects
  public IEnumerable<InterceptorOriginObject> GetAllOriginObjects() {
    return _originObjects.Values;
  }

  // Initializes the strategy function mappings.
  // Each strategy implements a different logic for selecting interceptor origins.
  private void InitializeStrategies() {
    _strategies = new Dictionary<OriginAssignmentStrategy,
                                 Func<Vector3, string, float, string, InterceptorOriginConfig>> {
      { OriginAssignmentStrategy.CLOSEST, SelectClosestOrigin },
      { OriginAssignmentStrategy.LOAD_BALANCED, SelectLoadBalancedOrigin },
      { OriginAssignmentStrategy.CAPABILITY_BASED, SelectCapabilityBasedOrigin },
      { OriginAssignmentStrategy.MANUAL, SelectManualOrigin }
    };
  }

  // Selects an appropriate interceptor origin based on the specified strategy.
  //   threatPosition: Position of the incoming threat
  //   interceptorType: Type of interceptor needed (e.g., "sm2.json")
  //   strategy: Assignment strategy to use
  //   currentTime: Current simulation time
  //   manualOriginId: Manual origin ID (for MANUAL strategy)
  // Returns: Selected origin, or null if no suitable origin available
  public InterceptorOriginConfig SelectOrigin(Vector3 threatPosition, string interceptorType,
                                              OriginAssignmentStrategy strategy, float currentTime,
                                              string manualOriginId = null) {
    if (_strategies.ContainsKey(strategy)) {
      var selectedOrigin =
          _strategies[strategy](threatPosition, interceptorType, currentTime, manualOriginId);

      // Update assignment counter for load balancing
      if (selectedOrigin != null) {
        _assignmentCounts[selectedOrigin.id]++;
      }

      return selectedOrigin;
    }

    Debug.LogWarning($"Unknown origin assignment strategy: {strategy}");
    return null;
  }

  // Selects an appropriate interceptor origin runtime object based on the specified strategy.
  // This is the preferred method that returns the actual runtime object.
  //   threatPosition: Position of the incoming threat
  //   interceptorType: Type of interceptor needed (e.g., "sm2.json")
  //   strategy: Assignment strategy to use
  //   manualOriginId: Manual origin ID (for MANUAL strategy)
  // Returns: Selected origin runtime object, or null if no suitable origin available
  public InterceptorOriginObject SelectOriginObject(Vector3 threatPosition, string interceptorType,
                                                    OriginAssignmentStrategy strategy,
                                                    string manualOriginId = null) {
    // First select the config using existing logic
    var selectedConfig =
        SelectOrigin(threatPosition, interceptorType, strategy, Time.time, manualOriginId);

    if (selectedConfig == null) {
      return null;
    }

    // Return the corresponding runtime object
    return GetOriginObject(selectedConfig.id);
  }

  // Selects the closest available origin that supports the interceptor type.
  // Accounts for origin movement when calculating distances.
  private InterceptorOriginConfig SelectClosestOrigin(Vector3 threatPosition,
                                                      string interceptorType, float currentTime,
                                                      string manualOriginId) {
    var availableOrigins = GetAvailableOrigins(interceptorType);
    if (availableOrigins.Count == 0) {
      return null;
    }

    InterceptorOriginConfig closestOrigin = null;
    float closestDistance = float.MaxValue;

    foreach (var origin in availableOrigins) {
      // Use runtime object position if available, otherwise fall back to calculated
      var originObject = GetOriginObject(origin.id);
      float distance;

      if (originObject != null) {
        // Use actual GameObject position
        distance = originObject.GetDistanceToTarget(threatPosition);
      } else {
        // Fallback to calculated position
        distance = origin.GetDistanceToTarget(threatPosition, currentTime);
      }

      if (distance < closestDistance) {
        closestDistance = distance;
        closestOrigin = origin;
      }
    }

    return closestOrigin;
  }

  // Selects an origin using load balancing to distribute assignments evenly.
  // Prefers origins with fewer current assignments.
  private InterceptorOriginConfig SelectLoadBalancedOrigin(Vector3 threatPosition,
                                                           string interceptorType,
                                                           float currentTime,
                                                           string manualOriginId) {
    var availableOrigins = GetAvailableOrigins(interceptorType);
    if (availableOrigins.Count == 0) {
      return null;
    }

    // Sort by assignment count (ascending) and then by distance as tiebreaker
    var sortedOrigins =
        availableOrigins.OrderBy(origin => _assignmentCounts[origin.id])
            .ThenBy(origin => origin.GetDistanceToTarget(threatPosition, currentTime))
            .ToList();

    return sortedOrigins.First();
  }

  // Selects an origin based on capability matching with the interceptor type.
  // Implements preference rules for different interceptor-origin combinations.
  private InterceptorOriginConfig SelectCapabilityBasedOrigin(Vector3 threatPosition,
                                                              string interceptorType,
                                                              float currentTime,
                                                              string manualOriginId) {
    var availableOrigins = GetAvailableOrigins(interceptorType);
    if (availableOrigins.Count == 0) {
      return null;
    }

    // Define capability preference rules
    var preferenceScores = new Dictionary<InterceptorOriginConfig, int>();

    foreach (var origin in availableOrigins) {
      int score = CalculateCapabilityScore(origin, interceptorType, threatPosition, currentTime);
      preferenceScores[origin] = score;
    }

    // Select origin with highest capability score
    return preferenceScores.OrderByDescending(kvp => kvp.Value).First().Key;
  }

  // Calculates a capability score for an origin-interceptor combination.
  // Higher scores indicate better capability matches.
  private int CalculateCapabilityScore(InterceptorOriginConfig origin, string interceptorType,
                                       Vector3 threatPosition, float currentTime) {
    int score = 0;

    // Base score for supporting the interceptor type
    score += 10;

    // Bonus for specialized interceptor types
    if (interceptorType.Contains("sm3") || interceptorType.Contains("thaad")) {
      // Long-range interceptors prefer shore installations
      if (origin.velocity.magnitude == 0 && origin.max_interceptors > 20) {
        score += 20;  // Shore battery bonus
      }
    } else if (interceptorType.Contains("f18") || interceptorType.Contains("hydra")) {
      // Aircraft-launched interceptors prefer carriers
      if (origin.id.Contains("CVN") || origin.id.Contains("Carrier")) {
        score += 15;  // Carrier bonus
      }
    } else if (interceptorType.Contains("sm2") || interceptorType.Contains("sm6")) {
      // Standard missiles prefer surface combatants
      if (origin.id.Contains("DDG") || origin.id.Contains("CG")) {
        score += 12;  // Surface combatant bonus
      }
    }

    // Distance penalty (closer is better)
    float distance = origin.GetDistanceToTarget(threatPosition, currentTime);
    score -= Mathf.RoundToInt(distance / 1000f);  // -1 point per km

    // Capacity bonus (more available capacity is better)
    score += origin.GetAvailableCapacity();

    return score;
  }

  // Selects the manually specified origin if it's available and capable.
  private InterceptorOriginConfig SelectManualOrigin(Vector3 threatPosition, string interceptorType,
                                                     float currentTime, string manualOriginId) {
    if (string.IsNullOrEmpty(manualOriginId)) {
      Debug.LogWarning("Manual origin assignment requested but no origin ID specified");
      return null;
    }

    var origin = _origins.Find(o => o.id == manualOriginId);
    if (origin == null) {
      Debug.LogWarning($"Manual origin '{manualOriginId}' not found");
      return null;
    }

    if (!origin.SupportsInterceptorType(interceptorType)) {
      Debug.LogWarning(
          $"Manual origin '{manualOriginId}' does not support interceptor type '{interceptorType}'");
      return null;
    }

    if (!origin.HasCapacity()) {
      Debug.LogWarning($"Manual origin '{manualOriginId}' has no available capacity");
      return null;
    }

    return origin;
  }

  // Updates positions of all moving origins based on elapsed time.
  // Should be called regularly during simulation to maintain accurate positions.
  //   deltaTime: Time elapsed since last update
  public void UpdateMovingOrigins(float deltaTime) {
    // Note: Origin positions are calculated dynamically in GetCurrentPosition()
    // This method is provided for interface compatibility and future extensions
    // such as course changes or formation adjustments

    foreach (var origin in _origins) {
      if (origin.velocity.magnitude > 0) {
        // Future: Could implement course changes, formation adjustments, etc.
        // For now, origins move in straight lines as defined by their velocity
      }
    }
  }

  // Gets all origins that support the specified interceptor type and have available capacity.
  //   interceptorType: Interceptor type to filter by
  // Returns: List of available origins
  public List<InterceptorOriginConfig> GetAvailableOrigins(string interceptorType) {
    return _origins
        .Where(origin => origin.SupportsInterceptorType(interceptorType) && origin.HasCapacity())
        .ToList();
  }

  // Gets an origin by its unique identifier.
  //   originId: Unique origin identifier
  // Returns: Origin configuration, or null if not found
  public InterceptorOriginConfig GetOriginById(string originId) {
    return _origins.Find(o => o.id == originId);
  }

  // Gets all configured origins.
  // Returns: List of all origins
  public List<InterceptorOriginConfig> GetAllOrigins() {
    return new List<InterceptorOriginConfig>(_origins);
  }

  // Validates all origin configurations and returns any errors found.
  // Returns: List of validation error messages
  public List<string> ValidateConfiguration() {
    var errors = new List<string>();

    // Check for duplicate origin IDs
    var originIds = _origins.Select(o => o.id).ToList();
    var duplicateIds = originIds.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key);

    foreach (var duplicateId in duplicateIds) {
      errors.Add($"Duplicate origin ID found: {duplicateId}");
    }

    // Validate each origin
    foreach (var origin in _origins) {
      var originErrors = origin.Validate();
      errors.AddRange(originErrors.Select(error => $"Origin '{origin.id}': {error}"));
    }

    return errors;
  }

  // Resets assignment counters for load balancing. Useful for simulation restarts.
  public void ResetAssignmentCounters() {
    foreach (var originId in _assignmentCounts.Keys.ToList()) {
      _assignmentCounts[originId] = 0;
    }

    // Also reset origin capacity allocations
    foreach (var origin in _origins) {
      origin.ResetAllocations();
    }
  }

  // Gets assignment statistics for monitoring and debugging.
  // Returns: Dictionary mapping origin IDs to assignment counts
  public Dictionary<string, int> GetAssignmentStatistics() {
    return new Dictionary<string, int>(_assignmentCounts);
  }

  // Gets the total available interceptor capacity across all origins.
  //   interceptorType: Optional filter by interceptor type
  // Returns: Total available capacity
  public int GetTotalAvailableCapacity(string interceptorType = null) {
    var relevantOrigins = string.IsNullOrEmpty(interceptorType)
                              ? _origins
                              : _origins.Where(o => o.SupportsInterceptorType(interceptorType));

    return relevantOrigins.Sum(o => o.GetAvailableCapacity());
  }
}
