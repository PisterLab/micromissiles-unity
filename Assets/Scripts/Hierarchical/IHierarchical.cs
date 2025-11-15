using System.Collections.Generic;
using UnityEngine;

// Interface for a hierarchical object.
//
// A hierarchical object can represent a single entity, such as an interceptor or a threat, or a
// collection of entities, such as a salvo of interceptors or a swarm of threats.
//
// Hierarchical objects can be assigned to one another and are used by all hierarchical algorithms,
// including clustering, prediction, and assignment algorithms.
public interface IHierarchical {
  IReadOnlyList<IHierarchical> SubHierarchicals { get; }
  // Return a list of active sub-hierarchical objects.
  IEnumerable<IHierarchical> ActiveSubHierarchicals { get; }
  IHierarchical Target { get; set; }
  IReadOnlyList<IHierarchical> Pursuers { get; }
  IReadOnlyList<IHierarchical> LaunchedHierarchicals { get; }

  Vector3 Position { get; }
  Vector3 Velocity { get; }
  float Speed { get; }
  Vector3 Acceleration { get; }
  bool IsTerminated { get; }

  void AddSubHierarchical(IHierarchical subHierarchical);
  void RemoveSubHierarchical(IHierarchical subHierarchical);
  void ClearSubHierarchicals();

  void AddPursuer(IHierarchical pursuer);
  void RemovePursuer(IHierarchical pursuer);

  // Add a launched hierarchical object to keep track of whether an interceptor has been launched to
  // pursue the hierarchical object's target.
  void AddLaunchedHierarchical(IHierarchical hierarchical);

  // Remove the target hierarchical object from the hierarchy.
  void RemoveTargetHierarchical(IHierarchical target);

  // Recursively cluster the targets.
  void RecursiveCluster(int maxClusterSize);

  // Assign a new target to the given hierarchical object. Return whether a new target was
  // successfully assigned to the hierarchical object.
  bool AssignNewTarget(IHierarchical hierarchical, int capacity);

  // Re-assign the given target to another hierarchical object. Return whether the target was
  // successfully assigned to another hierarchical object.
  bool ReassignTarget(IHierarchical target);
}
