using NUnit.Framework;
using UnityEngine;

public class RocketMovementTests : TestBase {
  private const float _epsilon = 1e-3f;

  private AgentBase _agent;
  private RocketMovement _movement;

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
      LiftDragConfig =
          new Configs.LiftDragConfig() {
            DragCoefficient = 1,
            LiftDragRatio = 1,
          },
      BodyConfig =
          new Configs.BodyConfig() {
            CrossSectionalArea = 1,
            Mass = 1,
          },
    };
    Rigidbody agentRb = _agent.gameObject.AddComponent<Rigidbody>();
    _agent.transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, 1));
    InvokePrivateMethod(_agent, "Awake");
    _movement = new RocketMovement(_agent);
  }

  [Test]
  public void Act_Initialized_ReturnsZero() {
    _movement.FlightPhase = Simulation.FlightPhase.Initialized;
    var accelerationInput = new Vector3(1, -2, 3);
    var appliedAccelerationInput = _movement.Act(accelerationInput);
    Assert.AreEqual(Vector3.zero, appliedAccelerationInput);
  }

  [Test]
  public void Act_Ready_AddsAirDrag() {
    _movement.FlightPhase = Simulation.FlightPhase.Ready;
    _agent.Velocity = new Vector3(0, 0, 100);
    var accelerationInput = Vector3.zero;
    var appliedAccelerationInput = _movement.Act(accelerationInput);
    Assert.AreEqual(-6020f - Constants.kGravity, appliedAccelerationInput.z, _epsilon);
  }

  [Test]
  public void Act_Ready_AddsLiftInducedDrag() {
    _movement.FlightPhase = Simulation.FlightPhase.Ready;
    var accelerationInput = Vector3.zero;
    var appliedAccelerationInput = _movement.Act(accelerationInput);
    Assert.AreEqual(-Constants.kGravity, appliedAccelerationInput.z, _epsilon);
  }

  [Test]
  public void Act_Ready_AddsGravity() {
    _movement.FlightPhase = Simulation.FlightPhase.Ready;
    var accelerationInput = Vector3.zero;
    var appliedAccelerationInput = _movement.Act(accelerationInput);
    Assert.AreEqual(-Constants.kGravity, appliedAccelerationInput.y, _epsilon);
  }

  [Test]
  public void Act_Midcourse_AddsAirDrag() {
    _movement.FlightPhase = Simulation.FlightPhase.Midcourse;
    _agent.Velocity = new Vector3(0, 0, 100);
    var accelerationInput = Vector3.zero;
    var appliedAccelerationInput = _movement.Act(accelerationInput);
    Assert.AreEqual(-6020f - Constants.kGravity, appliedAccelerationInput.z, _epsilon);
  }

  [Test]
  public void Act_Midcourse_AddsLiftInducedDrag() {
    _movement.FlightPhase = Simulation.FlightPhase.Midcourse;
    var accelerationInput = Vector3.zero;
    var appliedAccelerationInput = _movement.Act(accelerationInput);
    Assert.AreEqual(-Constants.kGravity, appliedAccelerationInput.z, _epsilon);
  }

  [Test]
  public void Act_MidCourse_AddsGravity() {
    _movement.FlightPhase = Simulation.FlightPhase.Midcourse;
    var accelerationInput = Vector3.zero;
    var appliedAccelerationInput = _movement.Act(accelerationInput);
    Assert.AreEqual(-Constants.kGravity, appliedAccelerationInput.y, _epsilon);
  }

  [Test]
  public void Act_Terminated_ReturnsZero() {
    _movement.FlightPhase = Simulation.FlightPhase.Terminated;
    var accelerationInput = new Vector3(1, -2, 3);
    var appliedAccelerationInput = _movement.Act(accelerationInput);
    Assert.AreEqual(Vector3.zero, appliedAccelerationInput);
  }
}
