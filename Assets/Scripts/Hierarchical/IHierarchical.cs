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
  // Return a list of active sub-hierarchical objects.
  IEnumerable<IHierarchical> SubHierarchicals { get; }
  IHierarchical Target { get; set; }
  IReadOnlyList<IHierarchical> Pursuers { get; }

  Vector3 Position { get; }
  Vector3 Velocity { get; }
  float Speed { get; }
  Vector3 Acceleration { get; }
  bool IsTerminated { get; }

  void AddSubHierarchical(IHierarchical subHierarchical);
  void RemoveSubHierarchical(IHierarchical subHierarchical);

  void AddPursuer(IHierarchical pursuer);
  void RemovePursuer(IHierarchical pursuer);

  bool IsEscapingPursuers();
}
