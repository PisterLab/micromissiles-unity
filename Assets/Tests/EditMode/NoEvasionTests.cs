using NUnit.Framework;
using UnityEngine;

public class NoEvasionTests : TestBase {
  private AgentBase _agent;
  private AgentBase _pursuer;
  private NoEvasion _evasion;

  [SetUp]
  public void SetUp() {
    _agent = new GameObject("Agent").AddComponent<AgentBase>();
    Rigidbody agentRb = _agent.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(_agent, "Awake");
    _pursuer = new GameObject("Agent").AddComponent<AgentBase>();
    Rigidbody pursuerRb = _pursuer.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(_pursuer, "Awake");
    _evasion = new NoEvasion(_agent);
  }

  [Test]
  public void ShouldEvade_ReturnsFalse() {
    Assert.IsFalse(_evasion.ShouldEvade(_pursuer));
  }

  [Test]
  public void Evade_ReturnsZero() {
    Assert.AreEqual(Vector3.zero, _evasion.Evade(_pursuer));
  }
}
