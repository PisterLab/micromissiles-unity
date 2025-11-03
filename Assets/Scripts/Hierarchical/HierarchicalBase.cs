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
    _launchedHierarchicals.Add(hierarchical);
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
    if (Target == null || Target.ActiveSubHierarchicals.Count() <= maxClusterSize) {
      return;
    }

    // Perform clustering on the assigned targets.
    // TODO(titan): Define a better heuristic for choosing the clustering algorithm to minimize the
    // size and radius of each cluster without generating too many clusters.
    IClusterer clusterer = null;
    if (Target.ActiveSubHierarchicals.Count() >=
        _maxNumSubHierarchicals * Mathf.Max(maxClusterSize / 2, 1)) {
      clusterer = new KMeansClusterer(_maxNumSubHierarchicals);
    } else {
      clusterer = new AgglomerativeClusterer(maxClusterSize, _clusterMaxRadius);
    }
    List<Cluster> clusters = clusterer.Cluster(Target.ActiveSubHierarchicals);

    // Generate sub-hierarchical objects to manage the target clusters.
    foreach (var cluster in clusters) {
      var subHierarchical = new HierarchicalBase { Target = cluster };
      AddSubHierarchical(subHierarchical);
      subHierarchical.RecursiveCluster(maxClusterSize);
    }
  }

  public bool AssignNewTarget(IHierarchical hierarchical, int capacity) {
    hierarchical.Target = null;
    // TODO(titan): Abstract the target picking strategy to its own interface and class.
    // TODO(titan): Consider whether the OnAssignSubInterceptor and OnReassignTarget events should
    // be modified to refer to the new parent interceptor.
    hierarchical.Target ??= FindBestHierarchicalTarget(hierarchical, capacity) ??
                            FindBestLeafHierarchicalTarget(hierarchical, capacity);
    return hierarchical.Target != null;
  }

  public bool ReassignTarget(IHierarchical target) {
    // Recursively check through hierarchicals based on cluster position first (BFS).  Then check
    // through launched hierarchicals?
    return false;
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
    // Find all sub-hierarchical objects that have fewer than or as many targets as the interceptor
    // capacity.
    List<IHierarchical> FindPossibleHierarchicalTargets(IHierarchical hierarchical) {
      if (hierarchical.Target == null) {
        return new List<IHierarchical>();
      }
      if (hierarchical.Target.ActiveSubHierarchicals.Count() <= capacity) {
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
    List<IHierarchical> FindLeafHierarchicalTargets(IHierarchical hierarchical) {
      // Traverse the agent hierarchy to find only the leaf hierarchical objects.
      if (hierarchical.SubHierarchicals.Count > 0) {
        var leafHierarchicalTargets = new List<IHierarchical>();
        foreach (var subHierarchical in hierarchical.ActiveSubHierarchicals) {
          leafHierarchicalTargets.AddRange(FindLeafHierarchicalTargets(subHierarchical));
        }
        return leafHierarchicalTargets;
      }

      if (hierarchical.Target == null) {
        return new List<IHierarchical>();
      }
      return new List<IHierarchical> { hierarchical.Target };
    }
    List<IHierarchical> leafHierarchicalTargets = FindLeafHierarchicalTargets(this);
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
    var targetSubHierarchicals = assignments[0].Second.ActiveSubHierarchicals;
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

  private bool IsEscapingPursuers() {
    return Pursuers.All(pursuer => {
      // A hierarchical object is considered escaping a pursuer if the closing velocity is
      // non-positive or if the hierarchical object will reach its target before the pursuer reaches
      // the hierarchical object.
      Vector3 relativePosition = pursuer.Position - Position;
      Vector3 relativeVelocity = pursuer.Velocity - Velocity;
      float rangeRate = Vector3.Dot(relativeVelocity, relativePosition.normalized);
      float closingVelocity = -rangeRate;
      if (closingVelocity <= 0) {
        return true;
      }
      float pursuerDistance = relativePosition.magnitude;
      float targetDistance = (Target.Position - Position).magnitude;
      float timeToTarget = targetDistance / Speed;
      float pursuerTimeToIntercept = pursuerDistance / pursuer.Speed;
      return pursuerDistance > targetDistance && timeToTarget < pursuerTimeToIntercept;
    });
  }
}
