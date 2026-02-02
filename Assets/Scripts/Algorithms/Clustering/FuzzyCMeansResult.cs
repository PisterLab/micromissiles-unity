using System.Collections.Generic;
using UnityEngine;

// Result of a fuzzy c-means clustering run.
public class FuzzyCMeansResult {
  public IReadOnlyList<IHierarchical> Points { get; init; }
  public IReadOnlyList<Vector3> Centroids { get; init; }
  public float[,] Memberships { get; init; }
  public List<Cluster> Clusters { get; init; }
}
