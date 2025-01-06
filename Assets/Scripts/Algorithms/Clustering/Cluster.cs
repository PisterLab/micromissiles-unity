using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// The cluster class represents a collection of points with a defined centroid.
public class Cluster {
  // Coordinates of the cluster.
  private Vector3 _coordinates = Vector3.zero;

  // List of points in the cluster.
  private List<Vector3> _points = new List<Vector3>();

  public Cluster() {}
  public Cluster(in Vector3 coordinates) {
    _coordinates = coordinates;
  }

  // Get the cluster coordinates.
  public Vector3 Coordinates {
    get { return _coordinates; }
  }

  // Get the list of points.
  public IReadOnlyList<Vector3> Points {
    get { return _points; }
  }

  // Return the size of the cluster.
  public int Size() {
    return _points.Count;
  }

  // Check whether the cluster is empty.
  public bool IsEmpty() {
    return Size() == 0;
  }

  // Calculate the radius of the cluster.
  public float Radius() {
    if (IsEmpty()) {
      return 0;
    }

    Vector3 centroid = Centroid();
    return _points.Max(point => Vector3.Distance(centroid, point));
  }

  // Calculate the centroid of the cluster.
  public Vector3 Centroid() {
    if (IsEmpty()) {
      return Vector3.zero;
    }

    Vector3 centroid = Vector3.zero;
    foreach (var point in _points) {
      centroid += point;
    }
    centroid /= _points.Count;
    return centroid;
  }

  // Recenter the cluster's centroid to be the mean of all points in the cluster.
  public void Recenter() {
    _coordinates = Centroid();
  }

  // Add a point to the cluster.
  // This function does not update the centroid of the cluster.
  public void AddPoint(in Vector3 point) {
    _points.Add(point);
  }

  // Add multiple points to the cluster.
  // This function does not update the centroid of the cluster.
  public void AddPoints(in IReadOnlyList<Vector3> otherPoints) {
    _points.AddRange(otherPoints);
  }

  // Merge another cluster into this one.
  // This function does not update the centroid of the cluster.
  public void Merge(in Cluster cluster) {
    AddPoints(cluster.Points);
  }
}
