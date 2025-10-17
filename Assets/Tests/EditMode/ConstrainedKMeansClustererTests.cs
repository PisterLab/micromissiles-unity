using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class ConstrainedKMeansClustererTests {
  private static readonly List<FixedHierarchical> _hierarchicals = new List<FixedHierarchical> {
    new FixedHierarchical(position: new Vector3(0, 0, 0)),
    new FixedHierarchical(position: new Vector3(0, 1, 0)),
    new FixedHierarchical(position: new Vector3(0, 1.5f, 0)),
    new FixedHierarchical(position: new Vector3(0, 2.5f, 0)),
  };

  private ConstrainedKMeansClusterer _clusterer;

  [SetUp]
  public void SetUp() {
    Random.InitState(5);
  }

  [Test]
  public void Cluster_NoConstraints_ReturnsSingleCluster() {
    _clusterer =
        new ConstrainedKMeansClusterer(maxSize: _hierarchicals.Count, maxRadius: Mathf.Infinity);
    var clusters = _clusterer.Cluster(_hierarchicals);
    Assert.AreEqual(1, clusters.Count);
    var cluster = clusters[0];
    Assert.AreEqual(_hierarchicals.Count, cluster.Size);
    Assert.AreEqual(new Vector3(0, 1.25f, 0), cluster.Centroid);
    Assert.AreEqual(new Vector3(0, 1.25f, 0), cluster.Position);
  }

  [Test]
  public void Cluster_MaxSizeOne_ReturnsSingletonClusters() {
    _clusterer = new ConstrainedKMeansClusterer(maxSize: 1, maxRadius: Mathf.Infinity);
    var clusters = _clusterer.Cluster(_hierarchicals);
    Assert.AreEqual(_hierarchicals.Count, clusters.Count);
    foreach (var cluster in clusters) {
      Assert.AreEqual(1, cluster.Size);
    }
  }

  [Test]
  public void Cluster_ZeroRadius_ReturnsSingletonClusters() {
    _clusterer = new ConstrainedKMeansClusterer(maxSize: _hierarchicals.Count, maxRadius: 0);
    var clusters = _clusterer.Cluster(_hierarchicals);
    Assert.AreEqual(_hierarchicals.Count, clusters.Count);
    foreach (var cluster in clusters) {
      Assert.AreEqual(1, cluster.Size);
    }
  }

  [Test]
  public void Cluster_SmallRadius_ReturnsMultipleClusters() {
    _clusterer = new ConstrainedKMeansClusterer(maxSize: _hierarchicals.Count, maxRadius: 1);
    var clusters = _clusterer.Cluster(_hierarchicals);
    Assert.AreEqual(2, clusters.Count);
  }
}
