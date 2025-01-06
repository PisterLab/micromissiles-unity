using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;

public class KMeansClustererTest {
  public static readonly List<GameObject> Objects = new List<GameObject> {
    GenerateObject(new Vector3(0, 0, 0)),
    GenerateObject(new Vector3(0, 1, 0)),
    GenerateObject(new Vector3(0, 1.5f, 0)),
    GenerateObject(new Vector3(0, 2.5f, 0)),
  };

  public static GameObject GenerateObject(in Vector3 position) {
    GameObject obj = new GameObject();
    obj.transform.position = position;
    return obj;
  }

  [Test]
  public void TestSingleCluster() {
    KMeansClusterer clusterer = new KMeansClusterer(Objects, k: 1);
    clusterer.Cluster();
    Cluster cluster = clusterer.Clusters[0];
    Assert.AreEqual(cluster.Size(), Objects.Count);
    Assert.AreEqual(cluster.Coordinates, new Vector3(0, 1.25f, 0));
    Assert.AreEqual(cluster.Centroid(), new Vector3(0, 1.25f, 0));
  }
}

public class ConstrainedKMeansClustererTest {
  public static readonly List<GameObject> Objects = new List<GameObject> {
    GenerateObject(new Vector3(0, 0, 0)),
    GenerateObject(new Vector3(0, 1, 0)),
    GenerateObject(new Vector3(0, 1.5f, 0)),
    GenerateObject(new Vector3(0, 2.5f, 0)),
  };

  public static GameObject GenerateObject(in Vector3 position) {
    GameObject obj = new GameObject();
    obj.transform.position = position;
    return obj;
  }

  [Test]
  public void TestSingleCluster() {
    ConstrainedKMeansClusterer clusterer =
        new ConstrainedKMeansClusterer(Objects, maxSize: Objects.Count, maxRadius: Mathf.Infinity);
    clusterer.Cluster();
    Assert.AreEqual(clusterer.Clusters.Count, 1);
    Cluster cluster = clusterer.Clusters[0];
    Assert.AreEqual(cluster.Size(), Objects.Count);
    Assert.AreEqual(cluster.Centroid(), new Vector3(0, 1.25f, 0));
  }

  [Test]
  public void TestMaxSizeOne() {
    ConstrainedKMeansClusterer clusterer =
        new ConstrainedKMeansClusterer(Objects, maxSize: 1, maxRadius: Mathf.Infinity);
    clusterer.Cluster();
    Assert.AreEqual(clusterer.Clusters.Count, Objects.Count);
    foreach (var cluster in clusterer.Clusters) {
      Assert.AreEqual(cluster.Size(), 1);
    }
  }

  [Test]
  public void TestZeroRadius() {
    ConstrainedKMeansClusterer clusterer =
        new ConstrainedKMeansClusterer(Objects, maxSize: Objects.Count, maxRadius: 0);
    clusterer.Cluster();
    Assert.AreEqual(clusterer.Clusters.Count, Objects.Count);
    foreach (var cluster in clusterer.Clusters) {
      Assert.AreEqual(cluster.Size(), 1);
    }
  }

  [Test]
  public void TestSmallRadius() {
    ConstrainedKMeansClusterer clusterer =
        new ConstrainedKMeansClusterer(Objects, maxSize: Objects.Count, maxRadius: 1);
    clusterer.Cluster();
    Assert.AreEqual(clusterer.Clusters.Count, 2);
    List<Cluster> clusters = clusterer.Clusters.OrderBy(cluster => cluster.Coordinates[1]).ToList();
  }
}
