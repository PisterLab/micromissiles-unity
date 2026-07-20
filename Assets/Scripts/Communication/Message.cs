// The message is a base class for the different types of messages being sent and received among the
// agents. It always carries a sender, a receiver, a message type, and a payload.

using System;

// Message type enumeration that defines the message payload.
public enum MessageType {
  AssignTargetRequest,
  AssignTargetResponse,
  ReassignTargetRequest,
}

public abstract class Message {
  public CommsNode Sender { get; }
  public CommsNode Receiver { get; }
  public MessageType Type { get; }

  public abstract IMessagePayload Payload { get; }

  protected Message(CommsNode sender, CommsNode receiver, MessageType type) {
    Sender = sender ?? throw new ArgumentNullException(nameof(sender));
    Receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
    Type = type;
  }
}

// Generic message that stores a payload. The generic type refers to the payloa type.
public abstract class Message<TPayload> : Message
    where TPayload : class, IMessagePayload {
  public TPayload PayloadData { get; }
  public sealed override IMessagePayload Payload => PayloadData;
  protected Message(CommsNode sender, CommsNode receiver, MessageType type, TPayload payload)
      : base(sender, receiver, type) {
    PayloadData = payload ?? throw new ArgumentNullException(nameof(payload));
  }
}

// This message is sent upwards to a parent interceptor or IADS when a sub-interceptor has no target
// and is requesting a new target.
public sealed class AssignTargetRequestMessage : Message<AssignTargetRequestPayload> {
  public AssignTargetRequestMessage(CommsNode sender, CommsNode receiver,
                                    IInterceptor subInterceptor)
      : base(sender, receiver, MessageType.AssignTargetRequest,
             new AssignTargetRequestPayload(subInterceptor)) {}
}

// This message is sent downwards from the IADS or a parent interceptor to inform the
// sub-interceptor of a new target.
public sealed class AssignTargetResponseMessage : Message<AssignTargetResponsePayload> {
  public AssignTargetResponseMessage(CommsNode sender, CommsNode receiver, IHierarchical target)
      : base(sender, receiver, MessageType.AssignTargetResponse,
             new AssignTargetResponsePayload(target)) {}
}

// This message is sent upwards to a parent interceptor or IADS when a sub-interceptor can no longer
// pursue the current target and is requesting the parent interceptor or IADS to reassign that
// target elsewhere.
public sealed class ReassignTargetRequestMessage : Message<ReassignTargetRequestPayload> {
  public ReassignTargetRequestMessage(CommsNode sender, CommsNode receiver, IHierarchical target)
      : base(sender, receiver, MessageType.ReassignTargetRequest,
             new ReassignTargetRequestPayload(target)) {}
}
