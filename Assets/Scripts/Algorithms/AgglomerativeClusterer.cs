using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// The agglomerative clusterer class performs agglomerative clustering with the stopping condition
// given by the size and radius constraints.
public class AgglomerativeClusterer : ISizeAndRadiusConstrainedClusterer {
  public AgglomerativeClusterer(List<Vector3> points, int maxSize, float maxRadius)
      : base(points, maxSize, maxRadius) {}

  // Cluster the points.
  public override void Cluster() {
    // Add a cluster for every point.
    foreach (var point in points) {
      Cluster cluster = new Cluster(point);
      cluster.AddPoint(point);
      clusters.Add(cluster);
    }

    // Create a set containing all valid cluster indices.
    HashSet<int> validClusterIndices = new HashSet<int>();
    for (int i = 0; i < clusters.Count; ++i) {
      validClusterIndices.Add(i);
    }

    // Find the pairwise distances between all clusters.
    // The upper triangular half of the distances matrix is unused.
    float[,] distances = new float[clusters.Count, clusters.Count];
    for (int i = 0; i < clusters.Count; ++i) {
      for (int j = 0; j < i; ++j) {
        distances[i, j] = Vector3.Distance(clusters[i].Position, clusters[j].Position);
      }
    }

    while (true) {
      // Find the minimum distance between two clusters.
      float minDistance = Mathf.Infinity;
      int clusterIndex1 = -1;
      int clusterIndex2 = -1;
      for (int i = 0; i < clusters.Count; ++i) {
        for (int j = 0; j < i; ++j) {
          if (distances[i, j] < minDistance) {
            minDistance = distances[i, j];
            clusterIndex1 = i;
            clusterIndex2 = j;
          }
        }
      }

      // Check whether the minimum distance exceeds the maximum cluster radius, in which case the
      // algorithm has converged. This produces a conservative solution because the radius of a
      // merged cluster is less than or equal to the sum of the original cluster radii.
      if (minDistance >= maxRadius) {
        break;
      }

      // Check whether merging the two clusters would violate the size constraint.
      if (clusters[clusterIndex1].Size() + clusters[clusterIndex2].Size() > maxSize) {
        distances[clusterIndex1, clusterIndex2] = Mathf.Infinity;
        continue;
      }

      // Merge the two clusters together.
      int minClusterIndex = Mathf.Min(clusterIndex1, clusterIndex2);
      int maxClusterIndex = Mathf.Max(clusterIndex1, clusterIndex2);
      clusters[minClusterIndex].Merge(clusters[maxClusterIndex]);
      clusters[minClusterIndex].Recenter();
      validClusterIndices.Remove(maxClusterIndex);

      // Update the distances matrix using the distance between the cluster centroids.
      // TODO(titan): Change the distance metric to use average or maximum linkage.
      for (int i = 0; i < minClusterIndex; ++i) {
        if (distances[minClusterIndex, i] < Mathf.Infinity) {
          distances[minClusterIndex, i] =
              Vector3.Distance(clusters[minClusterIndex].Position, clusters[i].Position);
        }
      }
      for (int i = minClusterIndex + 1; i < clusters.Count; ++i) {
        if (distances[i, minClusterIndex] < Mathf.Infinity) {
          distances[i, minClusterIndex] =
              Vector3.Distance(clusters[minClusterIndex].Position, clusters[i].Position);
        }
      }
      for (int i = 0; i < maxClusterIndex; ++i) {
        distances[maxClusterIndex, i] = Mathf.Infinity;
      }
      for (int i = maxClusterIndex + 1; i < clusters.Count; ++i) {
        distances[i, maxClusterIndex] = Mathf.Infinity;
      }
    }

    // Select only the valid clusters.
    for (int i = clusters.Count - 1; i >= 0; --i) {
      if (!validClusterIndices.Contains(i)) {
        clusters.RemoveAt(i);
      }
    }
  }
}
