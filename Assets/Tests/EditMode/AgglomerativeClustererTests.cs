using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AgglomerativeClustererTests {
  private static readonly List<FixedHierarchical> _hierarchicals = new List<FixedHierarchical> {
    new FixedHierarchical(position: new Vector3(0, 0, 0)),
    new FixedHierarchical(position: new Vector3(0, 1, 0)),
    new FixedHierarchical(position: new Vector3(0, 1.5f, 0)),
    new FixedHierarchical(position: new Vector3(0, 2.5f, 0)),
  };

  private AgglomerativeClusterer _clusterer;

  [Test]
  public void Cluster_NoConstraints_ReturnsSingleCluster() {
    _clusterer =
        new AgglomerativeClusterer(maxSize: _hierarchicals.Count, maxRadius: Mathf.Infinity);
    List<Cluster> clusters = _clusterer.Cluster(_hierarchicals);
    Assert.AreEqual(1, clusters.Count);
    Cluster cluster = clusters[0];
    Assert.AreEqual(_hierarchicals.Count, cluster.Size);
    Assert.AreEqual(new Vector3(0, 1.25f, 0), cluster.Centroid);
    Assert.AreEqual(new Vector3(0, 1.25f, 0), cluster.Position);
  }

  [Test]
  public void Cluster_MaxSizeOne_ReturnsSingletonClusters() {
    _clusterer = new AgglomerativeClusterer(maxSize: 1, maxRadius: Mathf.Infinity);
    List<Cluster> clusters = _clusterer.Cluster(_hierarchicals);
    Assert.AreEqual(_hierarchicals.Count, clusters.Count);
    foreach (var cluster in clusters) {
      Assert.AreEqual(1, cluster.Size);
    }
  }

  [Test]
  public void Cluster_ZeroRadius_ReturnsSingletonClusters() {
    _clusterer = new AgglomerativeClusterer(maxSize: _hierarchicals.Count, maxRadius: 0);
    List<Cluster> clusters = _clusterer.Cluster(_hierarchicals);
    Assert.AreEqual(_hierarchicals.Count, clusters.Count);
    foreach (var cluster in clusters) {
      Assert.AreEqual(1, cluster.Size);
    }
  }

  [Test]
  public void Cluster_SmallRadius_ReturnsMultipleClusters() {
    _clusterer = new AgglomerativeClusterer(maxSize: _hierarchicals.Count, maxRadius: 0.5f);
    List<Cluster> clusters = _clusterer.Cluster(_hierarchicals);
    Debug.Log(
        $"{clusters[0].Centroid} {clusters[1].Centroid} {clusters[0].Radius()} {clusters[1].Radius()}");
    Assert.AreEqual(3, clusters.Count);
    List<Cluster> sortedClusters = clusters.OrderBy(cluster => cluster.Centroid[1]).ToList();
    Assert.AreEqual(1, sortedClusters[0].Size);
    Assert.AreEqual(new Vector3(0, 0, 0), clusters[0].Centroid);
    Assert.AreEqual(new Vector3(0, 0, 0), clusters[0].Position);
    Assert.AreEqual(2, sortedClusters[1].Size);
    Assert.AreEqual(new Vector3(0, 1.25f, 0), clusters[1].Centroid);
    Assert.AreEqual(new Vector3(0, 1.25f, 0), clusters[1].Position);
    Assert.AreEqual(1, sortedClusters[2].Size);
    Assert.AreEqual(new Vector3(0, 2.5f, 0), clusters[2].Centroid);
    Assert.AreEqual(new Vector3(0, 2.5f, 0), clusters[2].Position);
  }

  [Test]
  public void Cluster_SmallSize_ReturnsMultipleClusters() {
    _clusterer = new AgglomerativeClusterer(maxSize: 2, maxRadius: 1);
    List<Cluster> clusters = _clusterer.Cluster(_hierarchicals);
    Debug.Log(
        $"{clusters[0].Centroid} {clusters[1].Centroid} {clusters[0].Size} {clusters[1].Size}");
    Assert.AreEqual(3, clusters.Count);
    List<Cluster> sortedClusters = clusters.OrderBy(cluster => cluster.Centroid[1]).ToList();
    Assert.AreEqual(1, sortedClusters[0].Size);
    Assert.AreEqual(new Vector3(0, 0, 0), clusters[0].Centroid);
    Assert.AreEqual(new Vector3(0, 0, 0), clusters[0].Position);
    Assert.AreEqual(2, sortedClusters[1].Size);
    Assert.AreEqual(new Vector3(0, 1.25f, 0), clusters[1].Centroid);
    Assert.AreEqual(new Vector3(0, 1.25f, 0), clusters[1].Position);
    Assert.AreEqual(1, sortedClusters[2].Size);
    Assert.AreEqual(new Vector3(0, 2.5f, 0), clusters[2].Centroid);
    Assert.AreEqual(new Vector3(0, 2.5f, 0), clusters[2].Position);
  }
}
