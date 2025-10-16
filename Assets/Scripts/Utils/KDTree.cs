using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// K-D tree node.
public class KDNode<T> {
  public T Data { get; set; }
  public KDNode<T> Left { get; set; }
  public KDNode<T> Right { get; set; }
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
    var neighbor = NearestNeighbor(_root, target, depth: 0, bestNode: null);
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

    var nodeCoordinates = _getCoordinates(node.Data);
    var currentDistance = Vector2.Distance(nodeCoordinates, target);
    var bestDistance = bestNode == null ? float.MaxValue
                                        : Vector2.Distance(_getCoordinates(bestNode.Data), target);
    if (currentDistance < bestDistance) {
      bestNode = node;
      bestDistance = currentDistance;
    }

    var axis = depth % 2;
    var targetCoordinatesValue = axis == 0 ? target.x : target.y;
    var nodeCoordinatesValue = axis == 0 ? nodeCoordinates.x : nodeCoordinates.y;
    var nextBranch = targetCoordinatesValue < nodeCoordinatesValue ? node.Left : node.Right;
    var otherBranch = targetCoordinatesValue < nodeCoordinatesValue ? node.Right : node.Right;

    // Explore the next branch first.
    bestNode = NearestNeighbor(nextBranch, target, depth + 1, bestNode);

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

    var k = 2;
    var axis = depth % k;

    // Sort the points by axis and find the median.
    var sortedPoints = points.Select(p => new { Point = p, Coordinates = _getCoordinates(p) })
                           .OrderBy(p => axis == 0 ? p.Coordinates.x : p.Coordinates.y)
                           .Select(p => p.Point)
                           .ToList();
    var medianIndex = sortedPoints.Count / 2;
    var medianPoint = sortedPoints[medianIndex];

    var node = new KDNode<T> { Data = medianPoint };
    var leftPoints = sortedPoints.GetRange(0, medianIndex);
    var rightPoints = sortedPoints.GetRange(medianIndex + 1, sortedPoints.Count - medianIndex - 1);

    node.Left = BuildTree(leftPoints, depth + 1);
    node.Right = BuildTree(rightPoints, depth + 1);
    return node;
  }
}
