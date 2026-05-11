// The pending message is the mailbox's internal queue item. It stores the message object and the
// scheduled delivery time in simulation seconds. The mailbox dequeues this item once the simulation
// time reaches the deliver at time.

using System;

public readonly struct PendingMessage : IComparable<PendingMessage> {
  public Message Message { get; }

  // Absolute simulation time in seconds when the mailbox should deliver this message.
  public float DeliverAt { get; }

  public IAgent Sender => Message?.Sender;
  public IAgent Receiver => Message?.Receiver;

  public PendingMessage(Message message, float deliverAt) {
    if (float.IsNaN(deliverAt) || float.IsInfinity(deliverAt) || deliverAt < 0f) {
      throw new ArgumentOutOfRangeException(nameof(deliverAt), deliverAt,
                                            "DeliverAt must be finite and non-negative.");
    }
    Message = message ?? throw new ArgumentNullException(nameof(message));
    DeliverAt = deliverAt;
  }

  public int CompareTo(PendingMessage other) => DeliverAt.CompareTo(other.DeliverAt);
}
