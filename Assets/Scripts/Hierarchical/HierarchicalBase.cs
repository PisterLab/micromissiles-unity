using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Base implementation of a hierarchical object.
//
// The position and velocity of a hierarchical object is defined as the mean of the positions and
// velocities of the sub-hierarchical objects.
public class HierarchicalBase : IHierarchical {
  // Soft maximum number of sub-hierarchical objects. This is used for recursive clustering.
  private const int _maxNumSubHierarchicals = 10;

  // Maximum cluster radius in meters.
  private const float _clusterMaxRadius = 1000f;

  // List of hierarchical objects in the hierarchy level below.
  protected List<IHierarchical> _subHierarchicals = new List<IHierarchical>();

  // List of hierarchical objects pursuing this hierarchical object.
  private List<IHierarchical> _pursuers = new List<IHierarchical>();

  // List of launched hierarchical objects.
  private List<IHierarchical> _launchedHierarchicals = new List<IHierarchical>();

  public IReadOnlyList<IHierarchical> SubHierarchicals => _subHierarchicals.AsReadOnly();

  // Return a list of active sub-hierarchical objects.
  public IEnumerable<IHierarchical> ActiveSubHierarchicals =>
      _subHierarchicals.Where(s => !s.IsTerminated);

  public virtual IHierarchical Target { get; set; }

  public IReadOnlyList<IHierarchical> Pursuers => _pursuers.AsReadOnly();

  public IReadOnlyList<IHierarchical> LaunchedHierarchicals => _launchedHierarchicals.AsReadOnly();

  public virtual Vector3 Position => GetMean(s => s.Position);
  public virtual Vector3 Velocity => GetMean(s => s.Velocity);
  public float Speed => Velocity.magnitude;
  public virtual Vector3 Acceleration => GetMean(s => s.Acceleration);
  public virtual bool IsTerminated => !ActiveSubHierarchicals.Any();

  public void AddSubHierarchical(IHierarchical subHierarchical) {
    if (!_subHierarchicals.Contains(subHierarchical)) {
      _subHierarchicals.Add(subHierarchical);
    }
  }

  public void RemoveSubHierarchical(IHierarchical subHierarchical) {
    _subHierarchicals.Remove(subHierarchical);
  }

  public void ClearSubHierarchicals() {
    _subHierarchicals.Clear();
  }

  public void AddPursuer(IHierarchical pursuer) {
    if (!_pursuers.Contains(pursuer)) {
      _pursuers.Add(pursuer);
    }
  }

  public void RemovePursuer(IHierarchical pursuer) {
    _pursuers.Remove(pursuer);
  }

  public void AddLaunchedHierarchical(IHierarchical hierarchical) {
    if (!_launchedHierarchicals.Contains(hierarchical)) {
      _launchedHierarchicals.Add(hierarchical);
    }
  }

  public void Update(int maxClusterSize) {
    // Perform recursive clustering on the assigned targets as necessary.
    RecursiveCluster(maxClusterSize);
  }

  public void RecursiveCluster(int maxClusterSize) {
    if (SubHierarchicals.Count > 0) {
      foreach (var subHierarchical in SubHierarchicals) {
        subHierarchical.RecursiveCluster(maxClusterSize);
      }
      return;
    }
    if (Target == null || Target.SubHierarchicals.Count <= maxClusterSize) {
      return;
    }

    // Perform clustering on the assigned targets.
    // TODO(titan): Define a better heuristic for choosing the clustering algorithm to minimize the
    // size and radius of each cluster without generating too many clusters.
    IClusterer clusterer = null;
    if (Target.SubHierarchicals.Count >=
        _maxNumSubHierarchicals * Mathf.Max(maxClusterSize / 2, 1)) {
      clusterer = new KMeansClusterer(_maxNumSubHierarchicals);
    } else {
      clusterer = new AgglomerativeClusterer(maxClusterSize, _clusterMaxRadius);
    }
    List<Cluster> clusters = clusterer.Cluster(Target.SubHierarchicals);

    // Generate sub-hierarchical objects to manage the target clusters.
    foreach (var cluster in clusters) {
      var subHierarchical = new HierarchicalBase { Target = cluster };
      AddSubHierarchical(subHierarchical);
      subHierarchical.RecursiveCluster(maxClusterSize);
    }
  }

  private Vector3 GetMean(System.Func<IHierarchical, Vector3> selector) {
    Vector3 sum = Vector3.zero;
    int count = 0;
    foreach (var subHierarchical in ActiveSubHierarchicals) {
      sum += selector(subHierarchical);
      ++count;
    }
    if (count == 0) {
      return Vector3.zero;
    }
    return sum / count;
  }
}
