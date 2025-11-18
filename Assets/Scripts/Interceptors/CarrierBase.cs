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

  // Release strategy for sub-interceptors.
  public IReleaseStrategy ReleaseStrategy { get; set; }

  protected override void Awake() {
    base.Awake();
    EscapeDetector = new GeometricEscapeDetector(this);
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

  private IEnumerator ReleaseManager(float period) {
    while (true) {
      // Determine whether to release the sub-interceptors.
      if (ReleaseStrategy != null) {
        List<IAgent> releasedAgents = ReleaseStrategy.Release();
        NumSubInterceptorsRemaining -= releasedAgents.Count;

        foreach (var agent in releasedAgents) {
          if (agent is IInterceptor subInterceptor) {
            subInterceptor.OnAssignSubInterceptor += AssignSubInterceptor;
            subInterceptor.OnReassignTarget += ReassignTarget;
            if (subInterceptor.Movement is MissileMovement movement) {
              movement.FlightPhase = Simulation.FlightPhase.Boost;
            }
          }
        }
      }

      yield return new WaitForSeconds(period);
    }
  }
}
