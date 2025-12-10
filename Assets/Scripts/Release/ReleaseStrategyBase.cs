using System.Collections.Generic;

// Base implementation of a release strategy.
public abstract class ReleaseStrategyBase : IReleaseStrategy {
  public IAgent Agent { get; init; }

  public ReleaseStrategyBase(IAgent agent) {
    Agent = agent;
  }

  // Release the sub-interceptors.
  public abstract List<IAgent> Release();

  // Plan the release for the given target.
  protected abstract LaunchPlan PlanRelease(IHierarchical target);
}
