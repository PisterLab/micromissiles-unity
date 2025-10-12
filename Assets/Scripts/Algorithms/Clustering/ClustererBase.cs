using System.Collections.Generic;
using System.Linq;

// Base implementation of a clustering algorithm.
public abstract class ClustererBase : IClusterer {
  // list of hierarchical objects to cluster.
  protected List<IHierarchical> _objects = new List<IHierarchical>();

  // list of generated clusters.
  protected List<Cluster> _clusters = new List<Cluster>();

  public ClustererBase(IEnumerable<IHierarchical> objects) {
    _objects = objects.ToList();
  }

  public IReadOnlyList<IHierarchical> Objects => _objects.AsReadOnly();
  public IReadOnlyList<Cluster> Clusters => _clusters.AsReadOnly();

  // Generate the clusters from the list of hierarchical objects.
  public abstract void Cluster();
}
