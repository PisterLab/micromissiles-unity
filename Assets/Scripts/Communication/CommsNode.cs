using System;
using UnityEngine;

// The communication node is an endpoint encapsulated by an agent or system like the IADS to
// interface with the mailbox.
public sealed class CommsNode {
  private readonly IAgent _agentOwner;
  private readonly UnityEngine.Object _unityOwner;
  private readonly bool _tracksUnityOwner;
  private readonly Configs.AgentType _agentType;
  private bool _isTerminated;

  public event Action<Message> OnMessageReceived;

  public Configs.AgentType AgentType => _agentOwner?.StaticConfig?.AgentType ?? _agentType;

  public bool IsTerminated {
    get {
      if (_isTerminated) {
        return true;
      }
      if (_tracksUnityOwner && _unityOwner == null) {
        return true;
      }
      if (_agentOwner is UnityEngine.Object unityObject && unityObject == null) {
        return true;
      }
      return _agentOwner?.IsTerminated ?? false;
    }
  }

  public CommsNode(IAgent agentOwner) {
    _agentOwner = agentOwner ?? throw new ArgumentNullException(nameof(agentOwner));
    _unityOwner = agentOwner as UnityEngine.Object;
    _tracksUnityOwner = _unityOwner != null;
    _agentType = Configs.AgentType.InvalidType;
  }

  public CommsNode(UnityEngine.Object unityOwner,
                   Configs.AgentType agentType = Configs.AgentType.InvalidType) {
    _unityOwner = unityOwner ? unityOwner : throw new ArgumentNullException(nameof(unityOwner));
    _tracksUnityOwner = true;
    _agentType = agentType;
  }

  public CommsNode(Configs.AgentType agentType = Configs.AgentType.InvalidType) {
    _agentType = agentType;
  }

  public void Receive(Message message) {
    if (message == null) {
      throw new ArgumentNullException(nameof(message));
    }
    if (IsTerminated) {
      return;
    }
    OnMessageReceived?.Invoke(message);
  }

  public void Terminate() {
    _isTerminated = true;
  }
}
