using NUnit.Framework;
using UnityEngine;

public class GeometricEscapeDetectorTests : TestBase {
  private AgentBase _agent;
  private GeometricEscapeDetector _escapeDetector;

  [SetUp]
  public void SetUp() {
    _agent = new GameObject("Agent").AddComponent<AgentBase>();
    _agent.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(_agent, "Awake");
    _escapeDetector = new GeometricEscapeDetector(_agent);
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
    var target =
        new FixedHierarchical(position: new Vector3(0, 0, 10), velocity: new Vector3(0, 0, -1)) {
          Target = new FixedHierarchical(position: new Vector3(1, 0, 0))
        };
    Assert.IsFalse(_escapeDetector.IsEscaping(target));
  }

  [Test]
  public void IsEscaping_AgentFarFromTarget_ReturnsTrue() {
    _agent.Position = new Vector3(0, 0, -2);
    var target =
        new FixedHierarchical(position: new Vector3(0, 0, 10), velocity: new Vector3(0, 0, -1)) {
          Target = new FixedHierarchical(position: new Vector3(1, 0, 0))
        };
    Assert.IsTrue(_escapeDetector.IsEscaping(target));
  }

  [Test]
  public void IsEscaping_AgentAheadOfTarget_ReturnsFalse() {
    var target =
        new FixedHierarchical(position: new Vector3(0, 0, 10), velocity: new Vector3(0, 0, -1)) {
          Target = new FixedHierarchical(position: new Vector3(1, 0, 0))
        };
    Assert.IsFalse(_escapeDetector.IsEscaping(target));
  }

  [Test]
  public void IsEscaping_AgentBehindTarget_ReturnsTrue() {
    _agent.Position = new Vector3(0, 0, 11);
    var target =
        new FixedHierarchical(position: new Vector3(0, 0, 10), velocity: new Vector3(0, 0, -1)) {
          Target = new FixedHierarchical(position: new Vector3(1, 0, 0))
        };
    Assert.IsTrue(_escapeDetector.IsEscaping(target));
  }

  [Test]
  public void IsEscaping_AgentOrthogonalToTarget_ReturnsFalse() {
    _agent.Position = new Vector3(0, 1, 0);
    var target =
        new FixedHierarchical(position: new Vector3(0, 0, 10), velocity: new Vector3(0, 0, -1)) {
          Target = new FixedHierarchical(position: new Vector3(1, 0, 0))
        };
    Assert.IsFalse(_escapeDetector.IsEscaping(target));
  }
}
