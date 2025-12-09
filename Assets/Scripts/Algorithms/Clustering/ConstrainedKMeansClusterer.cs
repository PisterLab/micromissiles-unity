using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// The constrained k-means clusterer performs k-means clustering under size and radius constraints.
public class ConstrainedKMeansClusterer : SizeAndRadiusConstrainedClustererBase {
  public ConstrainedKMeansClusterer(int maxSize, float maxRadius) : base(maxSize, maxRadius) {}

  // Generate the clusters from the list of hierarchical objects.
  public override List<Cluster> Cluster(IEnumerable<IHierarchical> hierarchicals) {
    if (hierarchicals == null || !hierarchicals.Any()) {
      return new List<Cluster>();
    }

    int numClusters = (int)Mathf.Ceil(hierarchicals.Count() / _maxSize);
    KMeansClusterer clusterer;
    List<Cluster> clusters;
    while (true) {
      clusterer = new KMeansClusterer(numClusters);
      clusters = clusterer.Cluster(hierarchicals);

      // Count the number of over-populated and over-sized clusters.
      int numOverPopulatedClusters = 0;
      int numOverSizedClusters = 0;
      foreach (var cluster in clusters) {
        if (cluster.Size > _maxSize) {
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
          (int)Mathf.Ceil(Mathf.Max(numOverPopulatedClusters, numOverSizedClusters) / 2f);
    }
    return clusters;
  }
}
