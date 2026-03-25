using System;
using UnityEngine;

// The centralized mailbox is what all agents use to send and receive delayed messages.

public class Mailbox : MonoBehaviour {
    public static Mailbox Instance { get; private set; }
    public event Action<IAgent, Message> OnMessageDelivered;
    private PriorityQueue<PendingMessage> _messageQueue;
    private LatencyTable _latencyTable;

    // 3 modes for constructing the latency table.
    private enum LatencyMode {
        NoLatency, // Ideal communication with zero latency.
        UniformLatency, // Set all latency entries to one value.
        IndividualLatency, // Configure each from->to pair independently.
    }

    private struct LatencyOverride {
        public CommsNode From;
        public CommsNode To;
        public float Seconds;
    }

    // Latency jitter standard deviation in seconds.
    [SerializeField]
    private float _latencyJitterStdSeconds = 0f;

    // Latency mode for setting latencyTable
    [SerializeField]
    private LatencyMode _latencyMode = LatencyMode.UniformLatency;

    // Used for to set all latency to _uniformLatency value. Only works when _latencyMode = LatencyMode.UniformLatency.
    [SerializeField]
    private float _uniformLatency = 0.2f;

    // TODO (Joseph): make this table serializable or put in protobuf.
    // Can set individual latency based on node-to-node types
    private static readonly LatencyOverride[] DefaultLatencyOverrides = {
        new LatencyOverride { From = CommsNode.IADS, To = CommsNode.IADS, Seconds = 0.2f },
        new LatencyOverride { From = CommsNode.IADS, To = CommsNode.Carrier, Seconds = 0.2f },
        new LatencyOverride { From = CommsNode.Carrier, To = CommsNode.IADS, Seconds = 0.2f },
        new LatencyOverride { From = CommsNode.Carrier, To = CommsNode.Carrier, Seconds = 0.2f },
        new LatencyOverride { From = CommsNode.Carrier, To = CommsNode.Interceptor, Seconds = 0.2f },
        new LatencyOverride { From = CommsNode.Interceptor, To = CommsNode.Carrier, Seconds = 0.2f },
        new LatencyOverride { From = CommsNode.Interceptor, To = CommsNode.Interceptor, Seconds = 0.2f },
    };

    // instantiate Mailbox Component
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

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        ApplyLatencyOverrides();
    }

    private void Update() {
        DeliverDueMessages();
    }

    private void OnValidate() {
        _latencyJitterStdSeconds = Mathf.Max(0f, _latencyJitterStdSeconds);
        _uniformLatency = Mathf.Max(0f, _uniformLatency);
    }

    // Enqueue a message for delayed delivery. Message will be released when DeliverTime has reached.
    public void Send(Message message) {
        if (message == null || message.Sender == null || message.Receiver == null) { return; }
        float baseLatency = _latencyTable.Get(message.Sender.NodeType, message.Receiver.NodeType);
        float jitter = _latencyJitterStdSeconds > 0f ? SampleGaussian(mean: 0f, stdDev: _latencyJitterStdSeconds) : 0f;
        float totalLatency = Mathf.Max(0f, baseLatency + jitter);
        float deliverAt = GetCurrentTime() + totalLatency;
        _messageQueue.Enqueue(new PendingMessage(deliverAt, message), deliverAt);
    }

    // Override latency values into LatencyTable.
    private void ApplyLatencyOverrides() {
        _messageQueue = new PriorityQueue<PendingMessage>();
        switch (_latencyMode) {
            case LatencyMode.NoLatency: {
                _latencyTable = new LatencyTable();
                break;
            }
            case LatencyMode.UniformLatency: {
                _latencyTable = new LatencyTable(_uniformLatency);
                break;
            }
            case LatencyMode.IndividualLatency: {
                _latencyTable = new LatencyTable();
                foreach (var latencyEntry in DefaultLatencyOverrides) {
                    _latencyTable.Set(latencyEntry.From, latencyEntry.To, latencyEntry.Seconds);
                }
                break;
            }
            default: {
                _latencyTable = new LatencyTable();
                break;
            }
        }
    }

    // Repeadly pop due messages off the queue. Apply PDR so only certain amount of messages pass through.
    private void DeliverDueMessages() {
        float currentTime = GetCurrentTime();
        while (!_messageQueue.IsEmpty() && currentTime >= _messageQueue.Peek().DeliverAt) {
        PendingMessage pending = _messageQueue.Dequeue();
        if (!IsReceiverValid(pending.Receiver)) { continue; }
        OnMessageDelivered?.Invoke(pending.Receiver, pending.Message);
        }
    }

    // Helper function for jitter calculation.
    private static float SampleGaussian(float mean, float stdDev) {
        float u1 = Mathf.Max(float.Epsilon, UnityEngine.Random.value);
        float u2 = UnityEngine.Random.value;
        float standardNormal = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
        return mean + stdDev * standardNormal;
    }

    private static float GetCurrentTime() {
        return SimManager.Instance != null ? SimManager.Instance.ElapsedTime : Time.time;
    }

    // Check if receiver still exists.
    private static bool IsReceiverValid(IAgent receiver) {
        if (receiver == null) {
            return false;
        }
        if (receiver is UnityEngine.Object unityObject && unityObject == null) {
            return false;
        }
        return !receiver.IsTerminated;
    }
}
