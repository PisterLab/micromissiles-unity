using System.Collections.Generic;

// Interface for a threat.
//
// Threats attempt to reach the asset while avoiding the defending interceptors.
public interface IThreat {
  IAttackBehavior AttackBehavior { get; set; }
  IEvasion Evasion { get; set; }

  IReadOnlyDictionary<Configs.Power, float> PowerTable { get; }

  float LookupPowerTable(Configs.Power power);
}
