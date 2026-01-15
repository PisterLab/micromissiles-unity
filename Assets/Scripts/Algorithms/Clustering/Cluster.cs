using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// The cluster represents a collection of hierarchical objects.
public class Cluster : HierarchicalBase {
  [SerializeField]
  private Vector3 _centroid;

  private readonly Dictionary<IHierarchical, float> _memberships = new Dictionary<IHierarchical, float>();

  public Vector3 Centroid {
    get => _centroid;
    set => _centroid = value;
  }

  public IReadOnlyDictionary<IHierarchical, float> Memberships => _memberships;

  public int Size => ActiveSubHierarchicals.Count();
  public bool IsEmpty => Size == 0;

  public float GetMembership(IHierarchical hierarchical) {
    if (hierarchical == null) {
      return 0f;
    }
    return _memberships.TryGetValue(hierarchical, out float membership) ? membership : 0f;
  }

  internal void SetMembership(IHierarchical hierarchical, float membership) {
    if (hierarchical == null) {
      return;
    }
    _memberships[hierarchical] = Mathf.Clamp01(membership);
  }

  internal void ClearMemberships() {
    _memberships.Clear();
  }

  public float Radius() {
    if (IsEmpty) {
      return 0;
    }
    return ActiveSubHierarchicals.DefaultIfEmpty().Max(
        subHierarchical =>
            subHierarchical == null ? 0 : Vector3.Distance(Centroid, subHierarchical.Position));
  }

  public void Recenter() {
    Centroid = Position;
  }

  // Merging with another cluster does not update the centroid of the current cluster.
  public void Merge(Cluster cluster) {
    foreach (var subHierarchical in cluster.SubHierarchicals) {
      AddSubHierarchical(subHierarchical);
    }
  }
}
