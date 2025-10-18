using UnityEngine;

// Interface for an evasion.
//
// When a threat detects a pursuing interceptor in its vicinity, it may attempt to evade the
// interceptor before resuming its attack behavior.
public interface IEvasion {
  // Determine whether to perform any evasive maneuvers.
  bool ShouldEvade(IAgent agent, IAgent pursuer);

  // Calculate the acceleration input to evade the pursuer.
  Vector3 Evade(IAgent agent, IAgent pursuer);
}
