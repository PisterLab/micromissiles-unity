using UnityEngine;

// Base implementation of an evasion.
public abstract class EvasionBase : IEvasion {
  // Determine whether to perform any evasive maneuvers.
  public abstract bool ShouldEvade(IAgent agent, IAgent pursuer);

  // Calculate the acceleration input to evade the pursuer.
  public abstract Vector3 Evade(IAgent agent, IAgent pursuer);
}
