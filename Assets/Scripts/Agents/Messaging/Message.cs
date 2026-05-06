using System;

// Message is a base class for message sending. It always carries Sender, Receiver, Type, and
// Payload.

// Message types based on inter-agent communication contents.
public enum MessageType {
  AssignSubInterceptorRequest,
  AssignTarget,
  ReassignTargetRequest,
}

// Message is a base envelope for message sending. It always carries Sender, Receiver, Type, and
// Payload. This file wraps the payload data with transport metadata. It intentionally layers and
// splits the payload and the transportation data. Mailbox only focuses on transportation (enqueue,
// latency, and delivery timing).

public abstract class Message {
  public IAgent Sender { get; }
  public IAgent Receiver { get; }
  public MessageType Type { get; }

  public abstract IMessagePayload Payload { get; }

  protected Message(IAgent sender, IAgent receiver, MessageType type) {
    Sender = sender ?? throw new ArgumentNullException(nameof(sender));
    Receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
    Type = type;
  }
}

// Generic message class (envelope) that stores a payload while exposing the common IMessagePayload
// interface through the base Message API.
public abstract class Message<TPayload> : Message
    where TPayload : class, IMessagePayload {
  public TPayload PayloadData { get; }
  public sealed override IMessagePayload Payload => PayloadData;
  protected Message(IAgent sender, IAgent receiver, MessageType type, TPayload payload)
      : base(sender, receiver, type) {
    PayloadData = payload ?? throw new ArgumentNullException(nameof(payload));
  }
}

public sealed class AssignSubInterceptorRequestMessage
    : Message<AssignSubInterceptorRequestPayload> {
  public AssignSubInterceptorRequestMessage(IAgent sender, IAgent receiver,
                                            IInterceptor subInterceptor)
      : base(sender, receiver, MessageType.AssignSubInterceptorRequest,
             new AssignSubInterceptorRequestPayload(subInterceptor)) {}
}

public sealed class AssignTargetMessage : Message<AssignTargetPayload> {
  public AssignTargetMessage(IAgent sender, IAgent receiver, IHierarchical target)
      : base(sender, receiver, MessageType.AssignTarget, new AssignTargetPayload(target)) {}
}

public sealed class ReassignTargetRequestMessage : Message<ReassignTargetRequestPayload> {
  public ReassignTargetRequestMessage(IAgent sender, IAgent receiver, IHierarchical target)
      : base(sender, receiver, MessageType.ReassignTargetRequest,
             new ReassignTargetRequestPayload(target)) {}
}
