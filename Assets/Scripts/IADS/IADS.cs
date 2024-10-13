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
      int popCount = 100;

      // Take up to popCount interceptors from the queue
      List<Interceptor> interceptorsToAssign = _assignmentQueue.Take(popCount).ToList();
      AssignInterceptorsToThreats(interceptorsToAssign);

      // Remove the processed interceptors from the queue
      _assignmentQueue.RemoveRange(0, Math.Min(popCount, _assignmentQueue.Count));

      // Check if any interceptors were not assigned
      List<Interceptor> assignedInterceptors =
          interceptorsToAssign.Where(m => m.HasAssignedTarget()).ToList();

      if (assignedInterceptors.Count < interceptorsToAssign.Count) {
        // Back into the queue they go!
        _assignmentQueue.AddRange(interceptorsToAssign.Except(assignedInterceptors));
        Debug.Log($"Backing interceptors into the queue. Failed to assign.");
      }
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
      // Debug.Log(
      //     $"Interceptor {assignment.Interceptor.name} assigned to threat
      //     {assignment.Threat.name}");
    }


  }

  public void RegisterNewThreat(Threat threat) {
    ThreatData threatData = new ThreatData(threat, threat.gameObject.name);
    _threatTable.Add(threatData);
    _threatDataMap.Add(threat, threatData);

    // Subscribe to the threat's events
    // TODO: If we do not want omniscient IADS, we
    // need to model the IADS's sensors here.
    threat.OnThreatHit += RegisterThreatHit;
    threat.OnThreatMiss += RegisterThreatMiss;
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

  private void MarkThreatDestroyed(ThreatData threatData) {
    if (threatData != null) {
      threatData.MarkDestroyed();
    }
  }

  private void RegisterThreatHit(Threat threat) {
    ThreatData threatData = _threatDataMap[threat];
    if (threatData != null) {
      MarkThreatDestroyed(threatData);
    }
  }


  private void RegisterThreatMiss(Threat threat) {
    // The threat missed (meaning it hit the floor, etc)
    ThreatData threatData = _threatDataMap[threat];
    if (threatData != null) {
      MarkThreatDestroyed(threatData);
    }
    //threatData.RemoveInterceptor(null);
  }

  private void RegisterSimulationEnded() {
    _threatTable.Clear();
    _threatDataMap.Clear();
    _assignmentQueue.Clear();
  }
}
