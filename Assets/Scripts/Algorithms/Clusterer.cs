using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The clusterer class is an interface for clustering algorithms.
public abstract class IClusterer {
  // List of points to cluster.
  protected List<Vector3> points = new List<Vector3>();

  // List of clusters.
  protected List<Cluster> clusters = new List<Cluster>();

  public IClusterer(List<Vector3> points) {
    this.points = points;
  }

  // Get the list of points.
  public IReadOnlyList<Vector3> Points {
    get { return points; }
  }

  // Get the list of clusters.
  public IReadOnlyList<Cluster> Clusters {
    get { return clusters; }
  }

  // Cluster the points.
  public abstract void Cluster();
}

// The size and radius-constrained clusterer class is an interface for clustering algorithms with
// size and radius constraints. The size is defined as the maximum number of points within a
// cluster, and the radius denotes the maximum distance from the cluster's centroid to any of its
// assigned points.
public abstract class ISizeAndRadiusConstrainedClusterer : IClusterer {
  // Maximum cluster size.
  protected readonly int maxSize = 0;

  // Maximum cluster radius.
  protected readonly float maxRadius = 0;

  public ISizeAndRadiusConstrainedClusterer(List<Vector3> points, int maxSize, float maxRadius)
      : base(points) {
    this.maxSize = maxSize;
    this.maxRadius = maxRadius;
  }
}
