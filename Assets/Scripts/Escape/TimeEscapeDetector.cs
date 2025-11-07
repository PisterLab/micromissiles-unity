using UnityEngine;

// Time escape detector.
//
// The time escape detector checks whether the agent reaches the target before the target reaches
// its target based on the current respective speeds.
public class TimeEscapeDetector : EscapeDetectorBase {
  public TimeEscapeDetector(IAgent agent) : base(agent) {}

  public override bool IsEscaping(IHierarchical target) {
    if (target == null) {
      return false;
    }

    Transformation agentToTargetTransformation = Agent.GetRelativeTransformation(target);

    // Check if the target has a target of its own.
    if (target.Target != null && !target.Target.IsTerminated) {
      Vector3 targetRelativePositionToTarget = target.Target.Position - target.Position;
      // Check whether the agent is moving head-on towards the target.
      if (Vector3.Dot(-agentToTargetTransformation.Position.Cartesian,
                      targetRelativePositionToTarget) > 0) {
        // Check the time-to-hit for the agent and the target.
        float targetTimeToHit = targetRelativePositionToTarget.magnitude / target.Speed;
        float agentTimeToHit = agentToTargetTransformation.Position.Range / Agent.Speed;
        return targetTimeToHit < agentTimeToHit;
      }
      // If the agent is chasing the tail of the target, check whether its speed is greater than the
      // target's speed.
      return target.Speed > Agent.Speed;
    }

    // Fall back to checking the relative velocity.
    return agentToTargetTransformation.Velocity.Range < 0;
  }
}
