// Interface for an interceptor.
//
// Interceptors defend the asset against incoming threats.

public delegate void InterceptorEventHandler(IInterceptor interceptor);
public delegate void TargetReassignEventHandler(IHierarchical target);

public interface IInterceptor : IAgent {
  // The OnHit event handler is called when the interceptor successfully intercepts a threat.
  event InterceptorEventHandler OnHit;
  // The OnMiss event handler is called when the interceptor misses its target but is not destroyed.
  event InterceptorEventHandler OnMiss;
  // The OnDestroyed event handler is called when the interceptor is destroyed prior to intercepting
  // a threat, e.g., through a ground collision.
  event InterceptorEventHandler OnDestroyed;

  // The OnAssignSubInterceptor event handler is called when a sub-interceptor has no assigned
  // target and should be assigned one.
  event InterceptorEventHandler OnAssignSubInterceptor;

  // The OnReassignTarget event handler is called when a target needs to be re-assigned to another
  // interceptor.
  event TargetReassignEventHandler OnReassignTarget;

  IEscapeDetector EscapeDetector { get; set; }

  // Maximum number of threats that this interceptor can target.
  int Capacity { get; }

  // Capacity of each sub-interceptor.
  int CapacityPerSubInterceptor { get; }

  // Number of threats that this interceptor can plan to target after having planned to launch some
  // sub-interceptors.
  int CapacityPlannedRemaining { get; }

  // Number of threats that this interceptor can target after having launched some sub-interceptors.
  int CapacityRemaining { get; }

  // Number of sub-interceptors.
  int NumSubInterceptors { get; }

  // Number of sub-interceptors remaining that can be planned to launch.
  int NumSubInterceptorsPlannedRemaining { get; }

  // Number of sub-interceptors remaining.
  int NumSubInterceptorsRemaining { get; }

  // Assign a new target to the sub-interceptor.
  void AssignSubInterceptor(IInterceptor subInterceptor);

  // Re-assign the target to another sub-interceptor.
  void ReassignTarget(IHierarchical target);
}
