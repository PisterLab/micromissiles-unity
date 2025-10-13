using System.Collections.Generic;

// Base implementation of a clustering algorithm.
public abstract class ClustererBase : IClusterer {
  // Generate the clusters from the list of hierarchical objects.
  public abstract IEnumerable<Cluster> Cluster(IEnumerable<IHierarchical> hierarchicals);
}
