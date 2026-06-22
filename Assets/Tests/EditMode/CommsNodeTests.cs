using NUnit.Framework;
using UnityEngine;

public class CommsNodeTests {
  [Test]
  public void Receive_WhenNodeIsActive_InvokesSubscribers() {
    var sender = new CommsNode(Configs.AgentType.Vessel);
    var receiver = new CommsNode(Configs.AgentType.InvalidType);
    var message = new TestMessage(sender, receiver);
    int receivedCount = 0;
    Message deliveredMessage = null;

    receiver.OnMessageReceived += delivered => {
      ++receivedCount;
      deliveredMessage = delivered;
    };

    receiver.Receive(message);

    Assert.AreEqual(1, receivedCount);
    Assert.AreSame(message, deliveredMessage);
  }

  [Test]
  public void IadsCommsAgent_Destroyed_TerminatesItsCommsNode() {
    var endpointObject = new GameObject("IadsCommsAgentTest");

    try {
      var endpoint = endpointObject.AddComponent<IadsCommsAgent>();
      Assert.NotNull(endpoint.CommsNode);

      CommsNode node = endpoint.CommsNode;
      Assert.IsFalse(node.IsTerminated);

      Object.DestroyImmediate(endpointObject);

      Assert.IsTrue(node.IsTerminated);
    } finally {
      if (endpointObject != null) {
        Object.DestroyImmediate(endpointObject);
      }
    }
  }

  private sealed class TestPayload : IMessagePayload {}

  private sealed class TestMessage : Message {
    private readonly TestPayload _payload = new TestPayload();

    public override IMessagePayload Payload => _payload;

    public TestMessage(CommsNode sender, CommsNode receiver)
        : base(sender, receiver, MessageType.AssignTargetRequest) {}
  }
}
