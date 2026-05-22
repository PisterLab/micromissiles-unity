using System;

// The pending message is the mailbox's internal queue item. It stores the Message object and the
// scheduled delivery time in simulation seconds. The mailbox dequeues this item once simulation
// time reaches DeliverAt.

public readonly struct PendingMessage : IComparable<PendingMessage> {
  // Absolute simulation time in seconds when the mailbox should deliver this message.
  public float DeliverAt { get; }
  public Message Message { get; }

  public IAgent Sender => Message?.Sender;
  public IAgent Receiver => Message?.Receiver;

  public PendingMessage(float deliverAt, Message message) {
    if (float.IsNaN(deliverAt) || float.IsInfinity(deliverAt) || deliverAt < 0f) {
      throw new ArgumentOutOfRangeException(nameof(deliverAt), deliverAt,
                                            "DeliverAt must be finite and non-negative.");
    }
    Message = message ?? throw new ArgumentNullException(nameof(message));
    DeliverAt = deliverAt;
  }

  public int CompareTo(PendingMessage other) => DeliverAt.CompareTo(other.DeliverAt);
}
