using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;

public class AgglomerativeClustererTests {
  public static GameObject GenerateObject(in Vector3 position) {
    GameObject obj = new GameObject();
    Agent agent = obj.AddComponent<DummyAgent>();
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
    Assert.AreEqual(1, clusterer.Clusters.Count);
    ClusterLegacy cluster = clusterer.Clusters[0];
    Assert.AreEqual(Objects.Count, cluster.Size());
    Assert.AreEqual(new Vector3(0, 1.25f, 0), cluster.Centroid());
  }

  [Test]
  public void TestMaxSizeOne() {
    AgglomerativeClusterer clusterer =
        new AgglomerativeClusterer(Objects, maxSize: 1, maxRadius: Mathf.Infinity);
    clusterer.Cluster();
    Assert.AreEqual(Objects.Count, clusterer.Clusters.Count);
    foreach (var cluster in clusterer.Clusters) {
      Assert.AreEqual(1, cluster.Size());
    }
  }

  [Test]
  public void TestZeroRadius() {
    AgglomerativeClusterer clusterer =
        new AgglomerativeClusterer(Objects, maxSize: Objects.Count, maxRadius: 0);
    clusterer.Cluster();
    Assert.AreEqual(Objects.Count, clusterer.Clusters.Count);
    foreach (var cluster in clusterer.Clusters) {
      Assert.AreEqual(1, cluster.Size());
    }
  }

  [Test]
  public void TestSmallRadius() {
    AgglomerativeClusterer clusterer =
        new AgglomerativeClusterer(Objects, maxSize: Objects.Count, maxRadius: 1);
    clusterer.Cluster();
    Assert.AreEqual(3, clusterer.Clusters.Count);
    List<ClusterLegacy> clusters =
        clusterer.Clusters.OrderBy(cluster => cluster.Coordinates[1]).ToList();
    Assert.AreEqual(1, clusters[0].Size());
    Assert.AreEqual(new Vector3(0, 0, 0), clusters[0].Coordinates);
    Assert.AreEqual(2, clusters[1].Size());
    Assert.AreEqual(new Vector3(0, 1.25f, 0), clusters[1].Coordinates);
    Assert.AreEqual(1, clusters[2].Size());
    Assert.AreEqual(new Vector3(0, 2.5f, 0), clusters[2].Coordinates);
  }
}
