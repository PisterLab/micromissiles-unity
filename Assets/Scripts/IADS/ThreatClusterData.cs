using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public enum ThreatClusterStatus { UNASSIGNED, ASSIGNED, DELEGATED }

[System.Serializable]
public class ThreatClusterData {
  private ClusterLegacy _cluster;

  // The agent tracks the centroid of the cluster.
  private Agent _agent;

  [SerializeField]
  private ThreatClusterStatus _status = ThreatClusterStatus.UNASSIGNED;

  [SerializeField]
  private List<Interceptor> _assignedInterceptors = new List<Interceptor>();

  public ThreatClusterData(ClusterLegacy cluster) {
    _cluster = cluster;
    _agent = SimManager.Instance.CreateDummyAgentLegacy(cluster.Centroid(), cluster.Velocity());
  }

  public ClusterLegacy Cluster {
    get { return _cluster; }
  }

  public Agent Centroid {
    get { return _agent; }
  }

  public ThreatClusterStatus Status {
    get { return _status; }
  }

  public int AssignedInterceptorCount {
    get { return _assignedInterceptors.Count; }
  }

  // Read-only view of interceptors currently assigned to this cluster.
  public IReadOnlyList<Interceptor> AssignedInterceptors {
    get { return _assignedInterceptors; }
  }

  public void UpdateCentroid() {
    _agent.SetPosition(_cluster.Centroid());
    _agent.SetVelocity(_cluster.Velocity());
  }

  public void AssignInterceptor(Interceptor interceptor) {
    _status = ThreatClusterStatus.ASSIGNED;
    _assignedInterceptors.Add(interceptor);

    // Assign the interceptor to all threats within the cluster.
    foreach (var threat in _cluster.Threats.ToList()) {
      threat.AddInterceptor(interceptor);
    }
  }

  public void RemoveInterceptor(Interceptor interceptor, bool delegated = false) {
    _assignedInterceptors.Remove(interceptor);
    if (AssignedInterceptorCount == 0) {
      _status = delegated ? ThreatClusterStatus.DELEGATED : ThreatClusterStatus.UNASSIGNED;
    }

    // Remove the interceptor from all threats within the cluster.
    foreach (var threat in _cluster.Threats.ToList()) {
      threat.RemoveInterceptor(interceptor);
    }
  }
}
