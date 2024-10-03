using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum ThreatStatus { UNASSIGNED, ASSIGNED, DESTROYED }
[System.Serializable]
public class ThreatData {
  public Threat Threat;
  [SerializeField]
  private ThreatStatus _status;
  public ThreatStatus Status {
    get { return _status; }
  }
  public string ThreatID;
  [SerializeField]
  private List<Interceptor> _assignedInterceptors;  // Changed from property to field

  public void AssignInterceptor(Interceptor interceptor) {
    if (Status == ThreatStatus.DESTROYED) {
      Debug.LogError(
          $"AssignInterceptor: Threat {ThreatID} is destroyed, cannot assign interceptor");
      return;
    }
    _status = ThreatStatus.ASSIGNED;
    _assignedInterceptors.Add(interceptor);
  }

  public void RemoveInterceptor(Interceptor interceptor) {
    _assignedInterceptors.Remove(interceptor);
    if (_assignedInterceptors.Count == 0) {
      _status = ThreatStatus.UNASSIGNED;
    }
  }

  public void MarkDestroyed() {
    _status = ThreatStatus.DESTROYED;
  }
  // Constructor remains the same
  public ThreatData(Threat threat, string threatID) {
    Threat = threat;
    _status = ThreatStatus.UNASSIGNED;
    ThreatID = threatID;
    _assignedInterceptors = new List<Interceptor>();  // Initialize the list
  }
}