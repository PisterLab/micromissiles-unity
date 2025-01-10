using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum ThreatStatus { UNASSIGNED, ASSIGNED, DESTROYED }

[System.Serializable]
public class ThreatData {
  private Threat _threat;

  private string _threatID;

  [SerializeField]
  private ThreatStatus _status = ThreatStatus.UNASSIGNED;

  [SerializeField]
  private List<Interceptor> _assignedInterceptors = new List<Interceptor>();

  public ThreatData(Threat threat, string threatID) {
    _threat = threat;
    _threatID = threatID;
  }

  public Threat Threat {
    get { return _threat; }
  }

  public string ThreatID {
    get { return _threatID; }
  }

  public ThreatStatus Status {
    get { return _status; }
  }

  public int AssignedInterceptorCount {
    get { return _assignedInterceptors.Count; }
  }

  public void AssignInterceptor(Interceptor interceptor) {
    if (Status == ThreatStatus.DESTROYED) {
      Debug.LogError(
          $"AssignInterceptor: Threat {ThreatID} is destroyed and be assigned an interceptor.");
      return;
    }
    _status = ThreatStatus.ASSIGNED;
    _assignedInterceptors.Add(interceptor);
  }

  public void RemoveInterceptor(Interceptor interceptor) {
    _assignedInterceptors.Remove(interceptor);
    if (AssignedInterceptorCount == 0) {
      _status = ThreatStatus.UNASSIGNED;
    }
  }

  public void MarkDestroyed() {
    _status = ThreatStatus.DESTROYED;
  }
}
