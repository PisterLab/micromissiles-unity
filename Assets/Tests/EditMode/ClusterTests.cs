using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class ClusterTests {
  private const float _epsilon = 1e-3f;

  private static Cluster GenerateCluster(IReadOnlyList<FixedHierarchical> hierarchicals) {
    var cluster = new Cluster();
    foreach (var hierarchical in hierarchicals) {
      cluster.AddSubHierarchical(hierarchical);
    }
    cluster.Recenter();
    return cluster;
  }

  [Test]
  public void Centroid_SetAndGets_WorksCorrectly() {
    var cluster = new Cluster();
    var centroid = new Vector3(5, 1, -3);
    cluster.Centroid = centroid;
    Assert.AreEqual(centroid, cluster.Centroid);
  }

  [Test]
  public void Size_ReturnsCorrectly() {
    const int size = 10;
    var hierarchicals = new List<FixedHierarchical>();
    for (int i = 0; i < size; ++i) {
      hierarchicals.Add(new FixedHierarchical(new Vector3(0, i, 0)));
    }
    Cluster cluster = GenerateCluster(hierarchicals);
    Assert.AreEqual(size, cluster.Size);
  }

  [Test]
  public void IsEmpty_ReturnsCorrectly() {
    var cluster = new Cluster();
    Assert.IsTrue(cluster.IsEmpty);
    cluster.AddSubHierarchical(new FixedHierarchical(Vector3.zero));
    Assert.IsFalse(cluster.IsEmpty);
  }

  [Test]
  public void Radius_ReturnsDistanceFromCentroidToFarthest() {
    const int size = 10;
    var hierarchicals = new List<FixedHierarchical>();
    for (int i = 0; i < size; ++i) {
      hierarchicals.Add(new FixedHierarchical(new Vector3(0, i, 0)));
    }
    Cluster cluster = GenerateCluster(hierarchicals);
    float centroid = size / 2 - 2;
    cluster.Centroid = new Vector3(0, centroid, 0);
    Assert.AreEqual(size - 1 - centroid, cluster.Radius(), _epsilon);
  }

  [Test]
  public void Radius_With_NoSubHierarchicals_ReturnsZero() {
    var cluster = new Cluster();
    Assert.AreEqual(0f, cluster.Radius(), _epsilon);
  }

  [Test]
  public void Recenter_SetsCentroidToMeanOfSubHierarchicalPositions() {
    var hierarchicals = new List<FixedHierarchical>();
    for (int i = -1; i <= 1; ++i) {
      for (int j = -1; j <= 1; ++j) {
        hierarchicals.Add(new FixedHierarchical(new Vector3(i, j, 0)));
      }
    }
    Cluster cluster = GenerateCluster(hierarchicals);
    cluster.AddSubHierarchical(new FixedHierarchical(new Vector3(10, -10, 0)));
    Assert.AreNotEqual(new Vector3(1, -1, 0), cluster.Centroid);
    cluster.Recenter();
    Assert.AreEqual(new Vector3(1, -1, 0), cluster.Centroid);
  }

  [Test]
  public void Merge_CombinesAllSubHierarchicals() {
    const int size = 10;
    var hierarchicals1 = new List<FixedHierarchical>();
    var hierarchicals2 = new List<FixedHierarchical>();
    for (int i = 0; i < size; ++i) {
      hierarchicals1.Add(new FixedHierarchical(new Vector3(0, i, 0)));
      hierarchicals2.Add(new FixedHierarchical(new Vector3(i, 0, 0)));
    }
    Cluster cluster1 = GenerateCluster(hierarchicals1);
    Cluster cluster2 = GenerateCluster(hierarchicals2);
    int size1 = cluster1.Size;
    int size2 = cluster2.Size;
    cluster1.Merge(cluster2);
    Assert.AreEqual(size1 + size2, cluster1.Size);
  }

  [Test]
  public void Merge_DoesNotUpdateCentroid() {
    const int size = 10;
    var hierarchicals1 = new List<FixedHierarchical>();
    var hierarchicals2 = new List<FixedHierarchical>();
    for (int i = 0; i < size; ++i) {
      hierarchicals1.Add(new FixedHierarchical(new Vector3(0, i, 0)));
      hierarchicals2.Add(new FixedHierarchical(new Vector3(i, 0, 0)));
    }
    Cluster cluster1 = GenerateCluster(hierarchicals1);
    Cluster cluster2 = GenerateCluster(hierarchicals2);
    Vector3 centroid1 = cluster1.Centroid;
    Vector3 centroid2 = cluster2.Centroid;
    cluster1.Merge(cluster2);
    Assert.AreNotEqual((centroid1 + centroid2) / 2, cluster1.Centroid);
    cluster1.Recenter();
    Assert.AreEqual((centroid1 + centroid2) / 2, cluster1.Centroid);
  }
}
