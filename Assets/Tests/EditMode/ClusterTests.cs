using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class ClusterTests {
  private const float _epsilon = 1e-3f;

  private static FixedHierarchical GenerateObject(in Vector3 position) {
    return new FixedHierarchical(position, Vector3.zero, Vector3.zero);
  }

  private static Cluster GenerateCluster(IReadOnlyList<FixedHierarchical> objects) {
    Cluster cluster = new Cluster();
    foreach (var obj in objects) {
      cluster.AddSubHierarchical(obj);
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
    var objects = new List<FixedHierarchical>();
    for (int i = 0; i < size; ++i) {
      objects.Add(GenerateObject(new Vector3(0, i, 0)));
    }
    Cluster cluster = GenerateCluster(objects);
    Assert.AreEqual(size, cluster.Size);
  }

  [Test]
  public void IsEmpty_ReturnsCorrectly() {
    var cluster = new Cluster();
    Assert.IsTrue(cluster.IsEmpty);
    cluster.AddSubHierarchical(GenerateObject(Vector3.zero));
    Assert.IsFalse(cluster.IsEmpty);
  }

  [Test]
  public void Radius_ReturnsDistanceFromCentroidToFarthest() {
    const int size = 10;
    var objects = new List<FixedHierarchical>();
    for (int i = 0; i < size; ++i) {
      objects.Add(GenerateObject(new Vector3(0, i, 0)));
    }
    Cluster cluster = GenerateCluster(objects);
    var centroid = size / 2 - 2;
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
    var objects = new List<FixedHierarchical>();
    for (int i = -1; i <= 1; ++i) {
      for (int j = -1; j <= 1; ++j) {
        objects.Add(GenerateObject(new Vector3(i, j, 0)));
      }
    }
    var cluster = GenerateCluster(objects);
    cluster.AddSubHierarchical(GenerateObject(new Vector3(10, -10, 0)));
    Assert.AreNotEqual(new Vector3(1, -1, 0), cluster.Centroid);
    cluster.Recenter();
    Assert.AreEqual(new Vector3(1, -1, 0), cluster.Centroid);
  }

  [Test]
  public void Merge_CombinesAllSubHierarchicals() {
    const int size = 10;
    var objects1 = new List<FixedHierarchical>();
    var objects2 = new List<FixedHierarchical>();
    for (int i = 0; i < size; ++i) {
      objects1.Add(GenerateObject(new Vector3(0, i, 0)));
      objects2.Add(GenerateObject(new Vector3(i, 0, 0)));
    }
    var cluster1 = GenerateCluster(objects1);
    var cluster2 = GenerateCluster(objects2);
    int size1 = cluster1.Size;
    int size2 = cluster2.Size;
    cluster1.Merge(cluster2);
    Assert.AreEqual(size1 + size2, cluster1.Size);
  }

  [Test]
  public void Merge_DoesNotUpdateCentroid() {
    const int size = 10;
    var objects1 = new List<FixedHierarchical>();
    var objects2 = new List<FixedHierarchical>();
    for (int i = 0; i < size; ++i) {
      objects1.Add(GenerateObject(new Vector3(0, i, 0)));
      objects2.Add(GenerateObject(new Vector3(i, 0, 0)));
    }
    var cluster1 = GenerateCluster(objects1);
    var cluster2 = GenerateCluster(objects2);
    var centroid1 = cluster1.Centroid;
    var centroid2 = cluster2.Centroid;
    cluster1.Merge(cluster2);
    Assert.AreNotEqual((centroid1 + centroid2) / 2, cluster1.Centroid);
    cluster1.Recenter();
    Assert.AreEqual((centroid1 + centroid2) / 2, cluster1.Centroid);
  }
}
