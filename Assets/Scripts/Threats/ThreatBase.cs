using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Base implementation of a threat.
public abstract class ThreatBase : AgentBase, IThreat {
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

  protected override void Awake() {
    base.Awake();
  }

  protected override void FixedUpdate() {
    base.FixedUpdate();

    Vector3 accelerationInput = Vector3.zero;
    Configs.Power power = Configs.Power.Idle;
    // Check whether the threat should evade any pursuer.
    IAgent closestPursuer = FindClosestPursuer();
    if (Evasion != null && closestPursuer != null && Evasion.ShouldEvade(closestPursuer)) {
      accelerationInput = Evasion.Evade(closestPursuer);
    } else {
      // Follow the attack behavior.
      (Vector3 waypoint, Configs.Power waypointPower) =
          AttackBehavior.GetNextWaypoint(TargetModel.Position);
      power = waypointPower;
      accelerationInput = Controller?.Plan(waypoint) ?? Vector3.zero;
    }

    Vector3 acceleration = Movement?.Act(accelerationInput) ?? Vector3.zero;
    _rigidbody.AddForce(acceleration, ForceMode.Acceleration);
  }

  protected override void UpdateAgentConfig() {
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
    if (HierarchicalAgent == null || HierarchicalAgent.Pursuers.Count == 0) {
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
    return closestAgent?.Agent ?? null;
  }
}
