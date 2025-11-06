using System.Collections.Generic;

// Interface for a clustering algorithm.
//
// The clustering algorithm generaters a list of clusters given a list of hierarchical objects.
public interface IClusterer {
  // Generate the clusters from the list of hierarchical objects.
  IEnumerable<Cluster> Cluster(IEnumerable<IHierarchical> hierarchicals);
}
