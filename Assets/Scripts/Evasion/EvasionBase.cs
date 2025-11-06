using UnityEngine;

// Base implementation of an evasion.
public abstract class EvasionBase : IEvasion {
  // Agent that will evade the pursuers.
  public IAgent Agent { get; set; }

  public EvasionBase(IAgent agent) {
    Agent = agent;
  }

  // Determine whether to perform any evasive maneuvers.
  public abstract bool ShouldEvade(IAgent pursuer);

  // Calculate the acceleration input to evade the pursuer.
  public abstract Vector3 Evade(IAgent pursuer);
}
