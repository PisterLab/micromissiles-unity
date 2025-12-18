using System;

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

  public SizeAndRadiusConstrainedClustererBase(int maxSize, float maxRadius) {
    if (maxSize <= 0) {
      throw new ArgumentOutOfRangeException("Maximum size must be positive.", nameof(maxSize));
    }
    if (maxRadius < 0) {
      throw new ArgumentOutOfRangeException("Maximum radius must be non-negative.",
                                            nameof(maxRadius));
    }
    _maxSize = maxSize;
    _maxRadius = maxRadius;
  }
}
