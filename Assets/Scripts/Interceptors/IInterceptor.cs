// Interface for an interceptor.
//
// Interceptors defend the asset against incoming threats.

public delegate void InterceptHitMissEventHandler(IInterceptor interceptor);

public interface IInterceptor : IAgent {
  // The OnHit event handler is called when the interceptor successfully intercepts a threat.
  event InterceptHitMissEventHandler OnHit;
  // The OnMiss event handler is called when the interceptor is destroyed, e.g., through a
  // collision, prior to intercepting a threat.
  event InterceptHitMissEventHandler OnMiss;

  // Maximum number of threats that this interceptor can target.
  int Capacity { get; }

  // Capacity of each sub-interceptor.
  int CapacityPerSubInterceptor { get; }

  // Number of threats that this interceptor can target after having launched some sub-interceptors.
  int CapacityRemaining { get; }

  // Number of sub-interceptors remaining.
  int NumSubInterceptorsRemaining { get; }
}
