// Interface for a threat.
//
// Threats attempt to reach the asset while avoiding the defending interceptors.
public interface IThreat {
  IAttackBehavior AttackBehavior { get; set; }
  IEvasion Evasion { get; set; }
}
