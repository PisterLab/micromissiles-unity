using System.Collections.Generic;
using UnityEngine;

// The comunication manager manages the communication nodes and handles communication between
// agents.
public class CommsManager : MonoBehaviour {
  public static CommsManager Instance { get; private set; }

  private Mailbox _mailbox;

  // Map from agent to the communication node.
  private readonly HashSet<CommsNode> _nodes = new HashSet<CommsNode>();

  // Add a communication node. This function should only be used by the IADS.
  public void AddNode(CommsNode node) => _nodes.Add(node);

  public bool ContainsNode(CommsNode node) => _nodes.Contains(node);

  private void Awake() {
    if (Instance != null && Instance != this) {
      Destroy(gameObject);
      return;
    }
    Instance = this;
    _mailbox = new Mailbox();
  }

  private void Start() {
    SimManager.Instance.OnSimulationEnded += ClearNodes;
    SimManager.Instance.OnSimulationStarted += _mailbox.ClearPendingMessageQueue;
    SimManager.Instance.OnSimulationEnded += _mailbox.ClearPendingMessageQueue;
    SimManager.Instance.OnNewInterceptor += RegisterNewAgent;
    SimManager.Instance.OnNewLauncher += RegisterNewAgent;
  }

  private void OnDestroy() {
    if (SimManager.Instance == null || _mailbox == null) {
      return;
    }
    SimManager.Instance.OnSimulationEnded -= ClearNodes;
    SimManager.Instance.OnSimulationStarted -= _mailbox.ClearPendingMessageQueue;
    SimManager.Instance.OnSimulationEnded -= _mailbox.ClearPendingMessageQueue;
    SimManager.Instance.OnNewInterceptor -= RegisterNewAgent;
    SimManager.Instance.OnNewLauncher -= RegisterNewAgent;
  }

  private void FixedUpdate() {
    _mailbox.UpdateMailbox();
  }

  private void ClearNodes() {
    _nodes.Clear();
  }

  private void RegisterNewAgent(IAgent agent) {
    var commsNode = new CommsNode(agent.StaticConfig.AgentType);
    agent.CommsNode = commsNode;
    agent.OnTerminated +=
        _ => _nodes.Remove(commsNode);
    _nodes.Add(commsNode);
  }
}
