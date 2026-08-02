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
    SimManager.Instance.OnSimulationEnded += HandleSimulationEnded;
    SimManager.Instance.OnNewInterceptor += RegisterNewAgent;
    SimManager.Instance.OnNewLauncher += RegisterNewAgent;
  }

  private void OnDestroy() {
    if (SimManager.Instance == null || _mailbox == null) {
      return;
    }
    SimManager.Instance.OnSimulationStarted -= _mailbox.Clear;
    SimManager.Instance.OnSimulationEnded -= HandleSimulationEnded;
    SimManager.Instance.OnNewInterceptor -= RegisterNewAgent;
    SimManager.Instance.OnNewLauncher -= RegisterNewAgent;
  }

  private void FixedUpdate() {
    _mailbox.Deliver();
  }

  public void SendMessage(Message message) {
    _mailbox.Send(message);
  }

  private void HandleSimulationEnded() {
    _nodes.Clear();
    _mailbox.Clear();
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
