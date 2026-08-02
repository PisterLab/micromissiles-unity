using System;
using UnityEngine;

// The mailbox handles the communication latencies for agent-to-agent communication. The mailbox
// owns the message queue and releases messages to be delivered after a set latency.
public class Mailbox {
  private static readonly Configs.LinkConfig _defaultLinkConfig = new Configs.LinkConfig {
    PacketDeliveryRatio = 1f,
  };

  // Message queue.
  private readonly PriorityQueue<PendingMessage> _messageQueue =
      new PriorityQueue<PendingMessage>();

  // Enqueue a message to be delivered after a set latency.
  public void Send(Message message) {
    if (message == null) {
      return;
    }

    if (CommsManager.Instance.ContainsNode(message.Receiver)) {
      Configs.LinkConfig config = GetLinkConfig(message);

      // TODO(Joseph0120): Set the packet delivery ratio to config.PacketDeliveryRatio.
      float packetDeliveryRatio = Mathf.Clamp01(1);
      if (UnityEngine.Random.value >= packetDeliveryRatio) {
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

  private Configs.LinkConfig GetLinkConfig(Message message) {
    Configs.CommunicationConfig communicationConfig =
        SimManager.Instance.SimulationConfig?.CommunicationConfig;
    if (communicationConfig == null) {
      return _defaultLinkConfig;
    }

    Configs.AgentType senderType = message.Sender.EndpointType;
    Configs.AgentType receiverType = message.Receiver.EndpointType;
    foreach (Configs.LinkOverride linkOverride in communicationConfig.LinkOverrides) {
      if (linkOverride.From == senderType && linkOverride.To == receiverType) {
        return linkOverride.LinkConfig;
      }
    }
    return communicationConfig.LinkConfig ?? _defaultLinkConfig;
  }
}
