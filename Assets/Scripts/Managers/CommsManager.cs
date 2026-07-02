using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// CommsManager owns the lifecycle of interceptor comms nodes and keeps that lifecycle tied to
// simulation events rather than Unity object construction and destruction.
public class CommsManager : MonoBehaviour {
  public static CommsManager Instance { get; private set; }

  private readonly Dictionary<IAgent, CommsNode> _nodesByAgent =
      new Dictionary<IAgent, CommsNode>();

  // Ensure the comms manager exists before scene objects begin their normal lifecycle.
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
  private static void OnBeforeSceneLoad() {
    if (Instance != null) {
      return;
    }

    var managerObject = new GameObject("CommsManager");
    DontDestroyOnLoad(managerObject);
    managerObject.AddComponent<CommsManager>();
  }

  // Enforce a single persistent comms manager instance across scene loads.
  private void Awake() {
    if (Instance != null && Instance != this) {
      Destroy(gameObject);
      return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject);
  }

  // Wait for the simulation manager, then subscribe to interceptor lifecycle events and catch up
  // any interceptors that already exist.
  private IEnumerator Start() {
    while (SimManager.Instance == null) {
      yield return null;
    }

    SimManager.Instance.OnSimulationEnded += RegisterSimulationEnded;
    SimManager.Instance.OnNewInterceptor += RegisterNewInterceptor;

    foreach (var agent in SimManager.Instance.Interceptors) {
      if (agent is IInterceptor interceptor) {
        RegisterNewInterceptor(interceptor);
      }
    }
  }

  // Unsubscribe from simulation events and terminate any endpoints still owned by this manager.
  private void OnDestroy() {
    if (SimManager.Instance != null) {
      SimManager.Instance.OnSimulationEnded -= RegisterSimulationEnded;
      SimManager.Instance.OnNewInterceptor -= RegisterNewInterceptor;
    }

    RegisterSimulationEnded();

    if (Instance == this) {
      Instance = null;
    }
  }

  // Create and attach a comms node the first time an interceptor is observed by the manager.
  private void RegisterNewInterceptor(IInterceptor interceptor) {
    if (interceptor == null || _nodesByAgent.ContainsKey(interceptor)) {
      return;
    }

    var agentBase = interceptor as AgentBase;
    if (agentBase == null) {
      return;
    }

    var commsNode = new CommsNode(interceptor);
    agentBase.AttachCommsNode(commsNode);
    interceptor.OnTerminated += RegisterAgentTerminated;
    _nodesByAgent.Add(interceptor, commsNode);
  }

  // Tear down an interceptor's comms node when that interceptor terminates.
  private void RegisterAgentTerminated(IAgent agent) {
    if (agent == null || !_nodesByAgent.TryGetValue(agent, out CommsNode commsNode)) {
      return;
    }

    agent.OnTerminated -= RegisterAgentTerminated;
    commsNode.Terminate();
    _nodesByAgent.Remove(agent);
  }

  // Terminate every tracked comms node when the simulation ends or the manager is torn down.
  private void RegisterSimulationEnded() {
    foreach (var agentNodePair in _nodesByAgent) {
      agentNodePair.Key.OnTerminated -= RegisterAgentTerminated;
      agentNodePair.Value.Terminate();
    }
    _nodesByAgent.Clear();
  }
}
