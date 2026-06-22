using System;
using UnityEngine;

public interface ICommsNodeOwner {
  CommsNode CommsNode { get; }
}

// CommsNode is the mailbox-facing endpoint owned by an agent or system such as the IADS.
public sealed class CommsNode {
  private readonly IAgent _agentOwner;
  private readonly Configs.AgentType _agentType;
  private bool _isTerminated;

  public event Action<Message> OnMessageReceived;

  public Configs.AgentType AgentType => _agentOwner?.StaticConfig?.AgentType ?? _agentType;

  public bool IsTerminated {
    get {
      if (_isTerminated) {
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
    _agentType = Configs.AgentType.InvalidType;
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
