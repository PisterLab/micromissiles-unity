using System;
using System.Collections.Generic;
using UnityEngine;

// The centralized mailbox is what all agents use to send and receive delayed messages.

public class Mailbox : MonoBehaviour {
  public static Mailbox Instance { get; private set; }
  public event Action<Message> OnMessageDelivered;

  // Dictionary key for a one-way directed communication link between two agent types.
  private readonly struct LinkKey : IEquatable<LinkKey> {
    public readonly Configs.AgentType From;
    public readonly Configs.AgentType To;

    // Identifies a directed sender->receiver link in the communication config.
    public LinkKey(Configs.AgentType from, Configs.AgentType to) {
      From = from;
      To = to;
    }

    // Compares two link keys for dictionary lookup.
    public bool Equals(LinkKey other) => From == other.From && To == other.To;
    // Produces a stable hash for use in the per-link override dictionary.
    public override int GetHashCode() => ((int)From * 397) ^ (int)To;
  }

  // Runtime communication settings for one link, taken directly from proto files.
  private readonly struct LinkRuntimeConfig {
    // Default link config with 0 latency, 0 jitter, and no drops.
    public static readonly LinkRuntimeConfig ZeroLatencyNoDrops = new LinkRuntimeConfig(0f, 0f, 1f);

    public readonly float LatencySeconds;
    public readonly float LatencyStdSeconds;
    public readonly float PacketDeliveryRatio;

    // Insert proto values into runtime values.
    public LinkRuntimeConfig(float latencySeconds, float latencyStdSeconds,
                             float packetDeliveryRatio) {
      LatencySeconds = Mathf.Max(0f, latencySeconds);
      LatencyStdSeconds = Mathf.Max(0f, latencyStdSeconds);
      PacketDeliveryRatio = Mathf.Clamp01(packetDeliveryRatio);
    }
  }

  private PriorityQueue<PendingMessage> _messageQueue = new PriorityQueue<PendingMessage>();
  // _messageBuffer is a reusable temporary buffer; due messages are added, processed, then
  // cleared every delivery frame.
  private readonly List<PendingMessage> _messageBuffer = new();
  private readonly Dictionary<LinkKey, LinkRuntimeConfig> _linkOverrides = new();

  // If no communication config exists, messages fall back to 0 latency, 0 jitter, and with PDR = 1
  // delivery.
  private LinkRuntimeConfig _defaultLinkConfig = LinkRuntimeConfig.ZeroLatencyNoDrops;

  public static Mailbox GetOrCreateInstance() {
    if (Instance != null) {
      return Instance;
    }
    Instance = UnityEngine.Object.FindFirstObjectByType<Mailbox>();
    if (Instance != null) {
      return Instance;
    }
    var mailboxObject = new GameObject(nameof(Mailbox));
    DontDestroyOnLoad(mailboxObject);
    Instance = mailboxObject.AddComponent<Mailbox>();
    return Instance;
  }

  // Initializes the mailbox from the current simulation config.
  private void Awake() {
    if (Instance != null && Instance != this) {
      Destroy(gameObject);
      return;
    }
    Instance = this;
    Configure(SimManager.Instance?.SimulationConfig?.CommunicationConfig);
  }

  // Advances message delivery once per frame using simulation time.
  private void Update() {
    DeliverMessages();
  }

  // Rebuilds runtime link settings from the protobuf communication config.
  public void Configure(Configs.CommunicationConfig communicationConfig) {
    ClearPendingMessages();
    _linkOverrides.Clear();

    // If ToRuntimeConfig(null) it uses ZeroLatencyNoDrops, else set to standard
    // link_config.
    _defaultLinkConfig = ToRuntimeConfig(communicationConfig?.LinkConfig);

    // If no communication config links exists, the mailbox uses ZeroLatencyNoDrops
    // default.
    if (communicationConfig == null) {
      return;
    }

    // Iterate through every link pair from config to create LinkKey.
    foreach (Configs.LinkOverride linkOverride in communicationConfig.LinkOverrides) {
      _linkOverrides[new LinkKey(linkOverride.From, linkOverride.To)] =
          ToRuntimeConfig(linkOverride.LinkConfig);
    }
  }

  // Drops all queued messages.
  public void ClearPendingMessages() {
    _messageQueue = new PriorityQueue<PendingMessage>();
  }

  // Applies link loss/latency then queues a message into PQ for future delivery.
  public void Send(Message message) {
    if (message == null || !IsNodeActive(message.Sender) || !IsNodeActive(message.Receiver)) {
      return;
    }

    LinkRuntimeConfig linkConfig = GetEffectiveLinkConfig(message.Sender, message.Receiver);
    // Applies packet delivery ratio (PDR). This is done before sending into PQ.
    if (UnityEngine.Random.value > linkConfig.PacketDeliveryRatio) {
      return;
    }

    float jitter = linkConfig.LatencyStdSeconds > 0f
                       ? Utilities.SampleStandardNormal() * linkConfig.LatencyStdSeconds
                       : 0f;
    float totalLatency = Mathf.Max(0f, linkConfig.LatencySeconds + jitter);
    float deliverAt = SimManager.Instance.ElapsedTime + totalLatency;
    _messageQueue.Enqueue(new PendingMessage(message, deliverAt), deliverAt);
  }

  // Releases all queued messages in PQ whose scheduled delivery time has arrived.
  private void DeliverMessages() {
    float currentTime = SimManager.Instance.ElapsedTime;
    while (!_messageQueue.IsEmpty() && currentTime >= _messageQueue.Peek().DeliverAt) {
      _messageBuffer.Add(_messageQueue.Dequeue());
    }
    foreach (PendingMessage pending in _messageBuffer) {
      if (!IsNodeActive(pending.Receiver)) {
        continue;
      }
      pending.Receiver.Receive(pending.Message);
      OnMessageDelivered?.Invoke(pending.Message);
    }
    _messageBuffer.Clear();
  }

  // Get information on effective runtime link config for a sender->receiver LinkRuntimeConfig pair.
  private LinkRuntimeConfig GetEffectiveLinkConfig(CommsNode sender, CommsNode receiver) {
    return _linkOverrides.TryGetValue(new LinkKey(sender.AgentType, receiver.AgentType),
                                      out LinkRuntimeConfig linkConfig)
               ? linkConfig
               : _defaultLinkConfig;
  }

  // Converts a protobuf link config into runtime values.
  private static LinkRuntimeConfig ToRuntimeConfig(Configs.LinkConfig linkConfig) {
    if (linkConfig == null) {
      return LinkRuntimeConfig.ZeroLatencyNoDrops;
    }
    return new LinkRuntimeConfig(linkConfig.LatencySeconds, linkConfig.LatencyStdSeconds,
                                 linkConfig.PacketDeliveryRatio);
  }

  private static bool IsNodeActive(CommsNode node) {
    if (node == null) {
      return false;
    }
    return CommsManager.Instance == null || CommsManager.Instance.ContainsNode(node);
  }
}
