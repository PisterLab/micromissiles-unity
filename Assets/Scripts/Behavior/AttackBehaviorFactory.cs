public static class AttackBehaviorFactory {
  public static AttackBehavior Create(in Configs.AttackBehaviorConfig config) {
    return config.Type switch {
      Configs.AttackType.DirectAttack => new DirectAttackBehaviorLegacy(config),
      Configs.AttackType.PreplannedAttack => null,
      Configs.AttackType.SlalomAttack => null,
      _ => null,
    };
  }
}
