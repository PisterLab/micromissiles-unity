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

  public KMeansClusterer(List<GameObject> objects, int k, int maxIterations = 20) : base(objects) {
    _k = k;
    _maxIterations = maxIterations;
  }
  public KMeansClusterer(List<Agent> agents, int k, int maxIterations = 20) : base(agents) {
    _k = k;
    _maxIterations = maxIterations;
  }

  // Cluster the game objects.
  public override void Cluster() {
    // Initialize the clusters with centroids located at random game objects.
    // Perform Fisher-Yates shuffling to find k random game objects.
    System.Random random = new System.Random();
    for (int i = _objects.Count - 1; i >= _objects.Count - _k; --i) {
      int j = random.Next(i + 1);
      (_objects[i], _objects[j]) = (_objects[j], _objects[i]);
    }
    for (int i = _objects.Count - 1; i >= _objects.Count - _k; --i) {
      _clusters.Add(new Cluster(_objects[i]));
    }

    bool converged = false;
    int iteration = 0;
    while (!converged && iteration < _maxIterations) {
      AssignObjectsToCluster();

      // Calculate the new clusters as the mean of all assigned game objects.
      converged = true;
      for (int clusterIndex = 0; clusterIndex < _clusters.Count; ++clusterIndex) {
        Cluster newCluster;
        if (_clusters[clusterIndex].IsEmpty()) {
          int objectIndex = random.Next(_objects.Count);
          newCluster = new Cluster(_objects[objectIndex]);
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

    AssignObjectsToCluster();
  }

  private void AssignObjectsToCluster() {
    // Determine the closest centroid to each game object.
    foreach (var obj in _objects) {
      float minDistance = Mathf.Infinity;
      int minIndex = -1;
      for (int clusterIndex = 0; clusterIndex < _clusters.Count; ++clusterIndex) {
        float distance =
            Vector3.Distance(_clusters[clusterIndex].Coordinates, obj.transform.position);
        if (distance < minDistance) {
          minDistance = distance;
          minIndex = clusterIndex;
        }
      }
      _clusters[minIndex].AddObject(obj);
    }
  }
}

// The constrained k-means clusterer class performs k-means clustering under size and radius
// constraints.
public class ConstrainedKMeansClusterer : ISizeAndRadiusConstrainedClusterer {
  public ConstrainedKMeansClusterer(List<GameObject> objects, int maxSize, float maxRadius)
      : base(objects, maxSize, maxRadius) {}

  // Cluster the game objects.
  public override void Cluster() {
    int numClusters = (int)Mathf.Ceil(_objects.Count / _maxSize);
    KMeansClusterer clusterer;
    while (true) {
      clusterer = new KMeansClusterer(_objects, numClusters);
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
