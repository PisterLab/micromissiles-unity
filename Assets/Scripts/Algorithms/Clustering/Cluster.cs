using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// The cluster class represents a collection of game objects.
public class Cluster {
  // Coordinates of the cluster.
  private Vector3 _coordinates = Vector3.zero;

  // List of game objects in the cluster.
  private List<GameObject> _objects = new List<GameObject>();

  public Cluster() {}
  public Cluster(in Vector3 coordinates) {
    _coordinates = coordinates;
  }
  public Cluster(in GameObject obj) {
    _coordinates = obj.transform.position;
  }

  // Get the cluster coordinates.
  public Vector3 Coordinates {
    get { return _coordinates; }
  }

  // Get the list of game objects.
  public IReadOnlyList<GameObject> Objects {
    get { return _objects; }
  }

  // Get the list of agents.
  public IReadOnlyList<Agent> Agents {
    get { return _objects.Select(gameObject => gameObject.GetComponent<Agent>()).ToList(); }
  }

  // Get the list of interceptors.
  public IReadOnlyList<Interceptor> Interceptors {
    get {
      return _objects.Select(gameObject => gameObject.GetComponent<Agent>() as Interceptor)
          .ToList();
    }
  }

  // Get the list of threats.
  public IReadOnlyList<Threat> Threats {
    get {
      return _objects.Select(gameObject => gameObject.GetComponent<Agent>() as Threat).ToList();
    }
  }

  // Return the size of the cluster.
  public int Size() {
    return _objects.Count;
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
    return _objects.Max(obj => Vector3.Distance(centroid, obj.transform.position));
  }

  // Calculate the centroid of the cluster.
  // The centroid is the mean position of all active game objects.
  public Vector3 Centroid() {
    Vector3 positionSum = Vector3.zero;
    int activeAgentCount = 0;
    foreach (var agent in Agents) {
      if (agent != null && !agent.IsTerminated()) {
        positionSum += agent.GetPosition();
        ++activeAgentCount;
      }
    }
    return activeAgentCount > 0 ? positionSum / activeAgentCount : Vector3.zero;
  }

  // Recenter the cluster's centroid to be the mean of all game objects' positions in the cluster.
  public void Recenter() {
    _coordinates = Centroid();
  }

  // Calculate the velocity of the cluster.
  // The velocity is the mean velocity of all active game objects.
  public Vector3 Velocity() {
    Vector3 velocitySum = Vector3.zero;
    int activeAgentCount = 0;
    foreach (var agent in Agents) {
      if (agent != null && !agent.IsTerminated()) {
        velocitySum += agent.GetVelocity();
        ++activeAgentCount;
      }
    }
    return activeAgentCount > 0 ? velocitySum / activeAgentCount : Vector3.zero;
  }

  // Add a game object to the cluster.
  // This function does not update the centroid of the cluster.
  public void AddObject(in GameObject obj) {
    _objects.Add(obj);
  }

  // Add multiple game objects to the cluster.
  // This function does not update the centroid of the cluster.
  public void AddObjects(in IReadOnlyList<GameObject> objects) {
    _objects.AddRange(objects);
  }

  // Merge another cluster into this one.
  // This function does not update the centroid of the cluster.
  public void Merge(in Cluster cluster) {
    AddObjects(cluster.Objects);
  }

  // Returns true if all agents in the cluster are terminated.
  public bool IsFullyTerminated() {
    return Agents.All(agent => agent?.IsTerminated() ?? true);
  }
}
