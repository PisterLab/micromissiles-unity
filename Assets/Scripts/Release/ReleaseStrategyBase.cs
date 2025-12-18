using System.Collections.Generic;

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

    List<IHierarchical> leafHierarchicals =
        Agent.HierarchicalAgent.LeafHierarchicals(activeOnly: false, withTargetOnly: true);
    return Release(leafHierarchicals);
  }

  // Release the sub-interceptors for the given leaf hierarchical objects.
  protected abstract List<IAgent> Release(IEnumerable<IHierarchical> hierarchicals);
}
