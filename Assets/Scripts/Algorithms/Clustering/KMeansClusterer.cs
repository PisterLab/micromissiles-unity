using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The k-means clusterer class performs k-means clustering.
public class KMeansClusterer : IClusterer {
  public const float Epsilon = 1e-3f;

  // Number of clusters.
  private int _k = 0;

  // Maximum number of iterations.
  private int _maxIterations = 20;

  public KMeansClusterer(List<Vector3> points, int k, int maxIterations = 20) : base(points) {
    _k = k;
    _maxIterations = maxIterations;
  }

  // Cluster the points.
  public override void Cluster() {
    // Initialize the clusters with centroids at random points.
    // Perform Fisher-Yates shuffling to find k random points.
    System.Random random = new System.Random();
    for (int i = _points.Count - 1; i >= _points.Count - _k; --i) {
      int j = random.Next(i + 1);
      (_points[i], _points[j]) = (_points[j], _points[i]);
    }
    for (int i = _points.Count - 1; i >= _points.Count - _k; --i) {
      _clusters.Add(new Cluster(_points[i]));
    }

    bool converged = false;
    int iteration = 0;
    while (!converged && iteration < _maxIterations) {
      AssignPointsToCluster();

      // Calculate the new clusters as the mean of all assigned points.
      converged = true;
      for (int clusterIndex = 0; clusterIndex < _clusters.Count; ++clusterIndex) {
        Cluster newCluster;
        if (_clusters[clusterIndex].IsEmpty()) {
          int pointIndex = random.Next(_points.Count);
          newCluster = new Cluster(_points[pointIndex]);
        } else {
          newCluster = new Cluster(_clusters[clusterIndex].Centroid());
        }

        // Check whether the algorithm has converged by checking whether the cluster has moved.
        if (Vector3.Distance(newCluster.Coordinates, _clusters[clusterIndex].Coordinates) >
            Epsilon) {
          converged = false;
        }

        _clusters[clusterIndex] = newCluster;
      }

      ++iteration;
    }

    AssignPointsToCluster();
  }

  private void AssignPointsToCluster() {
    // Determine the closest centroid to each point.
    foreach (var point in _points) {
      float minDistance = Mathf.Infinity;
      int minIndex = -1;
      for (int clusterIndex = 0; clusterIndex < _clusters.Count; ++clusterIndex) {
        float distance = Vector3.Distance(_clusters[clusterIndex].Coordinates, point);
        if (distance < minDistance) {
          minDistance = distance;
          minIndex = clusterIndex;
        }
      }
      _clusters[minIndex].AddPoint(point);
    }
  }
}

// The constrained k-means clusterer class performs k-means clustering under size and radius
// constraints.
public class ConstrainedKMeansClusterer : ISizeAndRadiusConstrainedClusterer {
  public ConstrainedKMeansClusterer(List<Vector3> points, int maxSize, float maxRadius)
      : base(points, maxSize, maxRadius) {}

  // Cluster the points.
  public override void Cluster() {
    int numClusters = (int)Mathf.Ceil(_points.Count / _maxSize);
    KMeansClusterer clusterer;
    while (true) {
      clusterer = new KMeansClusterer(_points, numClusters);
      clusterer.Cluster();

      // Count the number of over-populated and over-sized clusters.
      int numOverPopulatedClusters = 0;
      int numOverSizedClusters = 0;
      foreach (var cluster in clusterer.Clusters) {
        if (cluster.Size() > _maxSize) {
          ++numOverPopulatedClusters;
        }
        if (cluster.Radius() > _maxRadius) {
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
    _clusters = new List<Cluster>(clusterer.Clusters);
  }
}
