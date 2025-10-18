// Base implementation of a threat.
public abstract class ThreatBase : AgentBase, IThreat {
  // The attack behavior determines how the threat navigates towards the asset.
  public IAttackBehavior AttackBehavior { get; set; }

  // The evasion handles how the threat behaves in the vicinity of a pursuing interceptor.
  public IEvasion Evasion { get; set; }
}
