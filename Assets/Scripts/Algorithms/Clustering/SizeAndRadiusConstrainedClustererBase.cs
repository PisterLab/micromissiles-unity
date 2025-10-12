using System.Collections.Generic;

// Base implementation of a size and radius-constrained clustering algorithm.
//
// The size is defined as the maximum number of hierarchical objects within a cluster, and the
// radius denotes the maximum distance from the cluster's centroid to any of its assigned
// hierarchical objects.
public abstract class SizeAndRadiusConstrainedClustererBase : ClustererBase {
  // Maximum cluster size.
  protected readonly int _maxSize = 0;

  // Maximum cluster radius.
  protected readonly float _maxRadius = 0;

  public SizeAndRadiusConstrainedClustererBase(IEnumerable<IHierarchical> objects, int maxSize,
                                               float maxRadius)
      : base(objects) {
    _maxSize = maxSize;
    _maxRadius = maxRadius;
  }
}
