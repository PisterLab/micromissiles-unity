// Interface for an interceptor.
//
// Interceptors defend the asset against incoming threats.
public interface IInterceptor {
  // Maximum number of threats that this interceptor can target.
  public int Capacity { get; }
}
