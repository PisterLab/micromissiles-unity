using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Base implementation of a hierarchical object.
//
// The position and velocity of a hierarchical object is defined as the mean of the positions and
// velocities of the sub-hierarchical objects.
[Serializable]
public class HierarchicalBase : IHierarchical {
  // Soft maximum number of sub-hierarchical objects. This is used for recursive clustering.
  private const int _maxNumSubHierarchicals = 10; // Var Def: target max clusters per node (k).

  // Maximum cluster radius in meters.
  private const float _clusterMaxRadius = 1000f; // Var Def: radius cap for agglomerative clustering.

  // Fuzzy clustering parameters for redundancy and drift handling.
  private const bool _useFuzzyClustering = true; // Var Def: toggle FCM for partitioning.
  private const float _fuzzyFuzziness = 2f; // Var Def: FCM fuzziness (m).
  private const int _fuzzyMaxNumIterations = 25; // Var Def: FCM iteration cap.
  private const float _fuzzyEpsilon = 1e-2f; // Var Def: FCM convergence threshold.
  private const float _fuzzyMembershipThreshold = 0.35f; // Var Def: membership cutoff for overlap.
  private const int _fuzzyMaxMembershipsPerThreat = 2; // Var Def: max clusters per threat.

  // List of hierarchical objects in the hierarchy level below.
  [SerializeReference]
  protected List<IHierarchical> _subHierarchicals = new List<IHierarchical>(); // Var Def: child nodes.

  // List of hierarchical objects pursuing this hierarchical object.
  [SerializeReference]
  private List<IHierarchical> _pursuers = new List<IHierarchical>(); // Var Def: pursuing agents.

  // Target hierarchical object.
  [SerializeReference]
  private IHierarchical _target; // Var Def: target assigned to this node.

  // List of launched hierarchical objects.
  [SerializeReference]
  private List<IHierarchical> _launchedHierarchicals = new List<IHierarchical>(); // Var Def: launched children.

  public IReadOnlyList<IHierarchical> SubHierarchicals => _subHierarchicals.AsReadOnly();

  // Return a list of active sub-hierarchical objects.
  public IEnumerable<IHierarchical> ActiveSubHierarchicals =>
      _subHierarchicals.Where(s => !s.IsTerminated);

  public virtual IHierarchical Target {
    get => _target;
    set { _target = value; }
  }

  public IReadOnlyList<IHierarchical> Pursuers => _pursuers.AsReadOnly();
  public IEnumerable<IHierarchical> ActivePursuers =>
      Pursuers.Where(pursuer => !pursuer.IsTerminated);

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

  public List<IHierarchical> LeafHierarchicals(bool activeOnly, bool withTargetOnly) {
    var subHierarchicals = (activeOnly ? ActiveSubHierarchicals : SubHierarchicals).ToList();
    if (subHierarchicals.Count > 0) {
      var leafHierarchicals = new List<IHierarchical>();
      foreach (var subHierarchical in subHierarchicals) {
        leafHierarchicals.AddRange(subHierarchical.LeafHierarchicals(activeOnly, withTargetOnly));
      }
      return leafHierarchicals;
    }

    if (withTargetOnly && (Target == null || Target.IsTerminated)) {
      return new List<IHierarchical>();
    }
    return new List<IHierarchical> { this };
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

  public void RemoveTargetHierarchical(IHierarchical target) {
    Target?.RemoveSubHierarchical(target);
    foreach (var subHierarchical in SubHierarchicals) {
      subHierarchical.RemoveTargetHierarchical(target);
    }
  }

  public void RecursiveCluster(int maxClusterSize) {
    if (SubHierarchicals.Count > 0) {
      foreach (var subHierarchical in SubHierarchicals) {
        subHierarchical.RecursiveCluster(maxClusterSize);
      }
      return;
    }
    if (Target == null) {
      return;
    }
    List<IHierarchical> activeTargets = Target.ActiveSubHierarchicals.ToList();
    int numActiveSubHierarchicals = activeTargets.Count;
    if (numActiveSubHierarchicals <= maxClusterSize) {
      return;
    }

    List<Cluster> clusters =
        ClusterTargets(activeTargets, maxClusterSize, _fuzzyMembershipThreshold,
                       _fuzzyMaxMembershipsPerThreat, SubHierarchicals);
    if (clusters.Count == 0) {
      return;
    }

    // Generate sub-hierarchical objects to manage the target clusters.
    foreach (var cluster in clusters) {
      var subHierarchical = new HierarchicalBase { Target = cluster };
      AddSubHierarchical(subHierarchical);
      subHierarchical.RecursiveCluster(maxClusterSize);
    }
  }

  public void RefreshClusters(int maxClusterSize) {
    RefreshClusters(maxClusterSize, _fuzzyMembershipThreshold, _fuzzyMaxMembershipsPerThreat);
  }

  public void RefreshClusters(int maxClusterSize, float membershipThreshold,
                              int maxMembershipsPerThreat) {
    if (Target == null) {
      ClearSubHierarchicals();
      return;
    }
    List<IHierarchical> activeTargets = Target.ActiveSubHierarchicals.ToList();
    if (activeTargets.Count <= maxClusterSize) {
      ClearSubHierarchicals();
      return;
    }

    List<Cluster> clusters =
        ClusterTargets(activeTargets, maxClusterSize, membershipThreshold, maxMembershipsPerThreat,
                       SubHierarchicals);
    if (clusters.Count == 0) {
      ClearSubHierarchicals();
      return;
    }

    ClearSubHierarchicals();
    foreach (var cluster in clusters) {
      var subHierarchical = new HierarchicalBase { Target = cluster };
      AddSubHierarchical(subHierarchical);
      subHierarchical.RefreshClusters(maxClusterSize, membershipThreshold, maxMembershipsPerThreat);
    }
  }

  private List<Cluster> ClusterTargets(IReadOnlyList<IHierarchical> activeTargets,
                                       int maxClusterSize,
                                       float membershipThreshold,
                                       int maxMembershipsPerThreat,
                                       IReadOnlyList<IHierarchical> existingSubHierarchicals) {
    if (activeTargets.Count == 0) {
      return new List<Cluster>();
    }

    // Perform clustering on the assigned targets.
    // TODO(titan): Define a better heuristic for choosing the clustering algorithm to minimize the
    // size and radius of each cluster without generating too many clusters.
    bool usePartitioningClusterer =
        activeTargets.Count >= _maxNumSubHierarchicals * Mathf.Max(maxClusterSize / 2, 1);
    if (usePartitioningClusterer) {
      if (_useFuzzyClustering) {
        int k = Mathf.Min(_maxNumSubHierarchicals, activeTargets.Count);
        var fuzzyClusterer =
            new FuzzyCMeansClusterer(k, _fuzzyFuzziness, _fuzzyMaxNumIterations, _fuzzyEpsilon);
        List<Vector3> initialCentroids = GetExistingCentroids(existingSubHierarchicals, k);
        FuzzyCMeansResult result = fuzzyClusterer.ClusterFuzzy(activeTargets, initialCentroids,
                                                               membershipThreshold,
                                                               maxMembershipsPerThreat);
        return result.Clusters;
      }
      return new KMeansClusterer(_maxNumSubHierarchicals).Cluster(activeTargets);
    }
    return new AgglomerativeClusterer(maxClusterSize, _clusterMaxRadius).Cluster(activeTargets);
  }

  private static List<Vector3> GetExistingCentroids(
      IReadOnlyList<IHierarchical> subHierarchicals,
      int k) {
    if (subHierarchicals == null || subHierarchicals.Count != k) {
      return null;
    }

    var centroids = new List<Vector3>(k);
    foreach (var subHierarchical in subHierarchicals) {
      if (subHierarchical?.Target is Cluster cluster) {
        centroids.Add(cluster.Centroid);
      } else {
        return null;
      }
    }
    return centroids;
  }

  public bool AssignNewTarget(IHierarchical hierarchical, int capacity) {
    // TODO(titan): Abstract the target picking strategy to its own interface and class.
    // TODO(titan): Consider whether the OnAssignSubInterceptor and OnReassignTarget events should
    // be modified to refer to the new parent interceptor.
    hierarchical.Target = FindBestHierarchicalTarget(hierarchical, capacity) ??
                          FindBestLeafHierarchicalTarget(hierarchical, capacity);
    return hierarchical.Target != null;
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

  private IHierarchical FindBestHierarchicalTarget(IHierarchical hierarchical, int capacity) {
    // Find all sub-hierarchical objects that have at least one active target but no more than the
    // interceptor capacity.
    List<IHierarchical> FindPossibleHierarchicalTargets(IHierarchical hierarchical) {
      if (hierarchical.Target == null) {
        return new List<IHierarchical>();
      }
      int numActiveTargets = hierarchical.Target.ActiveSubHierarchicals.Count();
      if (numActiveTargets > 0 && numActiveTargets <= capacity) {
        return new List<IHierarchical> { hierarchical.Target };
      }

      var possibleTargets = new List<IHierarchical>();
      foreach (var subHierarchical in hierarchical.SubHierarchicals) {
        possibleTargets.AddRange(FindPossibleHierarchicalTargets(subHierarchical));
      }
      return possibleTargets;
    }
    List<IHierarchical> possibleTargets = FindPossibleHierarchicalTargets(this);
    if (possibleTargets.Count == 0) {
      return null;
    }

    // Use a maximum speed assignment to select the target resulting in the maximum intercept speed.
    IAssignment targetAssignment =
        new MaxSpeedAssignment(Assignment.Assignment_EvenAssignment_Assign);
    List<AssignmentItem> assignments =
        targetAssignment.Assign(new List<IHierarchical> { hierarchical }, possibleTargets);
    if (assignments.Count != 1) {
      return null;
    }
    return assignments[0].Second;
  }

  private IHierarchical FindBestLeafHierarchicalTarget(IHierarchical hierarchical, int capacity) {
    List<IHierarchical> leafHierarchicalTargets =
        LeafHierarchicals(activeOnly: true, withTargetOnly: true)
            .Select(hierarchical => hierarchical.Target)
            .ToList();
    if (leafHierarchicalTargets.Count == 0) {
      return null;
    }

    // Use a maximum speed assignment to select the target resulting in the maximum intercept speed.
    IAssignment targetAssignment =
        new MaxSpeedAssignment(Assignment.Assignment_EvenAssignment_Assign);
    List<AssignmentItem> assignments =
        targetAssignment.Assign(new List<IHierarchical> { hierarchical }, leafHierarchicalTargets);
    if (assignments.Count != 1) {
      return null;
    }

    // Remove as many target sub-hierarchical objects until the interceptor capacity.
    var targetSubHierarchicals = assignments[0].Second.ActiveSubHierarchicals.ToList();
    var filteredSubHierarchicals =
        targetSubHierarchicals
            .OrderBy(subHierarchical =>
                         Vector3.Distance(subHierarchical.Position, assignments[0].Second.Position))
            .Take(capacity);
    var targetHierarchical = new HierarchicalBase();
    foreach (var subHierarchical in filteredSubHierarchicals) {
      targetHierarchical.AddSubHierarchical(subHierarchical);
    }
    return targetHierarchical;
  }
}
