using NUnit.Framework;
using UnityEngine;

public class OrthogonalEvasionTests : TestBase {
  private const float _epsilon = 1e-3f;

  private AgentBase _agent;
  private AgentBase _pursuer;
  private OrthogonalEvasion _evasion;

  [SetUp]
  public void SetUp() {
    _agent = new GameObject("Agent").AddComponent<AgentBase>();
    _agent.gameObject.AddComponent<Rigidbody>();
    _agent.Transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, 1));
    InvokePrivateMethod(_agent, "Awake");
    _agent.StaticConfig = new Configs.StaticConfig() {
      AccelerationConfig =
          new Configs.AccelerationConfig() {
            MaxForwardAcceleration = 10,
            MaxReferenceNormalAcceleration = 5 / Constants.kGravity,
            ReferenceSpeed = 1,
          },
    };
    _agent.AgentConfig = new Configs.AgentConfig() {
      DynamicConfig =
          new Configs.DynamicConfig() {
            FlightConfig =
                new Configs.FlightConfig() {
                  EvasionConfig =
                      new Configs.EvasionConfig() {
                        Enabled = true,
                        RangeThreshold = 1000,
                      },
                },
            SensorConfig =
                new Simulation.SensorConfig() {
                  Type = Simulation.SensorType.Ideal,
                  Frequency = 100,
                },
          },
    };
    _agent.Sensor = new IdealSensor(_agent);
    _pursuer = new GameObject("Agent").AddComponent<AgentBase>();
    _pursuer.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(_pursuer, "Awake");
    _evasion = new OrthogonalEvasion(_agent);
  }

  [Test]
  public void ShouldEvade_EvasionDisabled_ReturnsFalse() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 0, 2);
    _pursuer.Position = new Vector3(0, 0, 100);
    _pursuer.Velocity = new Vector3(0, 0, -1);
    _agent.AgentConfig.DynamicConfig.FlightConfig.EvasionConfig.Enabled = false;
    Assert.IsFalse(_evasion.ShouldEvade(_pursuer));
  }

  [Test]
  public void ShouldEvade_OutsideRange_ReturnsFalse() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 0, 2);
    _pursuer.Position = new Vector3(0, 0, 2000);
    _pursuer.Velocity = new Vector3(0, 0, -1);
    Assert.IsFalse(_evasion.ShouldEvade(_pursuer));
  }

  [Test]
  public void ShouldEvade_MovingAway_ReturnsFalse() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 0, 1);
    _pursuer.Position = new Vector3(0, 0, 500);
    _pursuer.Velocity = new Vector3(0, 0, 2);
    Assert.IsFalse(_evasion.ShouldEvade(_pursuer));
  }

  [Test]
  public void Evade_AppliesMaxNormalAcceleration() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 0, 2);
    _pursuer.Position = new Vector3(0, 0, 100);
    _pursuer.Velocity = new Vector3(1, 0, -1);
    Vector3 accelerationInput = _evasion.Evade(_pursuer);
    Vector3 normalAccelerationInput =
        Vector3.ProjectOnPlane(accelerationInput, _agent.Transform.forward);
    Assert.AreEqual(_agent.MaxNormalAcceleration(), normalAccelerationInput.magnitude, _epsilon);
  }

  [Test]
  public void Evade_AppliesMaxForwardAcceleration() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 0, 2);
    _pursuer.Position = new Vector3(0, 0, 100);
    _pursuer.Velocity = new Vector3(1, 0, -1);
    Vector3 accelerationInput = _evasion.Evade(_pursuer);
    Vector3 forwardAccelerationInput = Vector3.Project(accelerationInput, _agent.Transform.forward);
    Assert.AreEqual(_agent.MaxForwardAcceleration(), forwardAccelerationInput.magnitude, _epsilon);
  }

  [Test]
  public void Evade_AgentAndPursuerAlignedVelocities_OrthogonalToPursuerVelocity() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 0, 100);
    _pursuer.Position = new Vector3(0, 0, 100);
    _pursuer.Velocity = _agent.Velocity;
    Vector3 accelerationInput = _evasion.Evade(_pursuer);
    Vector3 normalAccelerationInput =
        Vector3.ProjectOnPlane(accelerationInput, _agent.Transform.forward);
    Assert.AreNotEqual(Vector3.zero, normalAccelerationInput);
    Assert.AreEqual(0, Vector3.Dot(normalAccelerationInput, _pursuer.Velocity));
  }

  [Test]
  public void Evade_AgentAndPursuerOrthogonalVelocities_AppliesNoNormalAcceleration() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 0, 100);
    _pursuer.Position = new Vector3(100, 0, 0);
    _pursuer.Velocity = new Vector3(-100, 0, 0);
    Vector3 accelerationInput = _evasion.Evade(_pursuer);
    Vector3 normalAccelerationInput =
        Vector3.ProjectOnPlane(accelerationInput, _agent.Transform.forward);
    Assert.AreEqual(Vector3.zero, normalAccelerationInput);
  }
}
