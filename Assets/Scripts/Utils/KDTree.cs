using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// K-D tree node.
public class KDNode<T> {
  public T Data;
  public KDNode<T> Left = null;
  public KDNode<T> Right = null;

  public KDNode(T data) {
    Data = data;
  }
}

// K-D tree with 2D coordinates.
public class KDTree<T> {
  private KDNode<T> _root;
  private Func<T, Vector2> _getCoordinates;

  public KDTree(in IReadOnlyList<T> points, Func<T, Vector2> getCoordinates) {
    _getCoordinates = getCoordinates;
    _root = BuildTree(points, depth: 0);
  }

  private KDNode<T> BuildTree(in IReadOnlyList<T> points, int depth) {
    if (points.Count == 0)
      return null;

    int k = 2;
    int axis = depth % k;

    // Sort the points by axis and find the median.
    List<T> sortedPoints =
        points.OrderBy(point => (axis == 0 ? _getCoordinates(point).x : _getCoordinates(point).y))
            .ToList();
    int medianIndex = sortedPoints.Count / 2;
    T medianPoint = sortedPoints[medianIndex];

    KDNode<T> node = new KDNode<T>(medianPoint);
    List<T> leftPoints = sortedPoints.GetRange(0, medianIndex);
    List<T> rightPoints =
        sortedPoints.GetRange(medianIndex + 1, sortedPoints.Count - medianIndex - 1);

    node.Left = BuildTree(leftPoints, depth + 1);
    node.Right = BuildTree(rightPoints, depth + 1);
    return node;
  }

  public T NearestNeighbor(Vector2 target) {
    KDNode<T> neighbor = NearestNeighbor(_root, target, depth: 0, best: null);
    if (neighbor == null) {
      return default(T);
    }
    return neighbor.Data;
  }

  private KDNode<T> NearestNeighbor(KDNode<T> node, Vector2 target, int depth, KDNode<T> best) {
    if (node == null)
      return best;

    float currentDistance = Vector2.Distance(_getCoordinates(node.Data), target);
    float bestDistance =
        best == null ? float.MaxValue : Vector2.Distance(_getCoordinates(best.Data), target);
    if (currentDistance < bestDistance)
      best = node;

    int axis = depth % 2;
    KDNode<T> nextBranch = (axis == 0 ? target.x < _getCoordinates(node.Data).x
                                      : target.y < _getCoordinates(node.Data).y)
                               ? node.Left
                               : node.Right;
    KDNode<T> otherBranch = (nextBranch == node.Left) ? node.Right : node.Left;

    // Check the next branch.
    best = NearestNeighbor(nextBranch, target, depth + 1, best);

    // Check the other branch.
    if ((axis == 0 && Mathf.Abs(target.x - _getCoordinates(node.Data).x) < bestDistance) ||
        (axis == 1 && Mathf.Abs(target.y - _getCoordinates(node.Data).y) < bestDistance)) {
      best = NearestNeighbor(otherBranch, target, depth + 1, best);
    }

    return best;
  }
}
