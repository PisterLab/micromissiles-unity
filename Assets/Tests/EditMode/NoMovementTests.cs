using NUnit.Framework;
using UnityEngine;

public class NoMovementTests : TestBase {
  private AgentBase _agent;
  private NoMovement _movement;

  [SetUp]
  public void SetUp() {
    _agent = new GameObject("Agent").AddComponent<AgentBase>();
    Rigidbody agentRb = _agent.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(_agent, "Awake");
    _agent.Velocity = new Vector3(x: 0, y: 0, z: 200);
    _movement = new NoMovement(_agent);
  }

  [Test]
  public void Act_ReturnsZero() {
    Vector3 accelerationInput = new Vector3(1, -2, 3);
    Vector3 appliedAccelerationInput = _movement.Act(accelerationInput);
    Assert.AreEqual(Vector3.zero, appliedAccelerationInput);
  }
}
