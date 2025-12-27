using System.Collections.Generic;

// Interface for a threat.
//
// Threats attempt to reach the asset while avoiding the defending interceptors.

public delegate void ThreatHitMissEventHandler(IThreat threat);

public interface IThreat : IAgent {
  // The OnHit event handler is called when the threat reaches its intended target, usually the
  // asset.
  event ThreatHitMissEventHandler OnHit;
  // The OnMiss event handler is called when the threat is destroyed prior to reaching its intended
  // target.
  event ThreatHitMissEventHandler OnMiss;

  IAttackBehavior AttackBehavior { get; set; }
  IEvasion Evasion { get; set; }

  IReadOnlyDictionary<Configs.Power, float> PowerTable { get; }

  float LookupPowerTable(Configs.Power power);

  // This function is called when the threat has been intercepted.
  void HandleIntercept();
}
