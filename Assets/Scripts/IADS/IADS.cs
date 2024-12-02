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

  private int _trackFileCount = 0;

  [SerializeField]
  private List<InterceptorData> _interceptorTable = new List<InterceptorData>();
  private Dictionary<Interceptor, InterceptorData> _interceptorDataMap = new Dictionary<Interceptor, InterceptorData>();

  private int _interceptorFileCount = 0;

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
      _interceptorDataMap[assignment.Interceptor].AssignThreat(assignment.Threat);
    }
  }

  public void RegisterNewThreat(Threat threat) {
    ThreatData threatData = new ThreatData(threat, $"T{1000 + _trackFileCount++}");
    _threatTable.Add(threatData);
    _threatDataMap.Add(threat, threatData);

    // Subscribe to the threat's events
    // TODO: If we do not want omniscient IADS, we
    // need to model the IADS's sensors here.
    threat.OnThreatHit += RegisterThreatHit;
    threat.OnThreatMiss += RegisterThreatMiss;
  }

  public void RegisterNewInterceptor(Interceptor interceptor) {
    InterceptorData interceptorData = new InterceptorData(interceptor, $"I{2000 + _interceptorFileCount++}");
    _interceptorTable.Add(interceptorData);
    _interceptorDataMap.Add(interceptor, interceptorData);

    // Subscribe to the interceptor's events
    interceptor.OnInterceptMiss += RegisterInterceptorMiss;
    interceptor.OnInterceptHit += RegisterInterceptorHit;
  }

  private void RegisterInterceptorHit(Interceptor interceptor, Threat threat) {
    ThreatData threatData = _threatDataMap[threat];
    if (threatData != null) {
      threatData.RemoveInterceptor(interceptor);
      MarkThreatDestroyed(threatData);
    }

    if (_interceptorDataMap.TryGetValue(interceptor, out InterceptorData interceptorData)) {
      interceptorData.RemoveThreat(threat);
      interceptorData.MarkDestroyed();
    }
  }

  private void RegisterInterceptorMiss(Interceptor interceptor, Threat threat) {
    _threatDataMap[threat].RemoveInterceptor(interceptor);

    if (_interceptorDataMap.TryGetValue(interceptor, out InterceptorData interceptorData)) {
      interceptorData.RemoveThreat(threat);
    }
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

  public List<ThreatData> GetThreatTable() {
    return _threatTable;
  }

  public List<InterceptorData> GetInterceptorTable() {
    return _interceptorTable;
  }

  private void RegisterThreatMiss(Threat threat) {
    // The threat missed (meaning it hit the floor, etc)
    ThreatData threatData = _threatDataMap[threat];
    if (threatData != null) {
      MarkThreatDestroyed(threatData);
    }
    // threatData.RemoveInterceptor(null);
  }

  private void RegisterSimulationEnded() {
    _threatTable.Clear();
    _threatDataMap.Clear();
    _interceptorTable.Clear();
    _interceptorDataMap.Clear();
    _assignmentQueue.Clear();
    _trackFileCount = 0;
    _interceptorFileCount = 0;
  }
}
