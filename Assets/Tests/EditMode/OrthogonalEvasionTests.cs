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
    _agent.StaticConfig = new Configs.StaticConfig() {
      AccelerationConfig =
          new Configs.AccelerationConfig() {
            MaxForwardAcceleration = 10,
            MaxReferenceNormalAcceleration = 5 / Constants.kGravity,
            ReferenceSpeed = 100,
          },
    };
    _agent.AgentConfig = new Configs.AgentConfig() {
      DynamicConfig =
          new Configs.DynamicConfig() {
            FlightConfig = new Configs.FlightConfig() { EvasionConfig =
                                                            new Configs.EvasionConfig() {
                                                              Enabled = true,
                                                              RangeThreshold = 1000,
                                                            } },
          },
    };
    Rigidbody agentRb = _agent.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(_agent, "Awake");
    _agent.Sensor = new IdealSensor(_agent);
    _pursuer = new GameObject("Agent").AddComponent<AgentBase>();
    Rigidbody pursuerRb = _pursuer.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(_pursuer, "Awake");
    _evasion = new OrthogonalEvasion(_agent);
  }

  [Test]
  public void ShouldEvade_EvasionDisabled_ReturnsFalse() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 2, 0);
    _pursuer.Position = new Vector3(0, 100, 0);
    _pursuer.Velocity = new Vector3(0, -1, 0);
    _agent.AgentConfig.DynamicConfig.FlightConfig.EvasionConfig.Enabled = false;
    Assert.IsFalse(_evasion.ShouldEvade(_pursuer));
  }

  [Test]
  public void ShouldEvade_OutsideRange_ReturnsFalse() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 2, 0);
    _pursuer.Position = new Vector3(0, 2000, 0);
    _pursuer.Velocity = new Vector3(0, -1, 0);
    Assert.IsFalse(_evasion.ShouldEvade(_pursuer));
  }

  [Test]
  public void ShouldEvade_MovingAway_ReturnsFalse() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 1, 0);
    _pursuer.Position = new Vector3(0, 500, 0);
    _pursuer.Velocity = new Vector3(0, 2, 0);
    Assert.IsFalse(_evasion.ShouldEvade(_pursuer));
  }

  [Test]
  public void Evade_NonZeroNormalAcceleration() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(1, 2, 0);
    _pursuer.Position = new Vector3(0, 100, 0);
    _pursuer.Velocity = new Vector3(0, -1, 0);
    Assert.AreEqual(0, Vector3.Dot(_evasion.Evade(_pursuer), _agent.Velocity));
  }

  [Test]
  public void Evade_OrthogonalToPursuerVelocity() {
    _agent.Position = new Vector3(0, 0, 0);
    _agent.Velocity = new Vector3(0, 2, 0);
    _pursuer.Position = new Vector3(0, 100, 0);
    _pursuer.Velocity = new Vector3(0, -1, 0);
    Assert.AreEqual(0, Vector3.Dot(_evasion.Evade(_pursuer), _pursuer.Velocity));
  }
}
