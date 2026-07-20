using System;

// The communication node is an endpoint encapsulated by an agent or system like the IADS to
// interface with the mailbox.
public sealed class CommsNode {
  public Configs.AgentType EndpointType { get; init; }

  // The OnReceived event handler is called when the communication node receives a message.
  public event Action<Message> OnReceived;

  public CommsNode(Configs.AgentType endpointType) {
    EndpointType = endpointType;
  }

  public void Receive(Message message) {
    OnReceived?.Invoke(message);
  }
}
