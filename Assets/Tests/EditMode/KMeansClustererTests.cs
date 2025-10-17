using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class KMeansClustererTests {
  private static readonly List<FixedHierarchical> _hierarchicals = new List<FixedHierarchical> {
    new FixedHierarchical(position: new Vector3(0, 0, 0)),
    new FixedHierarchical(position: new Vector3(0, 1, 0)),
    new FixedHierarchical(position: new Vector3(0, 1.5f, 0)),
    new FixedHierarchical(position: new Vector3(0, 2.5f, 0)),
  };

  private KMeansClusterer _clusterer;

  [SetUp]
  public void SetUp() {
    Random.InitState(5);
  }

  [Test]
  public void Cluster_SingleCluster() {
    _clusterer = new KMeansClusterer(k: 1);
    var clusters = _clusterer.Cluster(_hierarchicals);
    Assert.AreEqual(1, clusters.Count);
    var cluster = clusters[0];
    Assert.AreEqual(_hierarchicals.Count, cluster.Size);
    Assert.AreEqual(new Vector3(0, 1.25f, 0), cluster.Centroid);
    Assert.AreEqual(new Vector3(0, 1.25f, 0), cluster.Position);
  }

  [Test]
  public void Cluster_TwoDistinctClusters() {
    // Hierarchical objects near (0, 0, 0);
    var hierarchicalsA = new List<FixedHierarchical> {
      new FixedHierarchical(position: new Vector3(0, 0, 0)),
      new FixedHierarchical(position: new Vector3(1, 0, 0)),
      new FixedHierarchical(position: new Vector3(0, 1, 0)),
      new FixedHierarchical(position: new Vector3(1, 1, 0)),
    };

    // Hierarchical objects near (10, 10, 10).
    var hierarchicalsB = new List<FixedHierarchical> {
      new FixedHierarchical(position: new Vector3(10, 10, 10)),
      new FixedHierarchical(position: new Vector3(11, 10, 10)),
      new FixedHierarchical(position: new Vector3(10, 11, 10)),
      new FixedHierarchical(position: new Vector3(11, 11, 10)),
    };

    // Combine all hierarchical objects.
    var allHierarchicals = new List<FixedHierarchical>();
    allHierarchicals.AddRange(hierarchicalsA);
    allHierarchicals.AddRange(hierarchicalsB);

    // Create a k-means clusterer with k = 2 clusters.
    _clusterer = new KMeansClusterer(k: 2);
    var clusters = _clusterer.Cluster(allHierarchicals);
    Assert.AreEqual(2, clusters.Count);

    // Sort the clusters by the x-coordinate of their centroids.
    var sortedClusters = clusters.OrderBy(cluster => cluster.Centroid.x).ToList();
    var clusterA = sortedClusters[0];
    var clusterB = sortedClusters[1];
    Assert.AreEqual(4, clusterA.Size);
    Assert.AreEqual(4, clusterB.Size);
    Assert.AreEqual(new Vector3(0.5f, 0.5f, 0), clusterA.Centroid);
    Assert.AreEqual(new Vector3(0.5f, 0.5f, 0), clusterA.Position);
    Assert.AreEqual(new Vector3(10.5f, 10.5f, 10), clusterB.Centroid);
    Assert.AreEqual(new Vector3(10.5f, 10.5f, 10), clusterB.Position);
  }
}
