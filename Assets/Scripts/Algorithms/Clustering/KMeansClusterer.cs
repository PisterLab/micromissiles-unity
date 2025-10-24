using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// The k-means clusterer class performs k-means clustering.
public class KMeansClusterer : ClustererBase {
  // Default maximum number of iterations.
  protected const int _defaultMaxNumIterations = 20;

  // Convergence threshold.
  protected const float _epsilon = 1e-3f;

  // Number of clusters.
  private readonly int _k;

  // Maximum number of iterations.
  private readonly int _maxNumIterations;

  public KMeansClusterer(int k) : this(k, _defaultMaxNumIterations) {}
  public KMeansClusterer(int k, int maxNumIterations) {
    _k = k;
    _maxNumIterations = maxNumIterations;
  }

  // Generate the clusters from the list of hierarchical objects.
  public override List<Cluster> Cluster(IEnumerable<IHierarchical> hierarchicals) {
    // Initialize the clusters with centroids located at the positions of k random hierarchical
    // objects. Perform Fisher-Yates shuffling to find k random hierarchical objects.
    List<IHierarchical> hierarchicalsList = hierarchicals.ToList();
    var clusters = new List<Cluster>();
    for (int i = 0; i < _k; ++i) {
      int j = Random.Range(i, hierarchicalsList.Count);
      (hierarchicalsList[i], hierarchicalsList[j]) = (hierarchicalsList[j], hierarchicalsList[i]);

      clusters.Add(new Cluster { Centroid = hierarchicalsList[i].Position });
    }

    bool converged = false;
    int numIteration = 0;
    while (!converged && numIteration < _maxNumIterations) {
      AssignToClusters(clusters, hierarchicals);

      // Calculate the new clusters as the mean of all assigned hierarchical objects.
      converged = true;
      for (int clusterIndex = 0; clusterIndex < clusters.Count; ++clusterIndex) {
        Vector3 oldClusterPosition = clusters[clusterIndex].Centroid;
        if (clusters[clusterIndex].IsEmpty) {
          int hierarchicalIndex = Random.Range(0, hierarchicalsList.Count);
          clusters[clusterIndex].Centroid = hierarchicalsList[hierarchicalIndex].Position;
        } else {
          clusters[clusterIndex].Recenter();
        }

        // Check whether the algorithm has converged by checking whether the cluster has moved.
        if (Vector3.Distance(oldClusterPosition, clusters[clusterIndex].Position) > _epsilon) {
          converged = false;
        }
      }
      ++numIteration;
    }
    AssignToClusters(clusters, hierarchicals);
    return clusters;
  }

  private static void AssignToClusters(IReadOnlyList<Cluster> clusters,
                                       IEnumerable<IHierarchical> hierarchicals) {
    // Determine the closest centroid to each hierarchical object.
    foreach (var hierarchical in hierarchicals) {
      float minDistance = Mathf.Infinity;
      int minIndex = -1;
      for (int clusterIndex = 0; clusterIndex < clusters.Count; ++clusterIndex) {
        float distance = Vector3.Distance(clusters[clusterIndex].Centroid, hierarchical.Position);
        if (distance < minDistance) {
          minDistance = distance;
          minIndex = clusterIndex;
        }
      }
      clusters[minIndex].AddSubHierarchical(hierarchical);
    }
  }
}
