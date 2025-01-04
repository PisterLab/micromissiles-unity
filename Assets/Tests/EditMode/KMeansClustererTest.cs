using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;

public class KMeansClustererTest {
  public static readonly List<Vector3> Points = new List<Vector3> {
    new Vector3(0, 0, 0),
    new Vector3(0, 1, 0),
    new Vector3(0, 1.5f, 0),
    new Vector3(0, 2.5f, 0),
  };

  [Test]
  public void TestSingleCluster() {
    KMeansClusterer clusterer = new KMeansClusterer(Points, k: 1);
    clusterer.Cluster();
    Cluster cluster = clusterer.Clusters[0];
    Assert.AreEqual(cluster.Size(), Points.Count);
    Assert.AreEqual(cluster.Position, new Vector3(0, 1.25f, 0));
    Assert.AreEqual(cluster.Centroid(), new Vector3(0, 1.25f, 0));
  }
}
