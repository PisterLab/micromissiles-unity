using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;

public class IdealMovementTests : AgentTestBase {
  private const float _epsilon = 1e-3f;

  private AgentBase _agent;
  private IdealMovement _movement;

  [SetUp]
  public void SetUp() {
    _agent = new GameObject("Agent").AddComponent<AgentBase>();
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
    Rigidbody agentRb = _agent.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(_agent, "Awake");
    _agent.Velocity = new Vector3(x: 0, y: 0, z: 200);
    _movement = new IdealMovement(_agent);
  }

  [Test]
  public void Act_LimitsForwardAccelerationInput() {
    var accelerationInput = new Vector3(0, 0, 100);
    var appliedAccelerationInput = _movement.Act(accelerationInput);
    Assert.AreEqual(new Vector3(0, 0, 10), appliedAccelerationInput);
  }

  [Test]
  public void Act_LimitsForwardAccelerationInput_NegativeAcceleration() {
    var accelerationInput = new Vector3(0, 0, -20);
    var appliedAccelerationInput = _movement.Act(accelerationInput);
    Assert.AreEqual(new Vector3(0, 0, -10), appliedAccelerationInput);
  }

  [Test]
  public void Act_LimitsNormalAccelerationInput() {
    var accelerationInput = new Vector3(0, 50, 0);
    var appliedAccelerationInput = _movement.Act(accelerationInput);
    Assert.That(appliedAccelerationInput,
                Is.EqualTo(new Vector3(0, 20f, 0)).Using(Vector3EqualityComparer.Instance));
  }
}
