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
  IHierarchical Target { get; set; }
  IHierarchical TargetModel { get; set; }

  Vector3 Position { get; }
  Vector3 Velocity { get; }
  float Speed { get; }

  void AddSubHierarchical(IHierarchical subHierarchical);
  void RemoveSubHierarchical(IHierarchical subHierarchical);
}
