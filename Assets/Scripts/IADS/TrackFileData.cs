using UnityEngine;
using System.Collections.Generic;

public enum TrackStatus { UNASSIGNED, ASSIGNED, DESTROYED }

[System.Serializable]
public abstract class TrackFileData {
  public Agent Agent { get; protected set; }
  [SerializeField]
  protected TrackStatus _status;
  public string TrackID { get; protected set; }

  public TrackStatus Status => _status;

  protected TrackFileData(Agent agent, string trackID) {
    Agent = agent;
    _status = TrackStatus.UNASSIGNED;
    TrackID = trackID;
  }

  public virtual void MarkDestroyed() {
    _status = TrackStatus.DESTROYED;
  }
}

[System.Serializable]
public class ThreatData : TrackFileData {
  public int assignedInterceptorCount = 0;
  [SerializeField]
  private List<Interceptor> _assignedInterceptors = new List<Interceptor>();

  public ThreatData(Threat threat, string trackID) : base(threat, trackID) {}

  public void AssignInterceptor(Interceptor interceptor) {
    if (Status == TrackStatus.DESTROYED) {
      Debug.LogError($"AssignInterceptor: Track {TrackID} is destroyed, cannot assign interceptor");
      return;
    }
    _status = TrackStatus.ASSIGNED;
    _assignedInterceptors.Add(interceptor);
    assignedInterceptorCount++;
  }

  public void RemoveInterceptor(Interceptor interceptor) {
    _assignedInterceptors.Remove(interceptor);
    if (_assignedInterceptors.Count == 0) {
      _status = TrackStatus.UNASSIGNED;
    }
    assignedInterceptorCount--;
  }
}

[System.Serializable]
public class InterceptorData : TrackFileData {
  [SerializeField]
  private List<Threat> _assignedThreats;

  public InterceptorData(Interceptor interceptor, string interceptorID)
      : base(interceptor, interceptorID) {
    _assignedThreats = new List<Threat>();
  }

  public void AssignThreat(Threat threat) {
    if (_status == TrackStatus.DESTROYED) {
      Debug.LogError($"AssignThreat: Interceptor {TrackID} is destroyed, cannot assign threat");
      return;
    }
    _status = TrackStatus.ASSIGNED;
    _assignedThreats.Add(threat);
  }

  public void RemoveThreat(Threat threat) {
    _assignedThreats.Remove(threat);
    if (_assignedThreats.Count == 0) {
      _status = TrackStatus.UNASSIGNED;
    }
  }
}
