using System;

// This is the mailbox's internal queue item. It stores the Message object 
// and DeliverAt into the priority queue and pops the message when DeliverAt
// time has reached.

public readonly struct PendingMessage : IComparable<PendingMessage> {
    public float DeliverAt { get; }
    public Message Message { get; }

    public IAgent Sender => Message?.Sender;
    public IAgent Receiver => Message?.Receiver;

    public PendingMessage(float deliverAt, Message message) {
        DeliverAt = deliverAt;
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }

    public int CompareTo(PendingMessage other) => DeliverAt.CompareTo(other.DeliverAt);
}
