using System;

// Message is a base envolope for message sending. It always carries Sender, Receiver, Type, and Payload.

// Types of Message types based on inter-agent communication contents.
public enum MessageType {
  AssignSubInterceptorRequest,
  AssignTarget,
  ReassignTargetRequest,
}

// Message is a base envelope for message sending. It always carries Sender, Receiver, Type, and Payload.
// This file wraps the payload data with transport metadata. It intentionally layers and splits the
// payload and the transportation data. Mailbox only focuses on transportation (enqueue, latency,
// and delivery timing).

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

public sealed class AssignSubInterceptorRequestMessage : Message {
  public AssignSubInterceptorRequestPayload PayloadData { get; }
  public override IMessagePayload Payload => PayloadData;

  public AssignSubInterceptorRequestMessage(IAgent sender, IAgent receiver, IInterceptor subInterceptor) : base(sender, receiver, MessageType.AssignSubInterceptorRequest) {
    PayloadData = new AssignSubInterceptorRequestPayload(subInterceptor);
  }
}

public sealed class AssignTargetMessage : Message {
  public AssignTargetPayload PayloadData { get; }
  public override IMessagePayload Payload => PayloadData;

  public AssignTargetMessage(IAgent sender, IAgent receiver, IHierarchical target) : base(sender, receiver, MessageType.AssignTarget) {
    PayloadData = new AssignTargetPayload(target);
  }
}

public sealed class ReassignTargetRequestMessage : Message {
  public ReassignTargetRequestPayload PayloadData { get; }
  public override IMessagePayload Payload => PayloadData;

  public ReassignTargetRequestMessage(IAgent sender, IAgent receiver, IHierarchical target) : base(sender, receiver, MessageType.ReassignTargetRequest) {
    PayloadData = new ReassignTargetRequestPayload(target);
  }
}
