using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;

public class AgglomerativeClustererTest {
  public static GameObject GenerateObject(in Vector3 position) {
    GameObject obj = new GameObject();
    obj.transform.position = position;
    return obj;
  }

  public static readonly List<GameObject> Objects = new List<GameObject> {
    GenerateObject(new Vector3(0, 0, 0)),
    GenerateObject(new Vector3(0, 1, 0)),
    GenerateObject(new Vector3(0, 1.5f, 0)),
    GenerateObject(new Vector3(0, 2.5f, 0)),
  };

  [Test]
  public void TestSingleCluster() {
    AgglomerativeClusterer clusterer =
        new AgglomerativeClusterer(Objects, maxSize: Objects.Count, maxRadius: Mathf.Infinity);
    clusterer.Cluster();
    Assert.AreEqual(clusterer.Clusters.Count, 1);
    Cluster cluster = clusterer.Clusters[0];
    Assert.AreEqual(cluster.Size(), Objects.Count);
    Assert.AreEqual(cluster.Centroid(), new Vector3(0, 1.25f, 0));
  }

  [Test]
  public void TestMaxSizeOne() {
    AgglomerativeClusterer clusterer =
        new AgglomerativeClusterer(Objects, maxSize: 1, maxRadius: Mathf.Infinity);
    clusterer.Cluster();
    Assert.AreEqual(clusterer.Clusters.Count, Objects.Count);
    foreach (var cluster in clusterer.Clusters) {
      Assert.AreEqual(cluster.Size(), 1);
    }
  }

  [Test]
  public void TestZeroRadius() {
    AgglomerativeClusterer clusterer =
        new AgglomerativeClusterer(Objects, maxSize: Objects.Count, maxRadius: 0);
    clusterer.Cluster();
    Assert.AreEqual(clusterer.Clusters.Count, Objects.Count);
    foreach (var cluster in clusterer.Clusters) {
      Assert.AreEqual(cluster.Size(), 1);
    }
  }

  [Test]
  public void TestSmallRadius() {
    AgglomerativeClusterer clusterer =
        new AgglomerativeClusterer(Objects, maxSize: Objects.Count, maxRadius: 1);
    clusterer.Cluster();
    Assert.AreEqual(clusterer.Clusters.Count, 3);
    List<Cluster> clusters = clusterer.Clusters.OrderBy(cluster => cluster.Coordinates[1]).ToList();
    Assert.AreEqual(clusters[0].Size(), 1);
    Assert.AreEqual(clusters[0].Coordinates, new Vector3(0, 0, 0));
    Assert.AreEqual(clusters[1].Size(), 2);
    Assert.AreEqual(clusters[1].Coordinates, new Vector3(0, 1.25f, 0));
    Assert.AreEqual(clusters[2].Size(), 1);
    Assert.AreEqual(clusters[2].Coordinates, new Vector3(0, 2.5f, 0));
  }
}
