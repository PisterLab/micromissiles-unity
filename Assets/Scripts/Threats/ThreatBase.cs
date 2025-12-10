using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Base implementation of a threat.
public abstract class ThreatBase : AgentBase, IThreat {
  public event ThreatHitMissEventHandler OnHit;
  public event ThreatHitMissEventHandler OnMiss;

  // Speed difference threshold for applying forward acceleration.
  private const float _speedErrorThreshold = 1f;

  // Power table map from the power to the speed.
  private Dictionary<Configs.Power, float> _powerTable;

  // The attack behavior determines how the threat navigates towards the asset.
  public IAttackBehavior AttackBehavior { get; set; }

  // The evasion handles how the threat behaves in the vicinity of a pursuing interceptor.
  public IEvasion Evasion { get; set; }

  public IReadOnlyDictionary<Configs.Power, float> PowerTable {
    get {
      if (_powerTable == null) {
        _powerTable =
            StaticConfig.PowerTable.ToDictionary(entry => entry.Power, entry => entry.Speed);
      }
      return _powerTable;
    }
  }

  public float LookupPowerTable(Configs.Power power) {
    PowerTable.TryGetValue(power, out float speed);
    return speed;
  }

  public void HandleIntercept() {
    OnMiss?.Invoke(this);
    Terminate();
  }

  protected override void Start() {
    base.Start();

    // The threat should target the nearest launcher.
    IHierarchical target = null;
    var launchers = IADS.Instance.Launchers;
    if (launchers.Count == 0) {
      target = new FixedHierarchical(position: Vector3.zero);
    } else {
      float minDistanceSqr = Mathf.Infinity;
      foreach (var launcher in launchers) {
        float distanceSqr = (launcher.Position - Position).sqrMagnitude;
        if (distanceSqr < minDistanceSqr) {
          minDistanceSqr = distanceSqr;
          target = launcher;
        }
      }
    }
    HierarchicalAgent.Target = target;
  }

  protected override void FixedUpdate() {
    base.FixedUpdate();

    float desiredSpeed = 0f;
    // Check whether the threat should evade any pursuer.
    IAgent closestPursuer = FindClosestPursuer();
    if (Evasion != null && closestPursuer != null && Evasion.ShouldEvade(closestPursuer)) {
      _accelerationInput = Evasion.Evade(closestPursuer);
      desiredSpeed = LookupPowerTable(Configs.Power.Max);
    } else {
      // Follow the attack behavior.
      (Vector3 waypoint, Configs.Power waypointPower) =
          AttackBehavior.GetNextWaypoint(TargetModel.Position);
      _accelerationInput = Controller?.Plan(waypoint) ?? Vector3.zero;
      desiredSpeed = LookupPowerTable(waypointPower);
    }

    // Limit the forward acceleration according to the desired speed.
    float speedError = desiredSpeed - Speed;
    Vector3 forwardAccelerationInput = Vector3.Project(_accelerationInput, transform.forward);
    Vector3 normalAccelerationInput = Vector3.ProjectOnPlane(_accelerationInput, transform.forward);
    if (Mathf.Abs(speedError) < _speedErrorThreshold) {
      _accelerationInput = normalAccelerationInput;
    } else {
      float speedFactor = Mathf.Clamp01(Mathf.Abs(speedError) / _speedErrorThreshold);
      _accelerationInput =
          normalAccelerationInput + forwardAccelerationInput * Mathf.Sign(speedError) * speedFactor;
    }

    _acceleration = Movement?.Act(_accelerationInput) ?? Vector3.zero;
    _rigidbody.AddForce(_acceleration, ForceMode.Acceleration);
  }

  protected override void UpdateAgentConfig() {
    base.UpdateAgentConfig();

    // Set the attack behavior.
    Configs.AttackBehaviorConfig attackBehaviorConfig =
        ConfigLoader.LoadAttackBehaviorConfig(AgentConfig.AttackBehaviorConfigFile ?? "");
    switch (attackBehaviorConfig?.Type) {
      case Configs.AttackType.DirectAttack: {
        AttackBehavior = new DirectAttackBehavior(this, attackBehaviorConfig);
        break;
      }
      case Configs.AttackType.PreplannedAttack:
      case Configs.AttackType.SlalomAttack: {
        Debug.LogError($"Attack behavior type {attackBehaviorConfig?.Type} is unimplemented.");
        break;
      }
      default: {
        Debug.LogError($"Attack behavior type {attackBehaviorConfig?.Type} not found.");
        break;
      }
    }

    // Set the evasion.
    Evasion = new OrthogonalEvasion(this);
  }

  private IAgent FindClosestPursuer() {
    if (HierarchicalAgent == null || HierarchicalAgent.Pursuers.Count == 0 || Sensor == null) {
      return null;
    }

    HierarchicalAgent closestAgent = null;
    float minDistance = float.MaxValue;
    foreach (var pursuer in HierarchicalAgent.Pursuers) {
      if (pursuer is HierarchicalAgent agent) {
        SensorOutput sensorOutput = Sensor.Sense(agent);
        if (sensorOutput.Position.Range < minDistance) {
          closestAgent = agent;
          minDistance = sensorOutput.Position.Range;
        }
      }
    }
    return closestAgent?.Agent;
  }

  // If the threat collides with the ground or another agent, it will be terminated. It is possible
  // for a threat to collide with another threat or with a non-pursuing interceptor. Interceptors
  // will handle colliding with a threat.
  private void OnTriggerEnter(Collider other) {
    if (CheckFloorCollision(other)) {
      OnMiss?.Invoke(this);
      Terminate();
    }

    IAgent otherAgent = other.gameObject.GetComponentInParent<IAgent>();
    // Dummy agents are virtual targets and should not trigger collisions.
    if (otherAgent == null || otherAgent is DummyAgent) {
      return;
    }
    // Check if the collision is with another threat or with the intended target.
    if (otherAgent is IThreat) {
      OnMiss?.Invoke(this);
      Terminate();
    } else if (HierarchicalAgent.Target is HierarchicalAgent targetAgent &&
               otherAgent == targetAgent.Agent) {
      OnHit?.Invoke(this);
      Terminate();
    }
  }
}
