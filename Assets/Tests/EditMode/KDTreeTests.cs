using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class KDTreeTests {
  [Test]
  public void NearestNeighbor_EmptyTree_ReturnsZero() {
    var tree = new KDTree<Vector2>(new List<Vector2>(), (Vector2 point) => point);
    Assert.AreEqual(tree.NearestNeighbor(Vector2.zero), Vector2.zero);
    Assert.AreEqual(tree.NearestNeighbor(new Vector2(1, 1)), Vector2.zero);
  }

  [Test]
  public void NearestNeighbor_SinglePoint_ReturnsSinglePoint() {
    var tree =
        new KDTree<Vector2>(new List<Vector2> { new Vector2(1, 1) }, (Vector2 point) => point);
    Assert.AreEqual(tree.NearestNeighbor(Vector2.zero), new Vector2(1, 1));
    Assert.AreEqual(tree.NearestNeighbor(new Vector2(1, 6)), new Vector2(1, 1));
    Assert.AreEqual(tree.NearestNeighbor(new Vector2(3, 2)), new Vector2(1, 1));
    Assert.AreEqual(tree.NearestNeighbor(new Vector2(9, 9)), new Vector2(1, 1));
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
    Assert.AreEqual(tree.NearestNeighbor(Vector2.zero), Vector2.zero);
    Assert.AreEqual(tree.NearestNeighbor(new Vector2(1, 6)), new Vector2(1, 6));
    Assert.AreEqual(tree.NearestNeighbor(new Vector2(3, 2)), new Vector2(3, 2));
    Assert.AreEqual(tree.NearestNeighbor(new Vector2(9, 9)), new Vector2(9, 9));
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
    Assert.AreEqual(tree.NearestNeighbor(Vector2.zero), Vector2.zero);
    Assert.AreEqual(tree.NearestNeighbor(new Vector2(1.4f, 1.2f)), new Vector2(1, 1));
    Assert.AreEqual(tree.NearestNeighbor(new Vector2(2.7f, 2.2f)), new Vector2(3, 2));
    Assert.AreEqual(tree.NearestNeighbor(new Vector2(9.05f, 8.61f)), new Vector2(9, 9));
  }
}
