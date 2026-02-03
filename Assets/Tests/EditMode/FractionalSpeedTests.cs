using NUnit.Framework;
using UnityEngine;

public class FractionalSpeedTests : TestBase {
  private AgentBase _agent;

  [SetUp]
  public void SetUp() {
    _agent = new GameObject("Agent").AddComponent<AgentBase>();
    _agent.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(_agent, "Awake");
    _agent.Velocity = new Vector3(0, 0, 1);
    _agent.Transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, 1));
    // No maximum normal acceleration is specified, so it defaults to infinite normal acceleration,
    // meaning the agent has a minimum turn radius of 0.
    _agent.StaticConfig = new Configs.StaticConfig() {
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
  }

  [Test]
  public void Calculate_DistanceOnly_ReturnsCorrectly() {
    Vector3 targetPosition = new Vector3(0, 0, 5);
    float distanceTimeConstant = 2 / (1.204f * 0.7f * 1);
    float expectedFractionalSpeed = Mathf.Exp(-5 / distanceTimeConstant);
    Assert.AreEqual(expectedFractionalSpeed, FractionalSpeed.Calculate(_agent, targetPosition));
  }

  [Test]
  public void Calculate_DistanceAndBearing_ReturnsCorrectly() {
    Vector3 targetPosition = new Vector3(0, 5, 0);
    float distanceTimeConstant = 2 / (1.204f * 0.7f * 1);
    float angleTimeConstant = 5f;
    float expectedFractionalSpeed =
        Mathf.Exp(-(5 / distanceTimeConstant + Mathf.PI / 2 / angleTimeConstant));
    Assert.AreEqual(expectedFractionalSpeed, FractionalSpeed.Calculate(_agent, targetPosition));
  }

  [Test]
  public void Calculate_DistanceOnly_MissingLiftDragConfig_ReturnsOne() {
    _agent.StaticConfig.LiftDragConfig = null;
    Vector3 targetPosition = new Vector3(0, 0, 5);
    float expectedFractionalSpeed = 1f;
    Assert.AreEqual(expectedFractionalSpeed, FractionalSpeed.Calculate(_agent, targetPosition));
  }

  [Test]
  public void Calculate_DistanceOnly_MissingBodyConfig_ReturnsOne() {
    _agent.StaticConfig.BodyConfig = null;
    Vector3 targetPosition = new Vector3(0, 0, 5);
    float expectedFractionalSpeed = 1f;
    Assert.AreEqual(expectedFractionalSpeed, FractionalSpeed.Calculate(_agent, targetPosition));
  }

  [Test]
  public void Calculate_DistanceAndBearing_MissingLiftDragConfig_ReturnsOne() {
    _agent.StaticConfig.LiftDragConfig = null;
    Vector3 targetPosition = new Vector3(0, 5, 0);
    float expectedFractionalSpeed = 1f;
    Assert.AreEqual(expectedFractionalSpeed, FractionalSpeed.Calculate(_agent, targetPosition));
  }
}
