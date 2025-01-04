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
