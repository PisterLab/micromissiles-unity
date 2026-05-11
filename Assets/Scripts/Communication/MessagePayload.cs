// MessagePayload defines different types of payloads carried by messages.

using System;

// The payload carries the sub-interceptor that is asking a parent interceptor or IADS for a new
// target assignment.
public sealed class AssignTargetRequestPayload : IMessagePayload {
  public IInterceptor SubInterceptor { get; }

  public AssignTargetRequestPayload(IInterceptor subInterceptor) {
    SubInterceptor = subInterceptor ?? throw new ArgumentNullException(nameof(subInterceptor));
  }
}

// The payload carries the target that a parent interceptor or IADS has selected for a requesting
// sub-interceptor.
public sealed class AssignTargetResponsePayload : IMessagePayload {
  public IHierarchical Target { get; }

  public AssignTargetResponsePayload(IHierarchical target) {
    Target = target ?? throw new ArgumentNullException(nameof(target));
  }
}

// The payload carries the target that a sub-interceptor wants its parent interceptor or IADS to
// reassign.
public sealed class ReassignTargetRequestPayload : IMessagePayload {
  public IHierarchical Target { get; }

  public ReassignTargetRequestPayload(IHierarchical target) {
    Target = target ?? throw new ArgumentNullException(nameof(target));
  }
}
