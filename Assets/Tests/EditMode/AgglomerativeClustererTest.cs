using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AgglomerativeClustererTest {
  public static readonly List<Vector3> Points = new List<Vector3> {
    new Vector3(0, 0, 0),
    new Vector3(0, 1, 0),
    new Vector3(0, 1.5f, 0),
    new Vector3(0, 2.5f, 0),
  };

  [Test]
  public void TestSingleCluster() {
    AgglomerativeClusterer clusterer =
        new AgglomerativeClusterer(Points, maxSize: Points.Count, maxRadius: Mathf.Infinity);
    clusterer.Cluster();
    Assert.AreEqual(clusterer.Clusters.Count, 1);
    Cluster cluster = clusterer.Clusters[0];
    Assert.AreEqual(cluster.Size(), Points.Count);
    Assert.AreEqual(cluster.Centroid(), new Vector3(0, 1.25f, 0));
  }

  [Test]
  public void TestMaxSizeOne() {
    AgglomerativeClusterer clusterer =
        new AgglomerativeClusterer(Points, maxSize: 1, maxRadius: Mathf.Infinity);
    clusterer.Cluster();
    Assert.AreEqual(clusterer.Clusters.Count, Points.Count);
    foreach (var cluster in clusterer.Clusters) {
      Assert.AreEqual(cluster.Size(), 1);
    }
  }

  [Test]
  public void TestZeroRadius() {
    AgglomerativeClusterer clusterer =
        new AgglomerativeClusterer(Points, maxSize: Points.Count, maxRadius: 0);
    clusterer.Cluster();
    Assert.AreEqual(clusterer.Clusters.Count, Points.Count);
    foreach (var cluster in clusterer.Clusters) {
      Assert.AreEqual(cluster.Size(), 1);
    }
  }

  [Test]
  public void TestSmallRadius() {
    AgglomerativeClusterer clusterer =
        new AgglomerativeClusterer(Points, maxSize: Points.Count, maxRadius: 1);
    clusterer.Cluster();
    Assert.AreEqual(clusterer.Clusters.Count, 3);
    List<Cluster> clusters = clusterer.Clusters.OrderBy(cluster => cluster.Position[1]).ToList();
    Assert.AreEqual(clusters[0].Size(), 1);
    Assert.AreEqual(clusters[0].Position, new Vector3(0, 0, 0));
    Assert.AreEqual(clusters[1].Size(), 2);
    Assert.AreEqual(clusters[1].Position, new Vector3(0, 1.25f, 0));
    Assert.AreEqual(clusters[2].Size(), 1);
    Assert.AreEqual(clusters[2].Position, new Vector3(0, 2.5f, 0));
  }
}
