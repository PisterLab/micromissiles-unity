using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// The clusterer class is an interface for clustering algorithms.
public abstract class IClusterer {
  // List of game objects to cluster.
  protected List<GameObject> _objects = new List<GameObject>();

  // List of clusters.
  protected List<Cluster> _clusters = new List<Cluster>();

  public IClusterer(List<GameObject> objects) {
    _objects = objects;
  }
  public IClusterer(List<Agent> agents) {
    _objects = agents.ConvertAll(agent => agent.gameObject).ToList();
  }

  // Get the list of game objects.
  public IReadOnlyList<GameObject> Objects {
    get { return _objects; }
  }

  // Get the list of clusters.
  public IReadOnlyList<Cluster> Clusters {
    get { return _clusters; }
  }

  // Cluster the game objects.
  public abstract void Cluster();
}

// The size and radius-constrained clusterer class is an interface for clustering algorithms with
// size and radius constraints. The size is defined as the maximum number of game objects within a
// cluster, and the radius denotes the maximum distance from the cluster's centroid to any of its
// assigned game objects.
public abstract class ISizeAndRadiusConstrainedClusterer : IClusterer {
  // Maximum cluster size.
  protected readonly int _maxSize = 0;

  // Maximum cluster radius.
  protected readonly float _maxRadius = 0;

  public ISizeAndRadiusConstrainedClusterer(List<GameObject> objects, int maxSize, float maxRadius)
      : base(objects) {
    _maxSize = maxSize;
    _maxRadius = maxRadius;
  }
  public ISizeAndRadiusConstrainedClusterer(List<Agent> agents, int maxSize, float maxRadius)
      : base(agents) {
    _maxSize = maxSize;
    _maxRadius = maxRadius;
  }
}
