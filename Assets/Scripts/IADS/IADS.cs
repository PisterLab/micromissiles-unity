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
  private List<TrackFileData> _trackFiles = new List<TrackFileData>();
  private Dictionary<Agent, TrackFileData> _trackFileMap = new Dictionary<Agent, TrackFileData>();

  private List<Interceptor> _assignmentQueue = new List<Interceptor>();

  private int _trackFileCount = 0;

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
        _assignmentScheme.Assign(missilesToAssign, _trackFiles.OfType<ThreatData>().ToList());

    // Apply the assignments to the missiles
    foreach (var assignment in assignments) {
      assignment.Interceptor.AssignTarget(assignment.Threat);
      ThreatData threatTrack = _trackFileMap[assignment.Threat] as ThreatData;  
      InterceptorData interceptorTrack = _trackFileMap[assignment.Interceptor] as InterceptorData;
      if (threatTrack != null && interceptorTrack != null) {
        threatTrack.AssignInterceptor(assignment.Interceptor);
        interceptorTrack.AssignThreat(assignment.Threat);
      }
    }
  }

  public void RegisterNewThreat(Threat threat) {
    string trackID = $"T{1000 + _trackFileCount++}";
    ThreatData trackFile = new ThreatData(threat, trackID);
    _trackFiles.Add(trackFile);
    _trackFileMap.Add(threat, trackFile);

    threat.OnThreatHit += RegisterThreatHit;
    threat.OnThreatMiss += RegisterThreatMiss;
  }

  public void RegisterNewInterceptor(Interceptor interceptor) {
    string trackID = $"I{2000 + _trackFileCount++}";
    InterceptorData trackFile = new InterceptorData(interceptor, trackID);
    _trackFiles.Add(trackFile);
    _trackFileMap.Add(interceptor, trackFile);

    interceptor.OnInterceptMiss += RegisterInterceptorMiss;
    interceptor.OnInterceptHit += RegisterInterceptorHit;
  }

  private void RegisterInterceptorHit(Interceptor interceptor, Threat threat) {
    var threatTrack = _trackFileMap[threat] as ThreatData;
    var interceptorTrack = _trackFileMap[interceptor] as InterceptorData;

    if (threatTrack != null) {
      threatTrack.RemoveInterceptor(interceptor);
      threatTrack.MarkDestroyed();
    }

    if (interceptorTrack != null) {
      interceptorTrack.RemoveThreat(threat);
      interceptorTrack.MarkDestroyed();
    }
  }

  private void RegisterInterceptorMiss(Interceptor interceptor, Threat threat) {
    var threatTrack = _trackFileMap[threat] as ThreatData;
    var interceptorTrack = _trackFileMap[interceptor] as InterceptorData;

    if (threatTrack != null) {
      threatTrack.RemoveInterceptor(interceptor);
    }

    if (interceptorTrack != null) {
      interceptorTrack.RemoveThreat(threat);
      interceptorTrack.MarkDestroyed();
    }
  }

  private void RegisterThreatHit(Threat threat) {
    var threatTrack = _trackFileMap[threat] as ThreatData;
    if (threatTrack != null) {
      threatTrack.MarkDestroyed();
    }
  }

  public List<ThreatData> GetThreatTracks() => 
      _trackFiles.OfType<ThreatData>().ToList();

  public List<InterceptorData> GetInterceptorTracks() => 
      _trackFiles.OfType<InterceptorData>().ToList();

  private void RegisterThreatMiss(Threat threat) {
    // The threat missed (meaning it hit the floor, etc)
    var threatTrack = _trackFileMap[threat] as ThreatData;
    if (threatTrack != null) {
      threatTrack.MarkDestroyed();
    }
  }

  private void RegisterSimulationEnded() {
    _trackFiles.Clear();
    _trackFileMap.Clear();
    _assignmentQueue.Clear();
    _trackFileCount = 0;
  }
}
