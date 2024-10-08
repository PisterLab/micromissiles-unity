using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// Integrated Air Defense System
public class IADS : MonoBehaviour {
  public enum ThreatAssignmentStyle { ONE_TIME, CONTINUOUS }

  public static IADS Instance { get; private set; }
  private IAssignment _assignmentScheme;

  [SerializeField]
  private List<ThreatData> _threatTable = new List<ThreatData>();
  private Dictionary<Threat, ThreatData> _threatDataMap = new Dictionary<Threat, ThreatData>();

  private List<Interceptor> _assignmentQueue = new List<Interceptor>();

  private void Awake() {
    if (Instance == null) {
      Instance = this;
    } else {
      Destroy(gameObject);
    }
  }

  private void Start() {
    SimManager.Instance.OnSimulationEnded += RegisterSimulationEnded;
    SimManager.Instance.OnNewThreat += RegisterNewThreat;
    SimManager.Instance.OnNewInterceptor += RegisterNewInterceptor;
    _assignmentScheme = new ThreatAssignment();
  }

  public void LateUpdate() {
    if (_assignmentQueue.Count > 0) {
      AssignInterceptorsToThreats(_assignmentQueue);
      _assignmentQueue.Clear();
    }
  }

  public void RequestThreatAssignment(List<Interceptor> interceptors) {
    _assignmentQueue.AddRange(interceptors);
  }

  public void RequestThreatAssignment(Interceptor interceptor) {
    _assignmentQueue.Add(interceptor);
  }

  /// <summary>
  /// Assigns the specified list of missiles to available targets based on the assignment scheme.
  /// </summary>
  /// <param name="missilesToAssign">The list of missiles to assign.</param>
  public void AssignInterceptorsToThreats(List<Interceptor> missilesToAssign) {
    // Perform the assignment
    IEnumerable<IAssignment.AssignmentItem> assignments =
        _assignmentScheme.Assign(missilesToAssign, _threatTable);

    // Apply the assignments to the missiles
    foreach (var assignment in assignments) {
      assignment.Interceptor.AssignTarget(assignment.Threat);
      _threatDataMap[assignment.Threat].AssignInterceptor(assignment.Interceptor);
      Debug.Log(
          $"Interceptor {assignment.Interceptor.name} assigned to threat {assignment.Threat.name}");
    }

    // Check if any interceptors were not assigned
    List<Interceptor> unassignedInterceptors =
        missilesToAssign.Where(m => !m.HasAssignedTarget()).ToList();

    if (unassignedInterceptors.Count > 0) {
      string unassignedIds = string.Join(", ", unassignedInterceptors.Select(m => m.name));
      int totalInterceptors = missilesToAssign.Count;
      int assignedInterceptors = totalInterceptors - unassignedInterceptors.Count;

      Debug.LogWarning(
          $"Warning: {unassignedInterceptors.Count} out of {totalInterceptors} interceptors were not assigned to any threat. " +
          $"Unassigned interceptor IDs: {unassignedIds}. " +
          $"Total interceptors: {totalInterceptors}, Assigned: {assignedInterceptors}, Unassigned: {unassignedInterceptors.Count}");

      // Log information about the assignment scheme
      Debug.Log($"Current Assignment Scheme: {_assignmentScheme.GetType().Name}");
    }
  }

  public void RegisterNewThreat(Threat threat) {
    ThreatData threatData = new ThreatData(threat, threat.gameObject.name);
    _threatTable.Add(threatData);
    _threatDataMap.Add(threat, threatData);

    // Subscribe to the threat's events
    // TODO: If we do not want omniscient IADS, we
    // need to model the IADS's sensors here.
    threat.OnInterceptHit += RegisterThreatHit;
    threat.OnInterceptMiss += RegisterThreatMiss;
  }

  public void RegisterNewInterceptor(Interceptor interceptor) {
    // Placeholder
    interceptor.OnInterceptMiss += RegisterInterceptorMiss;
    interceptor.OnInterceptHit += RegisterInterceptorHit;
  }

  private void RegisterInterceptorHit(Interceptor interceptor, Threat threat) {
    ThreatData threatData = _threatDataMap[threat];
    if (threatData != null) {
      threatData.RemoveInterceptor(interceptor);
      MarkThreatDestroyed(threatData);
    }
  }

  private void RegisterInterceptorMiss(Interceptor interceptor, Threat threat) {
    // Remove the interceptor from the threat's assigned interceptors
    _threatDataMap[threat].RemoveInterceptor(interceptor);
  }
  private void RegisterThreatHit(Interceptor interceptor, Threat threat) {
    ThreatData threatData = _threatDataMap[threat];
    if (threatData != null) {
      threatData.RemoveInterceptor(interceptor);
      MarkThreatDestroyed(threatData);
    }
  }

  private void MarkThreatDestroyed(ThreatData threatData) {
    if (threatData != null) {
      threatData.MarkDestroyed();
    }
  }

  private void RegisterThreatMiss(Interceptor interceptor, Threat threat) {
    ThreatData threatData = _threatDataMap[threat];
    threatData.RemoveInterceptor(interceptor);
  }

  private void RegisterSimulationEnded() {
    _threatTable.Clear();
    _threatDataMap.Clear();
    _assignmentQueue.Clear();
  }
}
