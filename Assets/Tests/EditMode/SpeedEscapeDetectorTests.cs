using NUnit.Framework;
using UnityEngine;

public class SpeedEscapeDetectorTests : TestBase {
  private AgentBase _agent;
  private SpeedEscapeDetector _escapeDetector;

  [SetUp]
  public void SetUp() {
    _agent = new GameObject("Agent").AddComponent<AgentBase>();
    _agent.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(_agent, "Awake");
    _agent.StaticConfig = new Configs.StaticConfig() {
      AccelerationConfig =
          new Configs.AccelerationConfig() {
            MaxForwardAcceleration = 10,
            MaxReferenceNormalAcceleration = 5 / Constants.kGravity,
            ReferenceSpeed = 0.5f,
          },
      LiftDragConfig =
          new Configs.LiftDragConfig() {
            DragCoefficient = 0.7f,
            LiftDragRatio = 5,
          },
      BodyConfig =
          new Configs.BodyConfig() {
            CrossSectionalArea = 1,
            Mass = 1,
          },
    };
    _escapeDetector = new SpeedEscapeDetector(_agent);
  }

  [Test]
  public void IsEscaping_NullTarget_ReturnsFalse() {
    Assert.IsFalse(_escapeDetector.IsEscaping(target: null));
  }

  [Test]
  public void IsEscaping_AgentAheadOfTarget_ReturnsFalse() {
    _agent.Velocity = new Vector3(0, 0, 100);
    var target =
        new FixedHierarchical(position: new Vector3(0, 0, 10), velocity: new Vector3(0, 0, -1)) {
          Target = new FixedHierarchical(position: new Vector3(1, 0, 0))
        };
    Assert.IsFalse(_escapeDetector.IsEscaping(target));
  }

  [Test]
  public void IsEscaping_AgentFarFromTarget_ReturnsTrue() {
    _agent.Position = new Vector3(0, 0, -100);
    _agent.Velocity = new Vector3(0, 0, 10);
    var target =
        new FixedHierarchical(position: new Vector3(0, 0, 10), velocity: new Vector3(0, 0, -1)) {
          Target = new FixedHierarchical(position: new Vector3(1, 0, 0))
        };
    Assert.IsTrue(_escapeDetector.IsEscaping(target));
  }

  [Test]
  public void IsEscaping_AgentBehindTarget_ReturnsTrue() {
    _agent.Velocity = new Vector3(0, 0, -10);
    var target =
        new FixedHierarchical(position: new Vector3(0, 0, 10), velocity: new Vector3(0, 0, -1)) {
          Target = new FixedHierarchical(position: new Vector3(1, 0, 0))
        };
    Assert.IsTrue(_escapeDetector.IsEscaping(target));
  }
}
