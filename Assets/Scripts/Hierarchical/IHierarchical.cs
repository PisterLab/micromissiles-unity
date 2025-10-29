using System.Collections.Generic;
using UnityEngine;

// Interface for a hierarchical object.
//
// A hierarchical object can represent a single entity, such as an interceptor or a threat, or a
// collection of entities, such as a salvo of interceptors or a swarm of threats.
//
// Hierarchical objects can be assigned to one another and are used by all hierarchical algorithms,
// including clustering, prediction, and assignment algorithms.

public delegate void HierarchicalEventHandler(IHierarchical hierarchical, IHierarchical target);

public interface IHierarchical {
  // The OnHit event handler is called by the hierarchical object with the target as the argument
  // when the hierarchical object hits the target.
  event HierarchicalEventHandler OnHit;

  // The OnMiss event handler is called by the hierarchical object with the target as the argument
  // when the hierarchical object decides that it cannot pursue the target. The hierarchical object
  // is unable to re-assign another sub-hierarchical object to the target or launch another
  // sub-hierarchical object, so it reports the miss to the hierarchical object in the hierarchy
  // level above.
  event HierarchicalEventHandler OnMiss;

  IReadOnlyList<IHierarchical> SubHierarchicals { get; }
  // Return a list of active sub-hierarchical objects.
  IEnumerable<IHierarchical> ActiveSubHierarchicals { get; }
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

  // This function is called when the hierarchical object hits its target.
  void HandleHit();

  // This function is called when the hierarchical object misses its target.
  void HandleMiss();

  void RegisterSubHierarchicalHit(IHierarchical subHierarchical, IHierarchical target);
  void RegisterSubHierarchicalMiss(IHierarchical subHierarchical, IHierarchical target);
}
