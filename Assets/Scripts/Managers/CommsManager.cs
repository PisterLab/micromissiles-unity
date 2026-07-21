using System.Collections.Generic;
using UnityEngine;

// The communication manager manages and owns the set of currently active communication nodes.
public class CommsManager : MonoBehaviour {
  public static CommsManager Instance { get; private set; }

  private readonly HashSet<CommsNode> _nodes = new HashSet<CommsNode>();

  public void AddNode(CommsNode node) {
    if (node != null) {
      _nodes.Add(node);
    }
  }

  public void RemoveNode(CommsNode node) {
    if (node != null) {
      _nodes.Remove(node);
    }
  }

  public bool ContainsNode(CommsNode node) => node != null && _nodes.Contains(node);

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
        _ => RemoveNode(commsNode);
    AddNode(commsNode);
  }
}
