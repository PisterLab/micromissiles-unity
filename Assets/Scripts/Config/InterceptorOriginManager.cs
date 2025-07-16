using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages multiple interceptor origins and implements origin assignment strategies.
/// 
/// This class provides centralized management of interceptor launch platforms,
/// including:
/// - Origin selection based on different strategies
/// - Capacity management across multiple origins
/// - Position tracking for moving origins
/// - Load balancing and capability-based assignment
/// 
/// The manager supports various scenarios:
/// - Naval task forces with multiple ships
/// - Layered defense networks with shore and ship-based systems
/// - Mixed static and mobile launcher deployments
/// </summary>
public class InterceptorOriginManager {
    private List<InterceptorOriginConfig> _origins;
    private Dictionary<OriginAssignmentStrategy, Func<Vector3, string, float, string, InterceptorOriginConfig>> _strategies;
    private Dictionary<string, int> _assignmentCounts; // For load balancing

    /// <summary>
    /// Initializes a new InterceptorOriginManager with the specified origins.
    /// </summary>
    /// <param name="origins">List of interceptor origin configurations</param>
    public InterceptorOriginManager(List<InterceptorOriginConfig> origins) {
        _origins = origins ?? new List<InterceptorOriginConfig>();
        _assignmentCounts = new Dictionary<string, int>();
        
        // Initialize assignment counters for load balancing
        foreach (var origin in _origins) {
            _assignmentCounts[origin.id] = 0;
        }
        
        InitializeStrategies();
    }

    /// <summary>
    /// Initializes the strategy function mappings.
    /// Each strategy implements a different logic for selecting interceptor origins.
    /// </summary>
    private void InitializeStrategies() {
        _strategies = new Dictionary<OriginAssignmentStrategy, Func<Vector3, string, float, string, InterceptorOriginConfig>> {
            { OriginAssignmentStrategy.CLOSEST, SelectClosestOrigin },
            { OriginAssignmentStrategy.LOAD_BALANCED, SelectLoadBalancedOrigin },
            { OriginAssignmentStrategy.CAPABILITY_BASED, SelectCapabilityBasedOrigin },
            { OriginAssignmentStrategy.MANUAL, SelectManualOrigin }
        };
    }

    /// <summary>
    /// Selects an appropriate interceptor origin based on the specified strategy.
    /// </summary>
    /// <param name="threatPosition">Position of the incoming threat</param>
    /// <param name="interceptorType">Type of interceptor needed (e.g., "sm2.json")</param>
    /// <param name="strategy">Assignment strategy to use</param>
    /// <param name="currentTime">Current simulation time</param>
    /// <param name="manualOriginId">Manual origin ID (for MANUAL strategy)</param>
    /// <returns>Selected origin, or null if no suitable origin available</returns>
    public InterceptorOriginConfig SelectOrigin(Vector3 threatPosition, string interceptorType, 
        OriginAssignmentStrategy strategy, float currentTime, string manualOriginId = null) {
        
        if (_strategies.ContainsKey(strategy)) {
            var selectedOrigin = _strategies[strategy](threatPosition, interceptorType, currentTime, manualOriginId);
            
            // Update assignment counter for load balancing
            if (selectedOrigin != null) {
                _assignmentCounts[selectedOrigin.id]++;
            }
            
            return selectedOrigin;
        }
        
        Debug.LogWarning($"Unknown origin assignment strategy: {strategy}");
        return null;
    }

    /// <summary>
    /// Selects the closest available origin that supports the interceptor type.
    /// Accounts for origin movement when calculating distances.
    /// </summary>
    private InterceptorOriginConfig SelectClosestOrigin(Vector3 threatPosition, string interceptorType, 
        float currentTime, string manualOriginId) {
        
        var availableOrigins = GetAvailableOrigins(interceptorType);
        if (availableOrigins.Count == 0) {
            return null;
        }

        InterceptorOriginConfig closestOrigin = null;
        float closestDistance = float.MaxValue;

        foreach (var origin in availableOrigins) {
            float distance = origin.GetDistanceToTarget(threatPosition, currentTime);
            if (distance < closestDistance) {
                closestDistance = distance;
                closestOrigin = origin;
            }
        }

        return closestOrigin;
    }

    /// <summary>
    /// Selects an origin using load balancing to distribute assignments evenly.
    /// Prefers origins with fewer current assignments.
    /// </summary>
    private InterceptorOriginConfig SelectLoadBalancedOrigin(Vector3 threatPosition, string interceptorType, 
        float currentTime, string manualOriginId) {
        
        var availableOrigins = GetAvailableOrigins(interceptorType);
        if (availableOrigins.Count == 0) {
            return null;
        }

        // Sort by assignment count (ascending) and then by distance as tiebreaker
        var sortedOrigins = availableOrigins.OrderBy(origin => _assignmentCounts[origin.id])
                                          .ThenBy(origin => origin.GetDistanceToTarget(threatPosition, currentTime))
                                          .ToList();

        return sortedOrigins.First();
    }

    /// <summary>
    /// Selects an origin based on capability matching with the interceptor type.
    /// Implements preference rules for different interceptor-origin combinations.
    /// </summary>
    private InterceptorOriginConfig SelectCapabilityBasedOrigin(Vector3 threatPosition, string interceptorType, 
        float currentTime, string manualOriginId) {
        
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

    /// <summary>
    /// Calculates a capability score for an origin-interceptor combination.
    /// Higher scores indicate better capability matches.
    /// </summary>
    private int CalculateCapabilityScore(InterceptorOriginConfig origin, string interceptorType, 
        Vector3 threatPosition, float currentTime) {
        
        int score = 0;

        // Base score for supporting the interceptor type
        score += 10;

        // Bonus for specialized interceptor types
        if (interceptorType.Contains("sm3") || interceptorType.Contains("thaad")) {
            // Long-range interceptors prefer shore installations
            if (origin.velocity.magnitude == 0 && origin.max_interceptors > 20) {
                score += 20; // Shore battery bonus
            }
        } else if (interceptorType.Contains("f18") || interceptorType.Contains("hydra")) {
            // Aircraft-launched interceptors prefer carriers
            if (origin.id.Contains("CVN") || origin.id.Contains("Carrier")) {
                score += 15; // Carrier bonus
            }
        } else if (interceptorType.Contains("sm2") || interceptorType.Contains("sm6")) {
            // Standard missiles prefer surface combatants
            if (origin.id.Contains("DDG") || origin.id.Contains("CG")) {
                score += 12; // Surface combatant bonus
            }
        }

        // Distance penalty (closer is better)
        float distance = origin.GetDistanceToTarget(threatPosition, currentTime);
        score -= Mathf.RoundToInt(distance / 1000f); // -1 point per km

        // Capacity bonus (more available capacity is better)
        score += origin.GetAvailableCapacity();

        return score;
    }

    /// <summary>
    /// Selects the manually specified origin if it's available and capable.
    /// </summary>
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
            Debug.LogWarning($"Manual origin '{manualOriginId}' does not support interceptor type '{interceptorType}'");
            return null;
        }

        if (!origin.HasCapacity()) {
            Debug.LogWarning($"Manual origin '{manualOriginId}' has no available capacity");
            return null;
        }

        return origin;
    }

    /// <summary>
    /// Updates positions of all moving origins based on elapsed time.
    /// Should be called regularly during simulation to maintain accurate positions.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update</param>
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

    /// <summary>
    /// Gets all origins that support the specified interceptor type and have available capacity.
    /// </summary>
    /// <param name="interceptorType">Interceptor type to filter by</param>
    /// <returns>List of available origins</returns>
    public List<InterceptorOriginConfig> GetAvailableOrigins(string interceptorType) {
        return _origins.Where(origin => 
            origin.SupportsInterceptorType(interceptorType) && 
            origin.HasCapacity()
        ).ToList();
    }

    /// <summary>
    /// Gets an origin by its unique identifier.
    /// </summary>
    /// <param name="originId">Unique origin identifier</param>
    /// <returns>Origin configuration, or null if not found</returns>
    public InterceptorOriginConfig GetOriginById(string originId) {
        return _origins.Find(o => o.id == originId);
    }

    /// <summary>
    /// Gets all configured origins.
    /// </summary>
    /// <returns>List of all origins</returns>
    public List<InterceptorOriginConfig> GetAllOrigins() {
        return new List<InterceptorOriginConfig>(_origins);
    }

    /// <summary>
    /// Validates all origin configurations and returns any errors found.
    /// </summary>
    /// <returns>List of validation error messages</returns>
    public List<string> ValidateConfiguration() {
        var errors = new List<string>();

        // Check for duplicate origin IDs
        var originIds = _origins.Select(o => o.id).ToList();
        var duplicateIds = originIds.GroupBy(id => id)
                                   .Where(g => g.Count() > 1)
                                   .Select(g => g.Key);
        
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

    /// <summary>
    /// Resets assignment counters for load balancing. Useful for simulation restarts.
    /// </summary>
    public void ResetAssignmentCounters() {
        foreach (var originId in _assignmentCounts.Keys.ToList()) {
            _assignmentCounts[originId] = 0;
        }
        
        // Also reset origin capacity allocations
        foreach (var origin in _origins) {
            origin.ResetAllocations();
        }
    }

    /// <summary>
    /// Gets assignment statistics for monitoring and debugging.
    /// </summary>
    /// <returns>Dictionary mapping origin IDs to assignment counts</returns>
    public Dictionary<string, int> GetAssignmentStatistics() {
        return new Dictionary<string, int>(_assignmentCounts);
    }

    /// <summary>
    /// Gets the total available interceptor capacity across all origins.
    /// </summary>
    /// <param name="interceptorType">Optional filter by interceptor type</param>
    /// <returns>Total available capacity</returns>
    public int GetTotalAvailableCapacity(string interceptorType = null) {
        var relevantOrigins = string.IsNullOrEmpty(interceptorType) 
            ? _origins 
            : _origins.Where(o => o.SupportsInterceptorType(interceptorType));

        return relevantOrigins.Sum(o => o.GetAvailableCapacity());
    }
}