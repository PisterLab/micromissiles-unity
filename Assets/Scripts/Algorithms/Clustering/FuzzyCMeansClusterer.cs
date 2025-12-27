using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The fuzzy c-means clusterer class performs fuzzy c means clustering with defuzzified assignments.
public class FuzzyCMeansClusterer : IClusterer {
  private int _k;
  private float _m;
  private int _maxIterations;
  private float _epsilon;
  private System.Random _random = new System.Random();

  public FuzzyCMeansClusterer(
      List<GameObject> objects, int k, float fuzziness = 2.0f, int maxIterations = 50,
      float epsilon = 1e-3f)
      : base(objects) {
    _k = k;
    _m = Mathf.Max(1.01f, fuzziness);
    _maxIterations = maxIterations;
    _epsilon = epsilon;
  }
  public FuzzyCMeansClusterer(
      List<Agent> agents, int k, float fuzziness = 2.0f, int maxIterations = 50,
      float epsilon = 1e-3f)
      : base(agents) {
    _k = k;
    _m = Mathf.Max(1.01f, fuzziness);
    _maxIterations = maxIterations;
    _epsilon = epsilon;
  }

  // Cluster the game objects.
  public override void Cluster() {
    if (_objects.Count == 0 || _k <= 0) {
      return;
    }

    int clusterCount = Mathf.Clamp(_k, 1, _objects.Count);
    float[,] membership = InitializeMembership(_objects.Count, clusterCount);
    Vector3[] centroids = new Vector3[clusterCount];

    float delta = Mathf.Infinity;
    int iteration = 0;
    while (iteration < _maxIterations && delta > _epsilon) {
      UpdateCentroids(membership, centroids, clusterCount);
      delta = UpdateMembership(membership, centroids, clusterCount);
      ++iteration;
    }

    for (int clusterIndex = 0; clusterIndex < clusterCount; ++clusterIndex) {
      _clusters.Add(new Cluster(centroids[clusterIndex]));
    }

    // Defuzzify by assigning each object to the cluster with the highest membership.
    for (int objectIndex = 0; objectIndex < _objects.Count; ++objectIndex) {
      int bestCluster = 0;
      float bestMembership = membership[objectIndex, 0];
      for (int clusterIndex = 1; clusterIndex < clusterCount; ++clusterIndex) {
        if (membership[objectIndex, clusterIndex] > bestMembership) {
          bestMembership = membership[objectIndex, clusterIndex];
          bestCluster = clusterIndex;
        }
      }
      _clusters[bestCluster].AddObject(_objects[objectIndex]);
    }
  }

  private float[,] InitializeMembership(int numObjects, int clusterCount) {
    float[,] membership = new float[numObjects, clusterCount];
    for (int i = 0; i < numObjects; ++i) {
      float sum = 0.0f;
      for (int j = 0; j < clusterCount; ++j) {
        membership[i, j] = (float)_random.NextDouble();
        sum += membership[i, j];
      }
      if (sum < Mathf.Epsilon) {
        membership[i, 0] = 1.0f;
        sum = 1.0f;
      }
      for (int j = 0; j < clusterCount; ++j) {
        membership[i, j] /= sum;
      }
    }
    return membership;
  }

  private void UpdateCentroids(float[,] membership, Vector3[] centroids, int clusterCount) {
    for (int clusterIndex = 0; clusterIndex < clusterCount; ++clusterIndex) {
      Vector3 weightedSum = Vector3.zero;
      float weightSum = 0.0f;
      for (int objectIndex = 0; objectIndex < _objects.Count; ++objectIndex) {
        float weight = Mathf.Pow(membership[objectIndex, clusterIndex], _m);
        weightedSum += weight * _objects[objectIndex].transform.position;
        weightSum += weight;
      }

      if (weightSum > Mathf.Epsilon) {
        centroids[clusterIndex] = weightedSum / weightSum;
      } else {
        int randomIndex = _random.Next(_objects.Count);
        centroids[clusterIndex] = _objects[randomIndex].transform.position;
      }
    }
  }

  private float UpdateMembership(float[,] membership, Vector3[] centroids, int clusterCount) {
    float maxChange = 0.0f;
    float exponent = 2.0f / (_m - 1.0f);
    const float MinDistance = 1e-6f;

    for (int objectIndex = 0; objectIndex < _objects.Count; ++objectIndex) {
      int zeroDistanceCluster = -1;
      for (int clusterIndex = 0; clusterIndex < clusterCount; ++clusterIndex) {
        float distance =
            Vector3.Distance(_objects[objectIndex].transform.position, centroids[clusterIndex]);
        if (distance < MinDistance) {
          zeroDistanceCluster = clusterIndex;
          break;
        }
      }

      if (zeroDistanceCluster >= 0) {
        for (int clusterIndex = 0; clusterIndex < clusterCount; ++clusterIndex) {
          float newValue = clusterIndex == zeroDistanceCluster ? 1.0f : 0.0f;
          maxChange = Mathf.Max(maxChange, Mathf.Abs(newValue - membership[objectIndex, clusterIndex]));
          membership[objectIndex, clusterIndex] = newValue;
        }
        continue;
      }

      for (int clusterIndex = 0; clusterIndex < clusterCount; ++clusterIndex) {
        float distanceToCluster =
            Vector3.Distance(_objects[objectIndex].transform.position, centroids[clusterIndex]);
        float denominator = 0.0f;
        for (int otherClusterIndex = 0; otherClusterIndex < clusterCount; ++otherClusterIndex) {
          float distanceToOther =
              Vector3.Distance(_objects[objectIndex].transform.position, centroids[otherClusterIndex]);
          float ratio = distanceToCluster / Mathf.Max(distanceToOther, MinDistance);
          denominator += Mathf.Pow(ratio, exponent);
        }

        float updated = denominator > 0.0f ? 1.0f / denominator : 0.0f;
        maxChange =
            Mathf.Max(maxChange, Mathf.Abs(updated - membership[objectIndex, clusterIndex]));
        membership[objectIndex, clusterIndex] = updated;
      }
    }

    return maxChange;
  }
}
