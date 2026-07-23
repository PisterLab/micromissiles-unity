using System;

// The communication node is an endpoint encapsulated by an agent or system like the IADS to
// interface with the mailbox.
public sealed class CommsNode {
  private readonly Configs.AgentType _agentType;
  private bool _isTerminated;

  // The OnMessageReceived event handler is called when the communication node receives a message.
  public event Action<Message> OnMessageReceived;

  public Configs.AgentType AgentType => _agentType;

  public bool IsTerminated => _isTerminated;

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
