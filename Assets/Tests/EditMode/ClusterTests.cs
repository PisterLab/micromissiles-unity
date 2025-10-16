using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

public class ClusterTests {
  public static GameObject GenerateObject(in Vector3 position) {
    GameObject obj = new GameObject();
    obj.transform.position = position;
    return obj;
  }

  public static Cluster GenerateCluster(in IReadOnlyList<GameObject> objects) {
    Cluster cluster = new Cluster();
    cluster.AddObjects(objects);
    cluster.Recenter();
    return cluster;
  }

  [Test]
  public void TestSize() {
    const int size = 10;
    List<GameObject> objects = new List<GameObject>();
    for (int i = 0; i < size; ++i) {
      objects.Add(GenerateObject(new Vector3(0, i, 0)));
    }
    Cluster cluster = GenerateCluster(objects);
    Assert.AreEqual(size, cluster.Size());
  }

  [Test]
  public void TestIsEmpty() {
    Cluster emptyCluster = new Cluster();
    Assert.IsTrue(emptyCluster.IsEmpty());

    Cluster cluster = new Cluster();
    cluster.AddObject(new GameObject());
    Assert.IsFalse(cluster.IsEmpty());
  }

  [Test]
  public void TestRadius() {
    const float radius = 5;
    List<GameObject> objects = new List<GameObject>();
    objects.Add(GenerateObject(new Vector3(0, radius, 0)));
    objects.Add(GenerateObject(new Vector3(0, -radius, 0)));
    Cluster cluster = GenerateCluster(objects);
    Assert.AreEqual(radius, cluster.Radius());
  }

  [Test]
  public void TestCentroid() {
    List<GameObject> objects = new List<GameObject>();
    for (int i = -1; i <= 1; ++i) {
      for (int j = -1; j <= 1; ++j) {
        objects.Add(GenerateObject(new Vector3(i, j, 0)));
      }
    }
    Cluster cluster = GenerateCluster(objects);
    Assert.AreEqual(Vector3.zero, cluster.Centroid());
  }

  [Test]
  public void TestRecenter() {
    List<GameObject> objects = new List<GameObject>();
    for (int i = -1; i <= 1; ++i) {
      for (int j = -1; j <= 1; ++j) {
        objects.Add(GenerateObject(new Vector3(i, j, 0)));
      }
    }
    Cluster cluster = GenerateCluster(objects);
    cluster.AddObject(GenerateObject(new Vector3(10, -10, 0)));
    Assert.AreNotEqual(new Vector3(1, -1, 0), cluster.Coordinates);
    cluster.Recenter();
    Assert.AreEqual(new Vector3(1, -1, 0), cluster.Coordinates);
  }

  [Test]
  public void TestMerge() {
    const int size = 10;
    List<GameObject> objects1 = new List<GameObject>();
    List<GameObject> objects2 = new List<GameObject>();
    for (int i = 0; i < size; ++i) {
      objects1.Add(GenerateObject(new Vector3(0, i, 0)));
      objects2.Add(GenerateObject(new Vector3(i, 0, 0)));
    }
    Cluster cluster1 = GenerateCluster(objects1);
    Cluster cluster2 = GenerateCluster(objects2);
    int size1 = cluster1.Size();
    int size2 = cluster2.Size();
    Vector3 centroid1 = cluster1.Centroid();
    Vector3 centroid2 = cluster2.Centroid();
    cluster1.Merge(cluster2);
    cluster1.Recenter();
    Assert.AreEqual(size1 + size2, cluster1.Size());
    Assert.AreEqual((centroid1 + centroid2) / 2, cluster1.Coordinates);
  }
}
