using System;

// MessagePayload defines different types of new payload objects. Payload is carried by
// Message envelopes. Concrete payload content lives here and is only read explicitly
// by receivers.

// Interface for MessagePayload.
public interface IMessagePayload {}

public sealed class AssignSubInterceptorRequestPayload : IMessagePayload {
  public IInterceptor SubInterceptor { get; }

  public AssignSubInterceptorRequestPayload(IInterceptor subInterceptor) {
    SubInterceptor = subInterceptor ?? throw new ArgumentNullException(nameof(subInterceptor));
  }
}

public sealed class AssignTargetPayload : IMessagePayload {
  public IHierarchical Target { get; }

  public AssignTargetPayload(IHierarchical target) {
    Target = target ?? throw new ArgumentNullException(nameof(target));
  }
}

public sealed class ReassignTargetRequestPayload : IMessagePayload {
  public IHierarchical Target { get; }

  public ReassignTargetRequestPayload(IHierarchical target) {
    Target = target ?? throw new ArgumentNullException(nameof(target));
  }
}
