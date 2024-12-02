using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum InterceptorStatus { UNASSIGNED, ASSIGNED, DESTROYED }

[System.Serializable]
public class InterceptorData {
    public Interceptor Interceptor;
    [SerializeField]
    private InterceptorStatus _status;

    public string InterceptorID;
    [SerializeField]
    private List<Threat> _assignedThreats;

    public InterceptorStatus Status {
        get { return _status; }
    }

    public InterceptorData(Interceptor interceptor, string interceptorID) {
        Interceptor = interceptor;
        _status = InterceptorStatus.UNASSIGNED;
        InterceptorID = interceptorID;
        _assignedThreats = new List<Threat>();
    }

    public void AssignThreat(Threat threat) {
        if (_status == InterceptorStatus.DESTROYED) {
            Debug.LogError($"AssignThreat: Interceptor {InterceptorID} is destroyed, cannot assign threat");
            return;
        }
        _status = InterceptorStatus.ASSIGNED;
        _assignedThreats.Add(threat);
    }

    public void RemoveThreat(Threat threat) {
        _assignedThreats.Remove(threat);
        if (_assignedThreats.Count == 0) {
            _status = InterceptorStatus.UNASSIGNED;
        }
    }

    public void MarkDestroyed() {
        _status = InterceptorStatus.DESTROYED;
    }
}