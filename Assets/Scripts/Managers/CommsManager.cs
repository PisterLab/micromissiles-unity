using System.Collections.Generic;
using UnityEngine;

// The comunication manager manages the communication nodes and handles communication between
// agents.
public class CommsManager : MonoBehaviour {
  public static CommsManager Instance { get; private set; }

  // Map from agent to the communication node.
  private readonly HashSet<CommsNode> _nodes = new HashSet<CommsNode>();

  // Add a communication node. This function should only be used by the IADS.
  public void AddNode(CommsNode node) => _nodes.Add(node);

  private void Awake() {
    if (Instance != null && Instance != this) {
      Destroy(gameObject);
      return;
    }
    Instance = this;
  }

  private void Start() {
    SimManager.Instance.OnSimulationStarted += RegisterSimulationStarted;
    SimManager.Instance.OnSimulationEnded += () => _nodes.Clear();
    SimManager.Instance.OnNewInterceptor += RegisterNewAgent;
    SimManager.Instance.OnNewLauncher += RegisterNewAgent;
  }

  private void RegisterSimulationStarted() {
    // Each simulation run reconfigures the mailbox and clears old queued messages.
    _nodes.Clear();
    Mailbox.GetOrCreateInstance().Configure(
        SimManager.Instance?.SimulationConfig?.CommunicationConfig);
  }

  private void RegisterNewAgent(IAgent agent) {
    var commsNode = new CommsNode(agent.StaticConfig.AgentType);
    agent.CommsNode = commsNode;
    agent.OnTerminated +=
        _ => _nodes.Remove(commsNode);
    _nodes.Add(commsNode);
  }
}
