using System.Collections.Generic;
using UnityEngine;

// The comunication manager manages the communication nodes and handles communication between
// agents.
public class CommsManager : MonoBehaviour {
  public static CommsManager Instance { get; private set; }

  // Mailbox for queued messages.
  private readonly Mailbox _mailbox = new Mailbox();

  // Map from agent to the communication node.
  private readonly HashSet<CommsNode> _nodes = new HashSet<CommsNode>();

  // Keep track of received messages for logging purposes.
  private readonly HashSet<Message> _receivedMessages = new HashSet<Message>();

  // Add a communication node. This function should only be used by the IADS.
  public void AddNode(CommsNode node) => _nodes.Add(node);

  public void RemoveNode(CommsNode node) => _nodes.Remove(node);

  public bool ContainsNode(CommsNode node) => _nodes.Contains(node);

  private void Awake() {
    if (Instance != null && Instance != this) {
      Destroy(gameObject);
      return;
    }
    Instance = this;
  }

  private void Start() {
    SimManager.Instance.OnSimulationStarted += _mailbox.Clear;
    SimManager.Instance.OnSimulationEnded += () => {
      _nodes.Clear();
      _mailbox.Clear();
    };
    SimManager.Instance.OnNewInterceptor += RegisterNewAgent;
    SimManager.Instance.OnNewLauncher += RegisterNewAgent;
    Mailbox.OnMessageReceived += RegisterMessageReceived;
  }

<<<<<<< HEAD
=======
  private void OnDestroy() {
    if (SimManager.Instance == null || _mailbox == null) {
      return;
    }
    SimManager.Instance.OnSimulationEnded -= ClearNodes;
    SimManager.Instance.OnSimulationStarted -= _mailbox.ClearPendingMessageQueue;
    SimManager.Instance.OnSimulationEnded -= _mailbox.ClearPendingMessageQueue;
    SimManager.Instance.OnNewInterceptor -= RegisterNewAgent;
    SimManager.Instance.OnNewLauncher -= RegisterNewAgent;
    Mailbox.OnMessageReceived -= RegisterMessageReceived;
  }

  private void RegisterMessageReceived(Message message) {
    if (message == null) {
      return;
    }
    _receivedMessages.Add(message);
    // TODO (Joseph): Need to find a way to identify which agent sent/received the message for
    // logging purposes. Currently it just shows EndpointType.
    Debug.Log(
        $"{message.Sender.EndpointType} sent {message.Type} to {message.Receiver.EndpointType}.");
  }

>>>>>>> 6d8f8cecf (Added Logging and TODOs)
  private void FixedUpdate() {
    _mailbox.Deliver();
  }

  public void SendMessage(Message message) {
    _mailbox.SendMessage(message);
  }

  private void RegisterNewAgent(IAgent agent) {
    if (agent.CommsNode != null) {
      _nodes.Add(agent.CommsNode);
      return;
    }

    var commsNode = new CommsNode(agent.StaticConfig.AgentType);
    agent.CommsNode = commsNode;
    agent.OnTerminated +=
        _ => RemoveNode(commsNode);
    _nodes.Add(commsNode);
  }
}
