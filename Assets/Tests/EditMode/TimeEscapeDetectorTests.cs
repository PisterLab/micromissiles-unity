using NUnit.Framework;
using UnityEngine;

public class TimeEscapeDetectorTests : TestBase {
  private AgentBase _agent;
  private TimeEscapeDetector _escapeDetector;

  [SetUp]
  public void SetUp() {
    _agent = new GameObject("Agent").AddComponent<AgentBase>();
    Rigidbody agentRb = _agent.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(_agent, "Awake");
    _escapeDetector = new TimeEscapeDetector(_agent);
  }

  [Test]
  public void IsEscaping_NullTarget_ReturnsFalse() {
    Assert.IsFalse(_escapeDetector.IsEscaping(target: null));
  }

  [Test]
  public void IsEscaping_TargetHasNoTarget_NegativeRangeRate_ReturnsTrue() {
    _agent.Velocity = new Vector3(0, 0, 1);
    var target =
        new FixedHierarchical(position: new Vector3(0, 0, 10), velocity: new Vector3(0, 0, -2));
    Assert.IsTrue(_escapeDetector.IsEscaping(target));
  }

  [Test]
  public void IsEscaping_TargetHasNoTarget_PositiveRangeRate_ReturnsFalse() {
    _agent.Velocity = new Vector3(0, 0, 1);
    var target =
        new FixedHierarchical(position: new Vector3(0, 0, 10), velocity: new Vector3(0, 0, 2));
    Assert.IsFalse(_escapeDetector.IsEscaping(target));
  }

  [Test]
  public void IsEscaping_TargetHasNoTarget_ZeroRangeRate_ReturnsFalse() {
    _agent.Velocity = new Vector3(0, 0, 1);
    var target =
        new FixedHierarchical(position: new Vector3(0, 0, 10), velocity: new Vector3(0, 0, 1));
    Assert.IsFalse(_escapeDetector.IsEscaping(target));
  }

  [Test]
  public void IsEscaping_AgentCloseToTarget_ReturnsFalse() {
    _agent.Velocity = new Vector3(0, 0, 1);
    var target =
        new FixedHierarchical(position: new Vector3(0, 0, 10), velocity: new Vector3(0, 0, -1)) {
          Target = new FixedHierarchical(position: new Vector3(1, 0, 0))
        };
    Assert.IsFalse(_escapeDetector.IsEscaping(target));
  }

  [Test]
  public void IsEscaping_AgentFarFromTarget_ReturnsTrue() {
    _agent.Position = new Vector3(0, 12, 5);
    _agent.Velocity = new Vector3(0, 0, 1);
    var target =
        new FixedHierarchical(position: new Vector3(0, 0, 10), velocity: new Vector3(0, 0, -1)) {
          Target = new FixedHierarchical(position: new Vector3(1, 0, 0))
        };
    Assert.IsTrue(_escapeDetector.IsEscaping(target));
  }

  [Test]
  public void IsEscaping_SlowTarget_ReturnsFalse() {
    _agent.Velocity = new Vector3(0, 0, 1);
    var target =
        new FixedHierarchical(position: new Vector3(0, 0, 10), velocity: new Vector3(0, 0, -0.1f)) {
          Target = new FixedHierarchical(position: new Vector3(1, 0, 0))
        };
    Assert.IsFalse(_escapeDetector.IsEscaping(target));
  }

  [Test]
  public void IsEscaping_FastTarget_ReturnsTrue() {
    _agent.Velocity = new Vector3(0, 0, 1);
    var target =
        new FixedHierarchical(position: new Vector3(0, 0, 10), velocity: new Vector3(0, 0, -5)) {
          Target = new FixedHierarchical(position: new Vector3(1, 0, 0))
        };
    Debug.Log(target.Speed);
    Assert.IsTrue(_escapeDetector.IsEscaping(target));
  }

  [Test]
  public void IsEscaping_EqualTimeToHit_ReturnsFalse() {
    _agent.Position = new Vector3(-1, 0, 0);
    _agent.Velocity = new Vector3(0, 0, 1);
    var target =
        new FixedHierarchical(position: new Vector3(0, 0, 10), velocity: new Vector3(0, 0, -1)) {
          Target = new FixedHierarchical(position: new Vector3(1, 0, 0))
        };
    Assert.IsFalse(_escapeDetector.IsEscaping(target));
  }

  [Test]
  public void IsEscaping_SlowAgentBehindTarget_ReturnsTrue() {
    _agent.Position = new Vector3(0, 0, 12);
    _agent.Velocity = new Vector3(0, 0, -0.5f);
    var target =
        new FixedHierarchical(position: new Vector3(0, 0, 10), velocity: new Vector3(0, 0, -1)) {
          Target = new FixedHierarchical(position: new Vector3(1, 0, 0))
        };
    Assert.IsTrue(_escapeDetector.IsEscaping(target));
  }

  [Test]
  public void IsEscaping_FastAgentBehindTarget_ReturnsFalse() {
    _agent.Position = new Vector3(0, 0, 12);
    _agent.Velocity = new Vector3(0, 0, -2f);
    var target =
        new FixedHierarchical(position: new Vector3(0, 0, 10), velocity: new Vector3(0, 0, -1)) {
          Target = new FixedHierarchical(position: new Vector3(1, 0, 0))
        };
    Assert.IsFalse(_escapeDetector.IsEscaping(target));
  }

  [Test]
  public void IsEscaping_SameSpeedAgentBehindTarget_ReturnsFalse() {
    _agent.Position = new Vector3(0, 0, 12);
    _agent.Velocity = new Vector3(0, 0, -1f);
    var target =
        new FixedHierarchical(position: new Vector3(0, 0, 10), velocity: new Vector3(0, 0, -1)) {
          Target = new FixedHierarchical(position: new Vector3(1, 0, 0))
        };
    Assert.IsFalse(_escapeDetector.IsEscaping(target));
  }
}
