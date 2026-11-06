using System;
using UnityEngine;

// The mailbox handles communication timing and bandwidth for agent-to-agent communication. The
// mailbox owns the message queue and releases messages after their latency and Link16 slot delay,
// subject to the configured per-slot delivery limit.
public class Mailbox {
  // Message queue.
  private readonly PriorityQueue<PendingMessage> _messageQueue =
      new PriorityQueue<PendingMessage>();

  // Link16 slot currently being delivered and the number of messages delivered in that slot.
  private long _deliverySlotIndex = -1;
  private uint _numMessagesDeliveredThisSlot;

  // Enqueue a message to be delivered after its latency at the next Link16 slot boundary.
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

      Configs.TimingConfig timingConfig =
          SimManager.Instance.SimulationConfig?.CommunicationConfig?.TimingConfig;
      float slotDuration = Mathf.Max(0f, timingConfig?.Link16SlotDurationSeconds ?? 0f);
      if (slotDuration > 0f) {
        deliverAt = Mathf.Ceil(deliverAt / slotDuration) * slotDuration;
      }

      bool bandwidthLimited =
          slotDuration > 0f && (timingConfig?.Link16MaxBandwidth ?? 0) > 0;

      // Deliver the message immediately if it is already due.
      if (deliverAt <= SimManager.Instance.ElapsedTime && !bandwidthLimited) {
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
    Configs.TimingConfig timingConfig =
        SimManager.Instance.SimulationConfig?.CommunicationConfig?.TimingConfig;
    float slotDuration = Mathf.Max(0f, timingConfig?.Link16SlotDurationSeconds ?? 0f);
    uint maxBandwidth = timingConfig?.Link16MaxBandwidth ?? 0;
    bool bandwidthLimited = slotDuration > 0f && maxBandwidth > 0;

    // Calcuate the current Link16 slot index and reset the number of messages delivered in this
    // slot if the slot index has changed when time has passed.
    if (bandwidthLimited) {
      long deliverySlotIndex = (long)Math.Floor(SimManager.Instance.ElapsedTime / slotDuration);
      if (deliverySlotIndex != _deliverySlotIndex) {
        _deliverySlotIndex = deliverySlotIndex;
        _numMessagesDeliveredThisSlot = 0;
      }

      // Bandwidth not limited case, slot index and number of messages delivered in this slot are
      // not relevant.
    } else {
      _deliverySlotIndex = -1;
      _numMessagesDeliveredThisSlot = 0;
    }

    while (!_messageQueue.IsEmpty() &&
           _messageQueue.Peek().DeliverAt <= SimManager.Instance.ElapsedTime) {
      if (bandwidthLimited && _numMessagesDeliveredThisSlot >= maxBandwidth) {
        break;
      }

      PendingMessage pendingMessage = _messageQueue.Dequeue();
      if (CommsManager.Instance.ContainsNode(pendingMessage.Receiver)) {
        pendingMessage.Receiver.Receive(pendingMessage.Message);
        if (bandwidthLimited) {
          ++_numMessagesDeliveredThisSlot;
        }
      }
    }
  }

  // Clear all pending messages.
  public void Clear() {
    _messageQueue.Clear();
    _deliverySlotIndex = -1;
    _numMessagesDeliveredThisSlot = 0;
  }
}
