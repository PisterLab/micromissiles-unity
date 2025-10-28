using System.Linq;
using UnityEngine;

// The cluster represents a collection of hierarchical objects.
public class Cluster : HierarchicalBase {
  [SerializeField]
  private Vector3 _centroid;

  public Vector3 Centroid {
    get => _centroid;
    set => _centroid = value;
  }

  public int Size => SubHierarchicals.Count;
  public bool IsEmpty => Size == 0;

  public float Radius() {
    if (IsEmpty) {
      return 0;
    }
    return SubHierarchicals.Where(subHierarchical => !subHierarchical.IsTerminated)
        .DefaultIfEmpty()
        .Max(subHierarchical => subHierarchical == null
                                    ? 0
                                    : Vector3.Distance(Centroid, subHierarchical.Position));
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
