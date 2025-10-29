using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Base implementation of a release strategy.
public abstract class ReleaseStrategyBase : IReleaseStrategy {
  public IAgent Agent { get; init; }

  public ReleaseStrategyBase(IAgent agent) {
    Agent = agent;
  }

  // Release the sub-interceptors.
  public List<IAgent> Release() {
    if (Agent is not CarrierBase carrier || carrier.NumSubInterceptorsRemaining <= 0) {
      return new List<IAgent>();
    }

    List<IHierarchical> FindLeafHierarchicals(IHierarchical hierarchical) {
      // Traverse the agent hierarchy to find only the leaf hierarchical objects.
      if (hierarchical.SubHierarchicals.Count > 0) {
        var leafHierarchicals = new List<IHierarchical>();
        foreach (var subHierarchical in hierarchical.SubHierarchicals) {
          leafHierarchicals.AddRange(FindLeafHierarchicals(subHierarchical));
        }
        return leafHierarchicals;
      }

      // Check if a target exists for the hierarchical object.
      IHierarchical target = hierarchical.Target;
      if (hierarchical.Target == null) {
        return new List<IHierarchical>();
      }
      return new List<IHierarchical> { hierarchical };
    }
    List<IHierarchical> leafHierarchicals = FindLeafHierarchicals(Agent.HierarchicalAgent);
    return Release(leafHierarchicals);
  }

  // Release the sub-interceptors for the given leaf hierarchical objects.
  protected abstract List<IAgent> Release(IEnumerable<IHierarchical> hierarchicals);
}
