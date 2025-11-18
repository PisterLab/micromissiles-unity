using System.Collections.Generic;

// Interface for a release strategy.
//
// Each carrier defines a release strategy to determine whether and when to release its
// sub-interceptors. The release may be time-based, occur at a specified distance and bearing
// threshold, or rely on the results of a launch planner.
public interface IReleaseStrategy {
  public IAgent Agent { get; init; }

  // Release the sub-interceptors. The returned list of released agents may be empty, in which case
  // no sub-interceptors should be released.
  List<IAgent> Release();
}
