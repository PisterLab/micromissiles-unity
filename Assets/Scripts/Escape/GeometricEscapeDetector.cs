using UnityEngine;

// Geometric escape detector.
//
// The geometric escape detector checks whether the agent is between the threat and its target.
public class GeometricEscapeDetector : EscapeDetectorBase {
  // The range buffer factor allows the interceptor to be slightly further away from the target than
  // the target is to its target before declaring an escape.
  private const float _rangeBufferFactor = 1.1f;

  public GeometricEscapeDetector(IAgent agent) : base(agent) {}

  public override bool IsEscaping(IHierarchical target) {
    if (target == null) {
      return false;
    }

    Transformation agentToTargetTransformation = Agent.GetRelativeTransformation(target);

    // Check if the target has a target of its own.
    if (target.Target != null && !target.Target.IsTerminated) {
      Vector3 targetRelativePositionToTarget = target.Target.Position - target.Position;
      // The target is escaping if the distance from the agent to the target is greater than the
      // distance from the target to its target or if the agent is moving head-on towards the
      // target.
      return agentToTargetTransformation.Position.Range >
                 targetRelativePositionToTarget.magnitude ||
             Vector3.Dot(-agentToTargetTransformation.Position.Cartesian,
                         targetRelativePositionToTarget) < 0;
    }

    // Fall back to checking the relative velocity.
    return agentToTargetTransformation.Velocity.Range < 0;
  }
}
