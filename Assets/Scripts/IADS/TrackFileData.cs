// A radar track file or an IADS (Integrated Air Defense System) track file is a digital record
// that continuously updates and stores information about a detected object (aircraft, missile,
// drone, etc.) as it moves through a monitored area.
///
// These files are maintained by radar systems and air defense networks to track and classify
// targets in real-time.
///
// In an Integrated Air Defense System (IADS), track files are shared and fused from multiple
// radar and sensor sources. These files enable coordinated tracking and engagement of threats.

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
  [SerializeField]
  private List<Interceptor> _assignedInterceptors = new List<Interceptor>();

  public ThreatData(Threat threat, string trackID) : base(threat, trackID) {}

  public int AssignedInterceptorCount {
    get { return _assignedInterceptors.Count; }
  }

  public void AssignInterceptor(Interceptor interceptor) {
    if (Status == TrackStatus.DESTROYED) {
      Debug.LogError(
          $"AssignInterceptor: Track {TrackID} is destroyed, cannot assign interceptor.");
      return;
    }
    _status = TrackStatus.ASSIGNED;
    _assignedInterceptors.Add(interceptor);
  }
  public void RemoveInterceptor(Interceptor interceptor) {
    if (_assignedInterceptors.Contains(interceptor)) {
      _assignedInterceptors.Remove(interceptor);
      if (_assignedInterceptors.Count == 0) {
        _status = TrackStatus.UNASSIGNED;
      }
    }
  }

  public override void MarkDestroyed() {
    base.MarkDestroyed();
    _assignedInterceptors.Clear();
  }
}

[System.Serializable]
public class InterceptorData : TrackFileData {
  [SerializeField]
  private List<Threat> _assignedThreats = new List<Threat>();

  public InterceptorData(Interceptor interceptor, string interceptorID)
      : base(interceptor, interceptorID) {}

  public void AssignThreat(Threat threat) {
    if (_status == TrackStatus.DESTROYED) {
      Debug.LogError($"AssignThreat: Interceptor {TrackID} is destroyed, cannot assign threat.");
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

  public override void MarkDestroyed() {
    base.MarkDestroyed();
    _assignedThreats.Clear();
  }
}
