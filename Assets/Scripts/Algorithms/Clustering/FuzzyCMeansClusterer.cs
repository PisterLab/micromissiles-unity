using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// The fuzzy c-means clusterer performs fuzzy c-means clustering.
public class FuzzyCMeansClusterer : ClustererBase {
  // Default maximum number of iterations.
  protected const int _defaultMaxNumIterations = 50;

  // Default fuzziness parameter (m).
  protected const float _defaultFuzziness = 2f;

  // Default convergence threshold.
  protected const float _defaultEpsilon = 1e-3f;

  private const float _distanceEpsilon = 1e-6f;

  // Number of clusters.
  private readonly int _k;

  // Fuzziness parameter (m).
  private readonly float _fuzziness;

  // Maximum number of iterations.
  private readonly int _maxNumIterations;

  // Convergence threshold.
  private readonly float _epsilon;

  public FuzzyCMeansClusterer(int k)
      : this(k, _defaultFuzziness, _defaultMaxNumIterations, _defaultEpsilon) {}

  public FuzzyCMeansClusterer(int k, float fuzziness, int maxNumIterations, float epsilon) {
    if (k <= 0) {
      throw new ArgumentOutOfRangeException(nameof(k), "Number of clusters must be positive.");
    }
    if (fuzziness <= 1f) {
      throw new ArgumentOutOfRangeException(nameof(fuzziness), "Fuzziness must be greater than 1.");
    }
    if (maxNumIterations <= 0) {
      throw new ArgumentOutOfRangeException(nameof(maxNumIterations),
                                            "Maximum iterations must be positive.");
    }
    if (epsilon <= 0f) {
      throw new ArgumentOutOfRangeException(nameof(epsilon), "Epsilon must be positive.");
    }
    _k = k;
    _fuzziness = fuzziness;
    _maxNumIterations = maxNumIterations;
    _epsilon = epsilon;
  }

  // Generate the clusters from the list of hierarchical objects.
  public override List<Cluster> Cluster(IEnumerable<IHierarchical> hierarchicals) {
    return ClusterFuzzy(hierarchicals).Clusters;
  }

  public FuzzyCMeansResult ClusterFuzzy(IEnumerable<IHierarchical> hierarchicals,
                                        IReadOnlyList<Vector3> initialCentroids = null,
                                        float membershipThreshold = 1f,
                                        int maxMembershipsPerPoint = 1) {
    if (hierarchicals == null) {
      return new FuzzyCMeansResult {
        Points = new List<IHierarchical>(),
        Centroids = new List<Vector3>(),
        Memberships = new float[0, 0],
        Clusters = new List<Cluster>(),
      };
    }

    if (membershipThreshold < 0f || membershipThreshold > 1f) {
      throw new ArgumentOutOfRangeException(
          nameof(membershipThreshold), "Membership threshold must be between 0 and 1.");
    }
    if (maxMembershipsPerPoint <= 0) {
      throw new ArgumentOutOfRangeException(
          nameof(maxMembershipsPerPoint), "Max memberships per point must be positive.");
    }

    List<IHierarchical> hierarchicalsList = hierarchicals.ToList();
    if (hierarchicalsList.Count == 0) {
      return new FuzzyCMeansResult {
        Points = new List<IHierarchical>(),
        Centroids = new List<Vector3>(),
        Memberships = new float[0, 0],
        Clusters = new List<Cluster>(),
      };
    }

    // Validate that k is not greater than the number of hierarchical objects.
    if (_k > hierarchicalsList.Count) {
      throw new InvalidOperationException(
          $"Cannot create {_k} clusters from {hierarchicalsList.Count} hierarchical objects.");
    }

    List<Vector3> positions = hierarchicalsList.Select(hierarchical => hierarchical.Position)
                                   .ToList();
    var centroids = new List<Vector3>(_k);
    if (initialCentroids != null && initialCentroids.Count == _k) {
      centroids.AddRange(initialCentroids);
    } else {
      var indices = new List<int>(positions.Count);
      for (int i = 0; i < positions.Count; ++i) {
        indices.Add(i);
      }
      for (int i = 0; i < _k; ++i) {
        int j = UnityEngine.Random.Range(i, indices.Count);
        (indices[i], indices[j]) = (indices[j], indices[i]);
        centroids.Add(positions[indices[i]]);
      }
    }

    float[,] memberships = new float[positions.Count, _k];
    UpdateMemberships(positions, centroids, memberships, _fuzziness);

    bool converged = false;
    int numIteration = 0;
    while (!converged && numIteration < _maxNumIterations) {
      List<Vector3> newCentroids = UpdateCentroids(positions, memberships, _fuzziness, _k);

      float maxShift = 0f;
      for (int clusterIndex = 0; clusterIndex < _k; ++clusterIndex) {
        float shift = Vector3.Distance(centroids[clusterIndex], newCentroids[clusterIndex]);
        if (shift > maxShift) {
          maxShift = shift;
        }
      }
      centroids = newCentroids;

      UpdateMemberships(positions, centroids, memberships, _fuzziness);

      converged = maxShift <= _epsilon;
      ++numIteration;
    }

    List<Vector3> centroidSnapshot = centroids.ToList();
    int cappedMaxMembershipsPerPoint = Mathf.Min(maxMembershipsPerPoint, _k);
    List<Cluster> clusters = BuildClusters(hierarchicalsList, centroids, memberships,
                                           membershipThreshold, cappedMaxMembershipsPerPoint);
    return new FuzzyCMeansResult {
      Points = hierarchicalsList,
      Centroids = centroidSnapshot,
      Memberships = memberships,
      Clusters = clusters,
    };
  }

  private struct MembershipEntry {
    public int Index;
    public float Value;
  }

  private static List<Cluster> BuildClusters(IReadOnlyList<IHierarchical> hierarchicals,
                                             IReadOnlyList<Vector3> centroids,
                                             float[,] memberships,
                                             float membershipThreshold,
                                             int maxMembershipsPerPoint) {
    int numClusters = centroids.Count;
    int numPoints = hierarchicals.Count;
    var clusters = new List<Cluster>(numClusters);
    for (int clusterIndex = 0; clusterIndex < numClusters; ++clusterIndex) {
      clusters.Add(new Cluster { Centroid = centroids[clusterIndex] });
    }

    var clusterSizes = new int[numClusters];
    var membershipsForPoint = new List<MembershipEntry>(numClusters);
    for (int pointIndex = 0; pointIndex < numPoints; ++pointIndex) {
      membershipsForPoint.Clear();

      float bestMembership = memberships[pointIndex, 0];
      int bestIndex = 0;
      for (int clusterIndex = 0; clusterIndex < numClusters; ++clusterIndex) {
        float membership = memberships[pointIndex, clusterIndex];
        if (membership > bestMembership) {
          bestMembership = membership;
          bestIndex = clusterIndex;
        }
        if (membership >= membershipThreshold) {
          membershipsForPoint.Add(
              new MembershipEntry { Index = clusterIndex, Value = membership });
        }
      }

      if (membershipsForPoint.Count == 0) {
        membershipsForPoint.Add(
            new MembershipEntry { Index = bestIndex, Value = bestMembership });
      } else if (membershipsForPoint.Count > maxMembershipsPerPoint) {
        membershipsForPoint.Sort((left, right) => right.Value.CompareTo(left.Value));
        membershipsForPoint.RemoveRange(maxMembershipsPerPoint,
                                        membershipsForPoint.Count - maxMembershipsPerPoint);
      }

      foreach (var membership in membershipsForPoint) {
        Cluster cluster = clusters[membership.Index];
        cluster.AddSubHierarchical(hierarchicals[pointIndex]);
        cluster.SetMembership(hierarchicals[pointIndex], membership.Value);
        ++clusterSizes[membership.Index];
      }
    }

    for (int clusterIndex = 0; clusterIndex < numClusters; ++clusterIndex) {
      if (clusterSizes[clusterIndex] > 0) {
        continue;
      }
      int bestPointIndex = 0;
      float bestMembership = memberships[0, clusterIndex];
      for (int pointIndex = 1; pointIndex < numPoints; ++pointIndex) {
        float membership = memberships[pointIndex, clusterIndex];
        if (membership > bestMembership) {
          bestMembership = membership;
          bestPointIndex = pointIndex;
        }
      }
      Cluster cluster = clusters[clusterIndex];
      cluster.AddSubHierarchical(hierarchicals[bestPointIndex]);
      cluster.SetMembership(hierarchicals[bestPointIndex], bestMembership);
      ++clusterSizes[clusterIndex];
    }

    foreach (var cluster in clusters) {
      cluster.Recenter();
    }
    return clusters;
  }

  private static void UpdateMemberships(IReadOnlyList<Vector3> positions,
                                        IReadOnlyList<Vector3> centroids,
                                        float[,] memberships,
                                        float fuzziness) {
    int numPoints = positions.Count;
    int numClusters = centroids.Count;
    float exponent = 2f / (fuzziness - 1f);

    var distances = new float[numClusters];
    for (int pointIndex = 0; pointIndex < numPoints; ++pointIndex) {
      int zeroCount = 0;
      for (int clusterIndex = 0; clusterIndex < numClusters; ++clusterIndex) {
        float distance = Vector3.Distance(positions[pointIndex], centroids[clusterIndex]);
        distances[clusterIndex] = distance;
        if (distance <= _distanceEpsilon) {
          ++zeroCount;
        }
      }

      if (zeroCount > 0) {
        float membership = 1f / zeroCount;
        for (int clusterIndex = 0; clusterIndex < numClusters; ++clusterIndex) {
          memberships[pointIndex, clusterIndex] =
              distances[clusterIndex] <= _distanceEpsilon ? membership : 0f;
        }
        continue;
      }

      for (int clusterIndex = 0; clusterIndex < numClusters; ++clusterIndex) {
        float sum = 0f;
        float distance = distances[clusterIndex];
        for (int otherIndex = 0; otherIndex < numClusters; ++otherIndex) {
          float ratio = distance / distances[otherIndex];
          sum += Mathf.Pow(ratio, exponent);
        }
        memberships[pointIndex, clusterIndex] = 1f / sum;
      }
    }
  }

  private static List<Vector3> UpdateCentroids(IReadOnlyList<Vector3> positions,
                                               float[,] memberships,
                                               float fuzziness,
                                               int numClusters) {
    int numPoints = positions.Count;
    var centroids = new List<Vector3>(numClusters);
    for (int clusterIndex = 0; clusterIndex < numClusters; ++clusterIndex) {
      Vector3 numerator = Vector3.zero;
      float denominator = 0f;
      for (int pointIndex = 0; pointIndex < numPoints; ++pointIndex) {
        float weight = Mathf.Pow(memberships[pointIndex, clusterIndex], fuzziness);
        numerator += weight * positions[pointIndex];
        denominator += weight;
      }

      if (denominator <= Mathf.Epsilon) {
        int randomIndex = UnityEngine.Random.Range(0, numPoints);
        centroids.Add(positions[randomIndex]);
      } else {
        centroids.Add(numerator / denominator);
      }
    }
    return centroids;
  }
}
