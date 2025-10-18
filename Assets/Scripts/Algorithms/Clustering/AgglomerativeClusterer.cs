using System.Collections.Generic;
using UnityEngine;

// The agglomerative clusterer performs agglomerative clustering with the stopping condition given
// by the size and radius constraints.
public class AgglomerativeClusterer : SizeAndRadiusConstrainedClustererBase {
  public AgglomerativeClusterer(int maxSize, float maxRadius) : base(maxSize, maxRadius) {}

  // Generate the clusters from the list of hierarchical objects.
  public override List<Cluster> Cluster(IEnumerable<IHierarchical> hierarchicals) {
    var clusters = new List<Cluster>();
    foreach (var hierarchical in hierarchicals) {
      var cluster = new Cluster { Centroid = hierarchical.Position };
      cluster.AddSubHierarchical(hierarchical);
      clusters.Add(cluster);
    }

    // Create a set containing all valid cluster indices.
    var validClusterIndices = new HashSet<int>();
    for (int i = 0; i < clusters.Count; ++i) {
      validClusterIndices.Add(i);
    }

    // Find the pairwise distances between all clusters.
    // Only the lower triangular part of the distances matrix is used, i.e., row index > column
    // index.
    var distances = new float[clusters.Count, clusters.Count];
    for (int i = 0; i < clusters.Count; ++i) {
      for (int j = 0; j < i; ++j) {
        distances[i, j] = Vector3.Distance(clusters[i].Centroid, clusters[j].Centroid);
      }
    }

    while (true) {
      // Find the minimum distance between two clusters.
      float minDistance = Mathf.Infinity;
      int clusterIndex1 = -1;
      int clusterIndex2 = -1;
      // Invariant: clusterIndex1 > clusterIndex2.
      foreach (int i in validClusterIndices) {
        for (int j = 0; j < i; ++j) {
          if (validClusterIndices.Contains(j) && distances[i, j] < minDistance) {
            minDistance = distances[i, j];
            clusterIndex1 = i;
            clusterIndex2 = j;
          }
        }
      }

      // Check whether the minimum distance exceeds the maximum cluster radius, in which case the
      // algorithm has converged. This produces a conservative solution because the radius of a
      // merged cluster is less than or equal to the sum of the original cluster radii.
      if (minDistance == Mathf.Infinity || minDistance > _maxRadius) {
        break;
      }

      // Check whether merging the two clusters would violate the size constraint.
      if (clusters[clusterIndex1].Size + clusters[clusterIndex2].Size > _maxSize) {
        distances[clusterIndex1, clusterIndex2] = Mathf.Infinity;
        continue;
      }

      // Merge the two clusters together and keep the cluster with the smaller index, i.e.,
      // clusterIndex2.
      clusters[clusterIndex2].Merge(clusters[clusterIndex1]);
      clusters[clusterIndex2].Recenter();
      validClusterIndices.Remove(clusterIndex1);

      // Update the distances matrix using the distance between the cluster centroids.
      // TODO(titan): Change the distance metric to use average or maximum linkage.
      foreach (int i in validClusterIndices) {
        float distance = Vector3.Distance(clusters[clusterIndex2].Centroid, clusters[i].Centroid);
        if (i < clusterIndex2) {
          distances[clusterIndex2, i] = distance;
        } else if (i > clusterIndex2) {
          distances[i, clusterIndex2] = distance;
        }
      }
    }

    // Select only the valid clusters.
    for (int i = clusters.Count - 1; i >= 0; --i) {
      if (!validClusterIndices.Contains(i)) {
        clusters.RemoveAt(i);
      }
    }
    return clusters;
  }
}
