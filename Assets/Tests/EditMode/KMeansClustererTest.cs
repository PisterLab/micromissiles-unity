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

  // Test to reveal improper clearing of cluster memberships.
  [Test]
  public void TestTwoDistinctClustersWithResetNeeded() {
    // Group A: points near (0, 0, 0).
    var groupA = new List<GameObject> {
      GenerateObject(new Vector3(0, 0, 0)),
      GenerateObject(new Vector3(1, 0, 0)),
      GenerateObject(new Vector3(0, 1, 0)),
      GenerateObject(new Vector3(1, 1, 0)),
    };

    // Group B: points near (10, 10, 10).
    var groupB = new List<GameObject> {
      GenerateObject(new Vector3(10, 10, 10)),
      GenerateObject(new Vector3(11, 10, 10)),
      GenerateObject(new Vector3(10, 11, 10)),
      GenerateObject(new Vector3(11, 11, 10)),
    };

    // Combine them.
    var objects = new List<GameObject>();
    objects.AddRange(groupA);
    objects.AddRange(groupB);

    // Create clusterer with k = 2.
    KMeansClusterer clusterer = new KMeansClusterer(objects, k: 2);
    clusterer.Cluster();

    // We expect exactly 2 clusters.
    Assert.AreEqual(2, clusterer.Clusters.Count);

    // Retrieve the clusters.
    Cluster c0 = clusterer.Clusters[0];
    Cluster c1 = clusterer.Clusters[1];

    // Because the clusters are well-separated, each cluster should contain all points from one
    // group or the other, not a mixture. Check via centroids.
    var centroid0 = c0.Centroid();
    var centroid1 = c1.Centroid();

    // One centroid should be near (0.5, 0.5, 0), the other near (10.5, 10.5, 10).
    var expectedCentroid0 = new Vector3(0.5f, 0.5f, 0);
    var expectedCentroid1 = new Vector3(10.5f, 10.5f, 10);
    bool correctPlacement = (centroid0 == expectedCentroid0 && centroid1 == expectedCentroid1) ||
                            (centroid0 == expectedCentroid1 && centroid1 == expectedCentroid0);
    Assert.IsTrue(
        correctPlacement,
        "Centroids not close to the expected group centers. Possible leftover membership from a previous iteration if clusters not cleared.");

    // Additionally, we can count membership to confirm that each cluster got exactly four points
    // for a more direct check.
    int cluster0Count = c0.Size();
    int cluster1Count = c1.Size();
    Assert.AreEqual(8, cluster0Count + cluster1Count,
                    "Total membership across clusters does not match the total number of objects.");

    // Even if the clusters swapped roles, each cluster should have 4 points if membership was
    // properly reset and re-assigned.
    bool clusterCountsValid = cluster0Count == 4 && cluster1Count == 4;
    Assert.IsTrue(clusterCountsValid,
                  $"Cluster sizes not as expected. c0={cluster0Count}, c1={cluster1Count}.");
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
