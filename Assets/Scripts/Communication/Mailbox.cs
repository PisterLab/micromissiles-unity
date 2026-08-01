using System;
using UnityEngine;

// The mailbox handles the communication latencies between agent to agent communication.
//
// Mailbox.cs owns the message queue and releases messages to deliver after the set latency in
// communication_config.
public class Mailbox {
  // OnMessageReceived is for logging purposes.
  public event Action<Message> OnMessageReceived;

  private static readonly Configs.LinkConfig _fallbackLinkConfig = new Configs.LinkConfig {
    PacketDeliveryRatio = 1f,
  };

  private readonly PriorityQueue<PendingMessage> _messageQueue =
      new PriorityQueue<PendingMessage>();

  // SendMessage checks and enqueues a message to be sent to the receiver. The message will be
  // delivered after the latency has been applied.
  public void SendMessage(Message message) {
    if (message == null) {
      return;
    }
    CommsManager commsManager = CommsManager.Instance;
    // Guard, Sender and Receiver must be valid.
    if (commsManager == null || !commsManager.ContainsNode(message.Receiver)) {
      return;
    }
    EnqueueMessage(message);
  }

  // EnqueueMessage adds a message to the message queue with the appropriate latency applied.
  private void EnqueueMessage(Message message) {
    Configs.LinkConfig config = GetLinkConfig(message);

    // TODO (Joseph): This packet delivery ratio is temporary set to 1 until the fundamental
    // firing/communication logic is changed.
    // float packetDeliveryRatio = config.PacketDeliveryRatio;
    float packetDeliveryRatio = 1f;  // Delete this when firing logic is changed.
    float packetDeliveryRatio = Mathf.Clamp01(packetDeliveryRatio);
    if (UnityEngine.Random.value >= packetDeliveryRatio) {
      return;
    }

    float latencySeconds = config.LatencySeconds;
    float latencyStdSeconds = config.LatencyStdSeconds;
    float jitter =
        latencyStdSeconds > 0f ? Utilities.SampleStandardNormal() * latencyStdSeconds : 0f;
    float totalLatency = Math.Max(0f, latencySeconds + jitter);
    float deliverAt = SimManager.Instance.ElapsedTime + totalLatency;

    // pendingMessage is a wrapper that includes the message and deliverAt.
    var pendingMessage = new PendingMessage(message, deliverAt);
    _messageQueue.Enqueue(pendingMessage, pendingMessage.DeliverAt);
  }

  private Configs.LinkConfig GetLinkConfig(Message message) {
    Configs.CommunicationConfig communicationConfig =
        SimManager.Instance.SimulationConfig?.CommunicationConfig;
    if (communicationConfig == null) {
      return _fallbackLinkConfig;
    }
    Configs.AgentType senderType = message.Sender.EndpointType;
    Configs.AgentType receiverType = message.Receiver.EndpointType;
    foreach (Configs.LinkOverride linkOverride in communicationConfig.LinkOverrides) {
      if (linkOverride.From == senderType && linkOverride.To == receiverType &&
          linkOverride.LinkConfig != null) {
        return linkOverride.LinkConfig;
      }
    }
    return communicationConfig.LinkConfig ?? _fallbackLinkConfig;
  }

  // UpdateMailbox is called at every FixedUpdate to check for due messages. Due messages are then
  // delivered to the receiver's CommsNode.
  public void UpdateMailbox() {
    CommsManager commsManager = CommsManager.Instance;
    SimManager simManager = SimManager.Instance;
    if (commsManager == null || simManager == null) {
      return;
    }

    while (!_messageQueue.IsEmpty() && _messageQueue.Peek().DeliverAt <= simManager.ElapsedTime) {
      var pendingMessage = _messageQueue.Dequeue();
      // Guard, Sender and Receiver must be valid.
      if (!commsManager.ContainsNode(pendingMessage.Receiver)) {
        continue;
      }
      pendingMessage.Receiver.Receive(pendingMessage.Message);
    }
  }
}
