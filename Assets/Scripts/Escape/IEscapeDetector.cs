using UnityEngine;

// Interface for an escape detector.
//
// The escape detector returns a boolean corresponding whether a target is escaping its pursuer.
// The pursuer agent owns the escape detector and uses the detector to determine whether to
// declare the target a miss.
public interface IEscapeDetector {
  IAgent Agent { get; init; }

  // Determine whether the target is escaping the agent.
  bool IsEscaping(IHierarchical target);
}
