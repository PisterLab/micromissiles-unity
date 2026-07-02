using NUnit.Framework;
using UnityEngine;

public class CommsNodeTests {
  // Verifies that an active comms node forwards an incoming message to its subscribers.
  [Test]
  public void Receive_WhenNodeIsActive_InvokesSubscribers() {
    var sender = new CommsNode(CommsEndpointType.Vessel);
    var receiver = new CommsNode(CommsEndpointType.Invalid);
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

  // Verifies that termination does not suppress delivery; subscribers are responsible for
  // unsubscribing when the owning endpoint dies.
  [Test]
  public void Receive_WhenNodeIsTerminated_StillInvokesSubscribers() {
    var sender = new CommsNode(CommsEndpointType.Vessel);
    var receiver = new CommsNode(CommsEndpointType.Invalid);
    var message = new TestMessage(sender, receiver);
    int receivedCount = 0;

    receiver.OnMessageReceived +=
        _ => { ++receivedCount; };
    receiver.Terminate();

    receiver.Receive(message);

    Assert.IsTrue(receiver.IsTerminated);
    Assert.AreEqual(1, receivedCount);
  }

  private sealed class TestPayload : IMessagePayload {}

  private sealed class TestMessage : Message {
    private readonly TestPayload _payload = new TestPayload();

    public override IMessagePayload Payload => _payload;

    public TestMessage(CommsNode sender, CommsNode receiver)
        : base(sender, receiver, MessageType.AssignTargetRequest) {}
  }
}
