using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

public class ClusterTest {
  public static Cluster GenerateCluster(in IReadOnlyList<Vector3> points) {
    Cluster cluster = new Cluster();
    cluster.AddPoints(points);
    cluster.Recenter();
    return cluster;
  }

  [Test]
  public void TestSize() {
    const int size = 10;
    List<Vector3> points = new List<Vector3>();
    for (int i = 0; i < size; ++i) {
      points.Add(new Vector3(0, i, 0));
    }
    Cluster cluster = GenerateCluster(points);
    Assert.AreEqual(cluster.Size(), size);
  }

  [Test]
  public void TestIsEmpty() {
    Cluster emptyCluster = new Cluster();
    Assert.IsTrue(emptyCluster.IsEmpty());

    Cluster cluster = new Cluster();
    cluster.AddPoint(new Vector3(1, -1, 0));
    Assert.IsFalse(cluster.IsEmpty());
  }

  [Test]
  public void TestRadius() {
    const float radius = 5;
    List<Vector3> points = new List<Vector3> {
      new Vector3(0, radius, 0),
      new Vector3(0, -radius, 0),
    };
    Cluster cluster = GenerateCluster(points);
    Assert.AreEqual(cluster.Radius(), radius);
  }

  [Test]
  public void TestCentroid() {
    const float radius = 3;
    List<Vector3> points = new List<Vector3>();
    for (int i = -1; i <= 1; ++i) {
      for (int j = -1; j <= 1; ++j) {
        points.Add(new Vector3(i, j, 0));
      }
    }
    Cluster cluster = GenerateCluster(points);
    Assert.AreEqual(cluster.Centroid(), Vector3.zero);
  }

  [Test]
  public void TestRecenter() {
    const float radius = 3;
    List<Vector3> points = new List<Vector3>();
    for (int i = -1; i <= 1; ++i) {
      for (int j = -1; j <= 1; ++j) {
        points.Add(new Vector3(i, j, 0));
      }
    }
    Cluster cluster = GenerateCluster(points);
    cluster.AddPoint(new Vector3(10, -10, 0));
    Assert.AreNotEqual(cluster.Coordinates, new Vector3(1, -1, 0));
    cluster.Recenter();
    Assert.AreEqual(cluster.Coordinates, new Vector3(1, -1, 0));
  }

  [Test]
  public void TestMerge() {
    const int size = 10;
    List<Vector3> points1 = new List<Vector3>();
    List<Vector3> points2 = new List<Vector3>();
    for (int i = 0; i < size; ++i) {
      points1.Add(new Vector3(0, i, 0));
      points2.Add(new Vector3(i, 0, 0));
    }
    Cluster cluster1 = GenerateCluster(points1);
    Cluster cluster2 = GenerateCluster(points2);
    int size1 = cluster1.Size();
    int size2 = cluster2.Size();
    Vector3 centroid1 = cluster1.Centroid();
    Vector3 centroid2 = cluster2.Centroid();
    cluster1.Merge(cluster2);
    cluster1.Recenter();
    Assert.AreEqual(cluster1.Size(), size1 + size2);
    Assert.AreEqual(cluster1.Coordinates, (centroid1 + centroid2) / 2);
  }
}
