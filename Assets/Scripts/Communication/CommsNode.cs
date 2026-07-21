using System;

// Simple CommsNode class that simply invokes OnReceived.
public sealed class CommsNode {
  private readonly Configs.AgentType _agentType;

  public event Action<Message> OnMessageReceived;

  public Configs.AgentType AgentType => _agentType;

  public CommsNode(Configs.AgentType agentType = Configs.AgentType.InvalidType) {
    _agentType = agentType;
  }

  public void Receive(Message message) {
    if (message == null) {
      throw new ArgumentNullException(nameof(message));
    }
    OnMessageReceived?.Invoke(message);
  }
}
