using System;
using UnityEngine;

// The mailbox handles the communication latencies for agent-to-agent communication. The mailbox
// owns the message queue and releases messages to be delivered after a set latency.
public class Mailbox {
  // Message queue.
  private readonly PriorityQueue<PendingMessage> _messageQueue =
      new PriorityQueue<PendingMessage>();

  // Enqueue a message to be delivered after a set latency.
  public void Send(Message message) {
    if (message == null) {
      return;
    }

    if (CommsManager.Instance.ContainsNode(message.Receiver)) {
      Configs.LinkConfig config =
          CommsManager.Instance.GetLinkConfig(message.Sender, message.Receiver);

      float packetDeliveryRatio = Mathf.Clamp01(config.PacketDeliveryRatio);
      if (packetDeliveryRatio < 1f && UnityEngine.Random.value >= packetDeliveryRatio) {
        return;
      }

      float latency = config.LatencySeconds;
      float jitter = Utilities.SampleStandardNormal() * config.LatencyStdSeconds;
      float totalLatency = Math.Max(0f, latency + jitter);
      float deliverAt = SimManager.Instance.ElapsedTime + totalLatency;

      // Deliver the message immediately if it is already due.
      if (deliverAt <= SimManager.Instance.ElapsedTime) {
        if (CommsManager.Instance.ContainsNode(message.Receiver)) {
          message.Receiver.Receive(message);
        }
      } else {
        var pendingMessage = new PendingMessage(message, deliverAt);
        _messageQueue.Enqueue(pendingMessage, pendingMessage.DeliverAt);
      }
    }
  }

  // Deliver should be called at every fixed update to check for due messages to be delivered.
  public void Deliver() {
    while (!_messageQueue.IsEmpty() &&
           _messageQueue.Peek().DeliverAt <= SimManager.Instance.ElapsedTime) {
      PendingMessage pendingMessage = _messageQueue.Dequeue();
      if (CommsManager.Instance.ContainsNode(pendingMessage.Receiver)) {
        pendingMessage.Receiver.Receive(pendingMessage.Message);
      }
    }
  }

  // Clear all pending messages.
  public void Clear() {
    _messageQueue.Clear();
  }
}
