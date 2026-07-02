using NUnit.Framework;
using UnityEngine;

public class CommsNodeTests {
  // Verifies that an active comms node forwards an incoming message to its subscribers.
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

  // Verifies that a terminated comms node drops incoming messages and does not notify subscribers.
  [Test]
  public void Receive_WhenNodeIsTerminated_DoesNotInvokeSubscribers() {
    var sender = new CommsNode(Configs.AgentType.Vessel);
    var receiver = new CommsNode(Configs.AgentType.InvalidType);
    var message = new TestMessage(sender, receiver);
    int receivedCount = 0;

    receiver.OnMessageReceived +=
        _ => { ++receivedCount; };
    receiver.Terminate();

    receiver.Receive(message);

    Assert.IsTrue(receiver.IsTerminated);
    Assert.AreEqual(0, receivedCount);
  }

  private sealed class TestPayload : IMessagePayload {}

  private sealed class TestMessage : Message {
    private readonly TestPayload _payload = new TestPayload();

    public override IMessagePayload Payload => _payload;

    public TestMessage(CommsNode sender, CommsNode receiver)
        : base(sender, receiver, MessageType.AssignTargetRequest) {}
  }
}
