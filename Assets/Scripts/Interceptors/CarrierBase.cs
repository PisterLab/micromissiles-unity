using System.Collections.Generic;
using UnityEngine;

// Base implementation of a carrier.
//
// A carrier carries other interceptors, such as a launcher or a carrier interceptor.
public abstract class CarrierBase : InterceptorBase {
  [SerializeField]
  private int _numReleasedSubInterceptors = 0;

  // Release strategy for sub-interceptors.
  public IReleaseStrategy ReleaseStrategy { get; set; }

  protected override void FixedUpdate() {
    base.FixedUpdate();

    // Determine whether to release the sub-interceptors.
    if (ReleaseStrategy != null) {
      List<IAgent> releasedAgents = ReleaseStrategy.Release();
      _numReleasedSubInterceptors += releasedAgents.Count;
    }
  }
}
