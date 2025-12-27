using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools.Utils;

public class KDTreeTests {
  [Test]
  public void NearestNeighbor_EmptyTree_ReturnsZero() {
    var tree = new KDTree<Vector2>(new List<Vector2>(), (Vector2 point) => point);
    Assert.AreEqual(Vector2.zero, tree.NearestNeighbor(Vector2.zero));
    Assert.AreEqual(Vector2.zero, tree.NearestNeighbor(new Vector2(1, 1)));
  }

  [Test]
  public void NearestNeighbor_SinglePoint_ReturnsSinglePoint() {
    var tree =
        new KDTree<Vector2>(new List<Vector2> { new Vector2(1, 1) }, (Vector2 point) => point);
    Assert.AreEqual(new Vector2(1, 1), tree.NearestNeighbor(Vector2.zero));
    Assert.AreEqual(new Vector2(1, 1), tree.NearestNeighbor(new Vector2(1, 6)));
    Assert.AreEqual(new Vector2(1, 1), tree.NearestNeighbor(new Vector2(3, 2)));
    Assert.AreEqual(new Vector2(1, 1), tree.NearestNeighbor(new Vector2(9, 9)));
  }

  [Test]
  public void NearestNeighbor_MatchesDataPoint_ReturnsSamePoint() {
    List<Vector2> points = new List<Vector2>();
    for (int i = 0; i <= 10; ++i) {
      for (int j = 0; j <= 10; ++j) {
        points.Add(new Vector2(i, j));
      }
    }
    var tree = new KDTree<Vector2>(points, (Vector2 point) => point);
    Assert.AreEqual(Vector2.zero, tree.NearestNeighbor(Vector2.zero));
    Assert.AreEqual(new Vector2(1, 6), tree.NearestNeighbor(new Vector2(1, 6)));
    Assert.AreEqual(new Vector2(3, 2), tree.NearestNeighbor(new Vector2(3, 2)));
    Assert.AreEqual(new Vector2(9, 9), tree.NearestNeighbor(new Vector2(9, 9)));
  }

  [Test]
  public void NearestNeighbor_NearDataPoint_ReturnsClosestPoint() {
    var points = new List<Vector2>();
    for (int i = 0; i <= 10; ++i) {
      for (int j = 0; j <= 10; ++j) {
        points.Add(new Vector2(i, j));
      }
    }
    var tree = new KDTree<Vector2>(points, (Vector2 point) => point);
    Assert.That(tree.NearestNeighbor(Vector2.zero),
                Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));
    Assert.That(tree.NearestNeighbor(new Vector2(1.4f, 1.2f)),
                Is.EqualTo(new Vector2(1, 1)).Using(Vector2EqualityComparer.Instance));
    Assert.That(tree.NearestNeighbor(new Vector2(2.7f, 2.2f)),
                Is.EqualTo(new Vector2(3, 2)).Using(Vector2EqualityComparer.Instance));
    Assert.That(tree.NearestNeighbor(new Vector2(9.05f, 8.61f)),
                Is.EqualTo(new Vector2(9, 9)).Using(Vector2EqualityComparer.Instance));
  }
}
