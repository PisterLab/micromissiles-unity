// Base implementation of a threat.
public abstract class ThreatBase : AgentBase, IThreat {
  // The attack behavior determines how the threat navigates towards the asset.
  private IAttackBehavior _attackBehavior;

  // The evasion handles how the threat behaves in the vicinity of a pursuing interceptor.
  private IEvasion _evasion;

  public IAttackBehavior AttackBehavior {
    get => _attackBehavior;
    set => _attackBehavior = value;
  }
  public IEvasion Evasion {
    get => _evasion;
    set => _evasion = value;
  }
}
