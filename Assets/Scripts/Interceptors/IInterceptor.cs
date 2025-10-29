// Interface for an interceptor.
//
// Interceptors defend the asset against incoming threats.

public delegate void InterceptEventHandler(IInterceptor interceptor);

public interface IInterceptor : IAgent {
  // The OnHit event handler is called when the interceptor successfully intercepts a threat.
  event InterceptEventHandler OnHit;
  // The OnMiss event handler is called when the interceptor is destroyed, e.g., through a
  // collision, prior to intercepting a threat.
  event InterceptEventHandler OnMiss;

  // Maximum number of threats that this interceptor can target.
  public int Capacity { get; }
}
