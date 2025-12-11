using NUnit.Framework;
using UnityEngine;

public class MissileMovementTests : TestBase {
  private const float _epsilon = 1e-3f;

  private AgentBase _agent;
  private MissileMovement _movement;

  [SetUp]
  public void SetUp() {
    _agent = new GameObject("Agent").AddComponent<AgentBase>();
    Rigidbody agentRb = _agent.gameObject.AddComponent<Rigidbody>();
    _agent.transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, 1));
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
            BoostAcceleration = 100 / Constants.kGravity,
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
    _movement = new MissileMovement(_agent);
  }

  [Test]
  public void Act_Initialized_ReturnsZero() {
    _movement.FlightPhase = Simulation.FlightPhase.Initialized;
    Vector3 accelerationInput = new Vector3(1, -2, 3);
    Vector3 appliedAccelerationInput = _movement.Act(accelerationInput);
    Assert.AreEqual(Vector3.zero, appliedAccelerationInput);
  }

  [Test]
  public void Act_Ready_AddsAirDrag() {
    _movement.FlightPhase = Simulation.FlightPhase.Ready;
    _agent.Velocity = new Vector3(0, 0, 100);
    Vector3 accelerationInput = Vector3.zero;
    Vector3 appliedAccelerationInput = _movement.Act(accelerationInput);

    // Calculate the expected air drag.
    // Velocity = 100 m/s, Cd = 1, area = 1 m^2, mass = 1 kg, altitude = 0 m.
    float airDensity = Constants.CalculateAirDensityAtAltitude(altitude: 0);
    float dynamicPressure = 0.5f * airDensity * 100f * 100f;
    float dragForce = 1f * dynamicPressure * 1f;
    float expectedDrag = dragForce / 1f;
    Assert.AreEqual(-expectedDrag - Constants.kGravity, appliedAccelerationInput.z, _epsilon);
  }

  [Test]
  public void Act_Ready_AddsLiftInducedDrag() {
    _movement.FlightPhase = Simulation.FlightPhase.Ready;
    Vector3 accelerationInput = Vector3.zero;
    Vector3 appliedAccelerationInput = _movement.Act(accelerationInput);
    Assert.AreEqual(-Constants.kGravity, appliedAccelerationInput.z, _epsilon);
  }

  [Test]
  public void Act_Ready_AddsGravity() {
    _movement.FlightPhase = Simulation.FlightPhase.Ready;
    Vector3 accelerationInput = Vector3.zero;
    Vector3 appliedAccelerationInput = _movement.Act(accelerationInput);
    Assert.AreEqual(-Constants.kGravity, appliedAccelerationInput.y, _epsilon);
  }

  [Test]
  public void Act_Boost_AddsAirDragToBoostAcceleration() {
    _movement.FlightPhase = Simulation.FlightPhase.Boost;
    _agent.Velocity = new Vector3(0, 0, 100);
    Vector3 accelerationInput = Vector3.zero;
    Vector3 appliedAccelerationInput = _movement.Act(accelerationInput);
    float boostAcceleration = 100f;

    // Calculate the expected air drag.
    // Velocity = 100 m/s, Cd = 1, area = 1 m^2, mass = 1 kg, altitude = 0 m.
    float airDensity = Constants.CalculateAirDensityAtAltitude(altitude: 0);
    float dynamicPressure = 0.5f * airDensity * 100f * 100f;
    float dragForce = 1f * dynamicPressure * 1f;
    float expectedDrag = dragForce / 1f;
    Assert.AreEqual(-expectedDrag - Constants.kGravity + boostAcceleration,
                    appliedAccelerationInput.z, _epsilon);
  }

  [Test]
  public void Act_Boost_AddsLiftInducedDragToBoostAcceleration() {
    _movement.FlightPhase = Simulation.FlightPhase.Boost;
    Vector3 accelerationInput = Vector3.zero;
    Vector3 appliedAccelerationInput = _movement.Act(accelerationInput);
    float boostAcceleration = 100f;
    Assert.AreEqual(-Constants.kGravity + boostAcceleration, appliedAccelerationInput.z, _epsilon);
  }

  [Test]
  public void Act_Boost_AddsGravity() {
    _movement.FlightPhase = Simulation.FlightPhase.Midcourse;
    Vector3 accelerationInput = Vector3.zero;
    Vector3 appliedAccelerationInput = _movement.Act(accelerationInput);
    Assert.AreEqual(-Constants.kGravity, appliedAccelerationInput.y, _epsilon);
  }

  [Test]
  public void Act_Boost_TransitionsToMidcourseAfterBoostTime() {
    _movement.FlightPhase = Simulation.FlightPhase.Boost;
    SetPrivateProperty(_agent, "ElapsedTime",
                       _agent.ElapsedTime + _agent.StaticConfig.BoostConfig.BoostTime + 1f);
    Vector3 accelerationInput = Vector3.zero;
    Assert.AreEqual(_movement.FlightPhase, Simulation.FlightPhase.Boost);
    Vector3 appliedAccelerationInput = _movement.Act(accelerationInput);
    Assert.AreEqual(_movement.FlightPhase, Simulation.FlightPhase.Midcourse);
  }

  [Test]
  public void Act_Midcourse_AddsAirDrag() {
    _movement.FlightPhase = Simulation.FlightPhase.Midcourse;
    _agent.Velocity = new Vector3(0, 0, 100);
    Vector3 accelerationInput = Vector3.zero;
    Vector3 appliedAccelerationInput = _movement.Act(accelerationInput);

    // Calculate the expected air drag.
    // Velocity = 100 m/s, Cd = 1, area = 1 m^2, mass = 1 kg, altitude = 0 m.
    float airDensity = Constants.CalculateAirDensityAtAltitude(altitude: 0);
    float dynamicPressure = 0.5f * airDensity * 100f * 100f;
    float dragForce = 1f * dynamicPressure * 1f;
    float expectedDrag = dragForce / 1f;
    Assert.AreEqual(-expectedDrag - Constants.kGravity, appliedAccelerationInput.z, _epsilon);
  }

  [Test]
  public void Act_Midcourse_AddsLiftInducedDrag() {
    _movement.FlightPhase = Simulation.FlightPhase.Midcourse;
    Vector3 accelerationInput = Vector3.zero;
    Vector3 appliedAccelerationInput = _movement.Act(accelerationInput);
    Assert.AreEqual(-Constants.kGravity, appliedAccelerationInput.z, _epsilon);
  }

  [Test]
  public void Act_MidCourse_AddsGravity() {
    _movement.FlightPhase = Simulation.FlightPhase.Midcourse;
    Vector3 accelerationInput = Vector3.zero;
    Vector3 appliedAccelerationInput = _movement.Act(accelerationInput);
    Assert.AreEqual(-Constants.kGravity, appliedAccelerationInput.y, _epsilon);
  }

  [Test]
  public void Act_Terminated_ReturnsZero() {
    _movement.FlightPhase = Simulation.FlightPhase.Terminated;
    Vector3 accelerationInput = new Vector3(1, -2, 3);
    Vector3 appliedAccelerationInput = _movement.Act(accelerationInput);
    Assert.AreEqual(Vector3.zero, appliedAccelerationInput);
  }
}
