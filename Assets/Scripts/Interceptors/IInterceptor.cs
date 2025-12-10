// Interface for an interceptor.
//
// Interceptors defend the asset against incoming threats.

public delegate void InterceptHitMissEventHandler(IInterceptor interceptor);

public interface IInterceptor : IAgent {
  // Maximum number of threats that this interceptor can target.
  public int Capacity { get; }

  // The OnHit event handler is called when the interceptor successfully intercepts a threat.
  event InterceptHitMissEventHandler OnHit;
  // The OnMiss event handler is called when the interceptor is destroyed, e.g., through a
  // collision, prior to intercepting a threat.
  event InterceptHitMissEventHandler OnMiss;
}
