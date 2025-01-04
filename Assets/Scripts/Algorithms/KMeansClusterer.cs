using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// The k-means clusterer class performs k-means clustering.
public class KMeansClusterer : IClusterer {
  public const float Epsilon = 1e-3f;

  // Number of clusters.
  private int k = 0;

  // Maximum number of iterations.
  private int maxIterations = 20;

  public KMeansClusterer(List<Vector3> points, int k, int maxIterations = 20) : base(points) {
    this.k = k;
    this.maxIterations = maxIterations;
  }

  // Cluster the points.
  public override void Cluster() {
    // Initialize the clusters with centroids at random points.
    // Perform Fisher-Yates shuffling to find k random points.
    System.Random random = new System.Random();
    for (int i = points.Count - 1; i >= points.Count - k; --i) {
      int j = random.Next(i + 1);
      (points[i], points[j]) = (points[j], points[i]);
    }
    for (int i = points.Count - 1; i >= points.Count - k; --i) {
      clusters.Add(new Cluster(points[i]));
    }

    bool converged = false;
    int iteration = 0;
    while (!converged && iteration < maxIterations) {
      AssignPointsToCluster();

      // Calculate the new clusters as the mean of all assigned points.
      converged = true;
      for (int clusterIndex = 0; clusterIndex < clusters.Count; ++clusterIndex) {
        Cluster newCluster;
        if (clusters[clusterIndex].IsEmpty()) {
          int pointIndex = random.Next(points.Count);
          newCluster = new Cluster(points[pointIndex]);
        } else {
          newCluster = new Cluster(clusters[clusterIndex].Centroid());
        }

        // Check whether the algorithm has converged by checking whether the cluster has moved.
        if (Vector3.Distance(newCluster.Position, clusters[clusterIndex].Position) > Epsilon) {
          converged = false;
        }

        clusters[clusterIndex] = newCluster;
      }

      ++iteration;
    }

    AssignPointsToCluster();
  }

  private void AssignPointsToCluster() {
    // Determine the closest centroid to each point.
    foreach (var point in points) {
      float minDistance = Mathf.Infinity;
      int minIndex = -1;
      for (int clusterIndex = 0; clusterIndex < clusters.Count; ++clusterIndex) {
        float distance = Vector3.Distance(clusters[clusterIndex].Position, point);
        if (distance < minDistance) {
          minDistance = distance;
          minIndex = clusterIndex;
        }
      }
      clusters[minIndex].AddPoint(point);
    }
  }
}

// The k-means clusterer class performs k-means clustering.
public class ConstrainedKMeansClusterer : ISizeAndRadiusConstrainedClusterer {
  public ConstrainedKMeansClusterer(List<Vector3> points, int maxSize, float maxRadius)
      : base(points, maxSize, maxRadius) {}

  // Cluster the points.
  public override void Cluster() {
    int numClusters = (int)Mathf.Ceil(points.Count / maxSize);
    KMeansClusterer clusterer;
    while (true) {
      clusterer = new KMeansClusterer(points, numClusters);
      clusterer.Cluster();

      // Count the number of over-populated and over-sized clusters.
      int numOverPopulatedClusters = 0;
      int numOverSizedClusters = 0;
      foreach (var cluster in clusterer.Clusters) {
        if (cluster.Size() > maxSize) {
          ++numOverPopulatedClusters;
        }
        if (cluster.Radius() > maxRadius) {
          ++numOverSizedClusters;
        }
      }

      // If all clusters satisfy the size and radius constraints, the algorithm has converged.
      if (numOverPopulatedClusters == 0 && numOverSizedClusters == 0) {
        break;
      }

      numClusters +=
          (int)Mathf.Ceil(Mathf.Max(numOverPopulatedClusters, numOverSizedClusters) / 2.0f);
    }
    clusters = new List<Cluster>(clusterer.Clusters);
  }
}
