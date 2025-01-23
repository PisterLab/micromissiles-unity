using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;

public class KDTreeTest {
  [Test]
  public void TestEmpty() {
    KDTree<Vector2> tree = new KDTree<Vector2>(new List<Vector2>(), (Vector2 point) => point);
    Assert.AreEqual(tree.NearestNeighbor(Vector2.zero), Vector2.zero);
    Assert.AreEqual(tree.NearestNeighbor(new Vector2(1, 1)), Vector2.zero);
  }

  [Test]
  public void TestSingle() {
    KDTree<Vector2> tree =
        new KDTree<Vector2>(new List<Vector2> { new Vector2(1, 1) }, (Vector2 point) => point);
    Assert.AreEqual(tree.NearestNeighbor(Vector2.zero), new Vector2(1, 1));
    Assert.AreEqual(tree.NearestNeighbor(new Vector2(1, 6)), new Vector2(1, 1));
    Assert.AreEqual(tree.NearestNeighbor(new Vector2(3, 2)), new Vector2(1, 1));
    Assert.AreEqual(tree.NearestNeighbor(new Vector2(9, 9)), new Vector2(1, 1));
  }

  [Test]
  public void TestNearestNeighborWithinPoints() {
    List<Vector2> points = new List<Vector2>();
    for (int i = 0; i <= 10; ++i) {
      for (int j = 0; j <= 10; ++j) {
        points.Add(new Vector2(i, j));
      }
    }
    KDTree<Vector2> tree = new KDTree<Vector2>(points, (Vector2 point) => point);
    Assert.AreEqual(tree.NearestNeighbor(Vector2.zero), Vector2.zero);
    Assert.AreEqual(tree.NearestNeighbor(new Vector2(1, 6)), new Vector2(1, 6));
    Assert.AreEqual(tree.NearestNeighbor(new Vector2(3, 2)), new Vector2(3, 2));
    Assert.AreEqual(tree.NearestNeighbor(new Vector2(9, 9)), new Vector2(9, 9));
  }

  [Test]
  public void TestNearestNeighborAroundPoints() {
    List<Vector2> points = new List<Vector2>();
    for (int i = 0; i <= 10; ++i) {
      for (int j = 0; j <= 10; ++j) {
        points.Add(new Vector2(i, j));
      }
    }
    KDTree<Vector2> tree = new KDTree<Vector2>(points, (Vector2 point) => point);
    Assert.AreEqual(tree.NearestNeighbor(Vector2.zero), Vector2.zero);
    Assert.AreEqual(tree.NearestNeighbor(new Vector2(1.4f, 1.2f)), new Vector2(1, 1));
    Assert.AreEqual(tree.NearestNeighbor(new Vector2(2.7f, 2.2f)), new Vector2(3, 2));
    Assert.AreEqual(tree.NearestNeighbor(new Vector2(9.05f, 8.61f)), new Vector2(9, 9));
  }
}
