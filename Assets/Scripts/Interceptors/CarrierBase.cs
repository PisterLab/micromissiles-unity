using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Base implementation of a carrier.
//
// A carrier carries other interceptors, such as a launcher or a carrier interceptor.
public abstract class CarrierBase : InterceptorBase {
  // Time between checking whether to release sub-interceptors.
  private const float _releasePeriod = 0.2f;

  // Coroutine for releasing sub-interceptors.
  private Coroutine _releaseCoroutine;

  public int NumSubInterceptorsRemaining { get; private set; } = 0;

  // Release strategy for sub-interceptors.
  public IReleaseStrategy ReleaseStrategy { get; set; }

  // This callback function should be called when a sub-interceptor successfully hits its target.
  public void RegisterSubInterceptorHit(IInterceptor interceptor) {
    // Re-assign the other pursuers of the target to other threats.
    var pursuers = interceptor.HierarchicalAgent.Target.Pursuers;
    // Use a maximum speed assignment.
    IAssignment targetAssignment =
        new MaxSpeedAssignment(Assignment.Assignment_EvenAssignment_Assign);
    var activeTargets = HierarchicalAgent.Target.ActiveSubHierarchicals.ToList();
    List<AssignmentItem> assignments = targetAssignment.Assign(pursuers, activeTargets);
    foreach (var assignment in assignments) {
      assignment.First.Target = assignment.Second;
    }
  }

  // This callback function should be called when a sub-interceptor misses its target.
  public void RegisterSubInterceptorMiss(IInterceptor interceptor) {
    // TODO(titan): The carrier interceptor should either re-assign an interceptor to the missed
    // threat or launch another interceptor at the threat. If there are no sub-interceptors
    // remaining, it should report the miss up the hierarchy for re-clustering.
  }

  protected override void Start() {
    base.Start();
    _releaseCoroutine = StartCoroutine(ReleaseManager(_releasePeriod));
  }

  protected override void OnDestroy() {
    base.OnDestroy();

    if (_releaseCoroutine != null) {
      StopCoroutine(_releaseCoroutine);
    }
  }

  protected override void UpdateAgentConfig() {
    base.UpdateAgentConfig();
    NumSubInterceptorsRemaining = (int)AgentConfig.SubAgentConfig.NumSubAgents;
  }

  private IEnumerator ReleaseManager(float period) {
    while (true) {
      // Determine whether to release the sub-interceptors.
      if (ReleaseStrategy != null) {
        List<IAgent> releasedAgents = ReleaseStrategy.Release();
        NumSubInterceptorsRemaining -= releasedAgents.Count;
      }
      yield return new WaitForSeconds(period);
    }
  }
}
