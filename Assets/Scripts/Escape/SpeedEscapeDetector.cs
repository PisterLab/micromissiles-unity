using UnityEngine;

// Speed escape detector.
//
// The speed escape detector checks whether the agent has a speed greater than the threat's when it
// has navigated to the threat's current position.
public class SpeedEscapeDetector : EscapeDetectorBase {
  public SpeedEscapeDetector(IAgent agent) : base(agent) {}

  public override bool IsEscaping(IHierarchical target) {
    if (target == null) {
      return false;
    }

    float predictedAgentSpeed = CalculatePredictedAgentSpeed(target.Position);
    return predictedAgentSpeed <= target.Speed;
  }

  // Calculate the predicted agent speed when it has reached the target's current position.
  private float CalculatePredictedAgentSpeed(in Vector3 targetPosition) {
    float fractionalSpeed = FractionalSpeed.Calculate(Agent, targetPosition);
    return fractionalSpeed * Agent.Speed;
  }
}
