// MessagePayload defines different types of new payload objects. Payload is carried by
// Message classes. Concrete payload content lives here and is only read explicitly
// by receivers.
using System;

// The payload carries the interceptor that is asking a parent interceptor or IADS for a target
// assignment.
public sealed class AssignSubInterceptorRequestPayload : IMessagePayload {
  public IInterceptor SubInterceptor { get; }

  public AssignSubInterceptorRequestPayload(IInterceptor subInterceptor) {
    SubInterceptor = subInterceptor ?? throw new ArgumentNullException(nameof(subInterceptor));
  }
}

// The payload carries the target that a parent interceptor or IADS has selected for a receiver
// interceptor.
public sealed class AssignTargetPayload : IMessagePayload {
  public IHierarchical Target { get; }

  public AssignTargetPayload(IHierarchical target) {
    Target = target ?? throw new ArgumentNullException(nameof(target));
  }
}

// The payload carries the target that an interceptor wants its parent interceptor or IADS to
// reassign.
public sealed class ReassignTargetRequestPayload : IMessagePayload {
  public IHierarchical Target { get; }

  public ReassignTargetRequestPayload(IHierarchical target) {
    Target = target ?? throw new ArgumentNullException(nameof(target));
  }
}
