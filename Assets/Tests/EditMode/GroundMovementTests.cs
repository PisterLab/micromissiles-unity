using NUnit.Framework;
using UnityEngine;

public class GroundMovementTests : TestBase {
  private AgentBase _agent;
  private GroundMovement _movement;

  [SetUp]
  public void SetUp() {
    _agent = new GameObject("Agent").AddComponent<AgentBase>();
    Rigidbody agentRb = _agent.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(_agent, "Awake");
    _agent.StaticConfig = new Configs.StaticConfig() {
      AccelerationConfig =
          new Configs.AccelerationConfig() {
            MaxForwardAcceleration = 10,
            MaxReferenceNormalAcceleration = 5 / Constants.kGravity,
            ReferenceSpeed = 100,
          },
      BoostConfig =
          new Configs.BoostConfig() {
            BoostTime = 1,
            BoostAcceleration = 100,
          },
    };
    _agent.Velocity = new Vector3(x: 0, y: 0, z: 200);
    _movement = new GroundMovement(_agent);
  }

  [Test]
  public void Act_KeepsAgentWithinXZPlane() {
    Vector3 accelerationInput = new Vector3(1, -2, 3);
    Vector3 appliedAccelerationInput = _movement.Act(accelerationInput);
    Assert.AreEqual(new Vector3(1, 0, 3), appliedAccelerationInput);
  }
}
