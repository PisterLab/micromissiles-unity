using System.Collections.Generic;
using System.Linq;

// Base implementation of a threat.
public abstract class ThreatBase : AgentBase, IThreat {
  // Power table map from the power to the speed.
  private Dictionary<Configs.Power, float> _powerTable;

  // The attack behavior determines how the threat navigates towards the asset.
  public IAttackBehavior AttackBehavior { get; set; }

  // The evasion handles how the threat behaves in the vicinity of a pursuing interceptor.
  public IEvasion Evasion { get; set; }

  public Dictionary<Configs.Power, float> PowerTable {
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
}
