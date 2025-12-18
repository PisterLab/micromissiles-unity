using UnityEngine;

// No evasion will be performed.
public class NoEvasion : EvasionBase {
  public NoEvasion(IAgent agent) : base(agent) {}

  // Determine whether to perform any evasive maneuvers.
  public override bool ShouldEvade(IAgent pursuer) {
    return false;
  }

  // Calculate the acceleration input to evade the pursuer.
  public override Vector3 Evade(IAgent pursuer) {
    return Vector3.zero;
  }
}
