using System;
using UnityEngine;

public class Mailbox {
  // OnMessageReceived is for logging purposes.
  public event Action<Message> OnMessageReceived;

  private static readonly Configs.LinkConfig _fallbackLinkConfig = new Configs.LinkConfig {
    PacketDeliveryRatio = 1f,
  };

  private readonly PriorityQueue<PendingMessage> _messageQueue =
      new PriorityQueue<PendingMessage>();

  public void SendMessage(Message message) {
    if (message == null) {
      return;
    }
    CommsManager commsManager = CommsManager.Instance;
    // Guard, Sender and Receiver must be valid.
    if (commsManager == null || !commsManager.ContainsNode(message.Sender) ||
        !commsManager.ContainsNode(message.Receiver)) {
      return;
    }
    EnqueueMessage(message);
  }

  private void EnqueueMessage(Message message) {
    Configs.LinkConfig config = GetLinkConfig(message);

    // TODO (Joseph): This packet delivery ratio is a temporary solution. Fundamental
    // firing/communication logic needs to change.
    float packetDeliveryRatio = config.PacketDeliveryRatio;
    if (packetDeliveryRatio <= 0f ||
        (packetDeliveryRatio < 1f && UnityEngine.Random.value >= packetDeliveryRatio)) {
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

  public void UpdateMailbox() {
    CommsManager commsManager = CommsManager.Instance;
    SimManager simManager = SimManager.Instance;
    if (commsManager == null || simManager == null || !simManager.IsRunning) {
      return;
    }

    while (!_messageQueue.IsEmpty() && _messageQueue.Peek().DeliverAt <= simManager.ElapsedTime) {
      var pendingMessage = _messageQueue.Dequeue();
      // Guard, Sender and Receiver must be valid.
      if (!commsManager.ContainsNode(pendingMessage.Sender) ||
          !commsManager.ContainsNode(pendingMessage.Receiver)) {
        continue;
      }
      pendingMessage.Receiver.Receive(pendingMessage.Message);
      // TODO (Joseph): OnMessageReceived for logging.
      OnMessageReceived?.Invoke(pendingMessage.Message);
    }
  }

  public void ClearPendingMessageQueue() {
    _messageQueue.Clear();
  }
}
