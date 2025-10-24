using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// K-D tree node.
public class KDNode<T> {
  public T Data { get; internal set; }
  public KDNode<T> Left { get; internal set; }
  public KDNode<T> Right { get; internal set; }
}

// K-D tree with 2D coordinates.
public class KDTree<T> {
  // Root node.
  private KDNode<T> _root;

  // Function to get the coordinates from the tree elements.
  private Func<T, Vector2> _getCoordinates;

  public KDTree(IReadOnlyList<T> points, Func<T, Vector2> getCoordinates) {
    _getCoordinates = getCoordinates;
    _root = BuildTree(points, depth: 0);
  }

  // Find the nearest neighbor to the target node.
  public T NearestNeighbor(in Vector2 target) {
    KDNode<T> neighbor = NearestNeighbor(_root, target, depth: 0, bestNode: null);
    if (neighbor == null) {
      return default(T);
    }
    return neighbor.Data;
  }

  // Find the nearest neighbor to the target node.
  private KDNode<T> NearestNeighbor(KDNode<T> node, in Vector2 target, int depth,
                                    KDNode<T> bestNode) {
    if (node == null) {
      return bestNode;
    }

    Vector2 nodeCoordinates = _getCoordinates(node.Data);
    float currentDistance = Vector2.Distance(nodeCoordinates, target);
    float bestDistance = bestNode == null
                             ? float.MaxValue
                             : Vector2.Distance(_getCoordinates(bestNode.Data), target);
    if (currentDistance < bestDistance) {
      bestNode = node;
      bestDistance = currentDistance;
    }

    int axis = depth % 2;
    float targetCoordinatesValue = axis == 0 ? target.x : target.y;
    float nodeCoordinatesValue = axis == 0 ? nodeCoordinates.x : nodeCoordinates.y;
    KDNode<T> nextBranch = targetCoordinatesValue < nodeCoordinatesValue ? node.Left : node.Right;
    KDNode<T> otherBranch = targetCoordinatesValue < nodeCoordinatesValue ? node.Right : node.Left;

    // Explore the next branch first.
    bestNode = NearestNeighbor(nextBranch, target, depth + 1, bestNode);
    if (bestNode != null) {
      bestDistance = Vector2.Distance(_getCoordinates(bestNode.Data), target);
      ;
    }

    // Explore the other branch.
    if (Mathf.Abs(targetCoordinatesValue - nodeCoordinatesValue) < bestDistance) {
      bestNode = NearestNeighbor(otherBranch, target, depth + 1, bestNode);
    }
    return bestNode;
  }

  // Construct a tree from the list of points.
  private KDNode<T> BuildTree(IReadOnlyList<T> points, int depth) {
    if (points.Count == 0) {
      return null;
    }

    int k = 2;
    int axis = depth % k;

    // Sort the points by axis and find the median.
    List<T> sortedPoints =
        points.OrderBy(point => (axis == 0 ? _getCoordinates(point).x : _getCoordinates(point).y))
            .ToList();
    int medianIndex = sortedPoints.Count / 2;
    T medianPoint = sortedPoints[medianIndex];

    var node = new KDNode<T> { Data = medianPoint };
    List<T> leftPoints = sortedPoints.GetRange(0, medianIndex);
    List<T> rightPoints =
        sortedPoints.GetRange(medianIndex + 1, sortedPoints.Count - medianIndex - 1);

    node.Left = BuildTree(leftPoints, depth + 1);
    node.Right = BuildTree(rightPoints, depth + 1);
    return node;
  }
}
