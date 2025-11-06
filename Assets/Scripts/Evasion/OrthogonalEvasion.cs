using UnityEngine;

// The agent will try to evade its pursuer by turning its velocity vector to be orthogonal to the
// pursuer's velocity vector.
//
// Turning orthogonal to the pursuer means that the pursuer needs to apply a normal acceleration to
// intercept the agent, incurring the maximum possible lift-induced drag.
public class OrthogonalEvasion : EvasionBase {
  public OrthogonalEvasion(IAgent agent) : base(agent) {}

  // Determine whether to perform any evasive maneuvers.
  public override bool ShouldEvade(IAgent pursuer) {
    if (!(Agent.AgentConfig?.DynamicConfig?.FlightConfig?.EvasionConfig?.Enabled ?? false)) {
      return false;
    }

    SensorOutput sensorOutput = Agent.Sensor.Sense(pursuer);
    float evasionRangeThreshold =
        Agent.AgentConfig.DynamicConfig.FlightConfig.EvasionConfig.RangeThreshold;
    return sensorOutput.Position.Range <= evasionRangeThreshold && sensorOutput.Velocity.Range < 0;
  }

  // Calculate the acceleration input to evade the pursuer.
  public override Vector3 Evade(IAgent pursuer) {
    const float epsilon = 1e-6f;
    const float groundProximityThresholdFactor = 5f;
    const float groundAvoidanceUpFactor = 5f;

    Vector3 agentPosition = Agent.Position;
    Vector3 agentVelocity = Agent.Velocity;
    Vector3 pursuerPosition = pursuer.Position;
    Vector3 pursuerVelocity = pursuer.Velocity;

    // Evade the pursuer by turning the velocity to be orthogonal to the pursuer's velocity.
    Vector3 normalVelocity = Vector3.ProjectOnPlane(agentVelocity, pursuerVelocity);
    if (normalVelocity.sqrMagnitude < epsilon) {
      // If the agent's velocity is aligned with the pursuer's velocity, choose a random normal
      // direction in which to evade.
      normalVelocity = pursuer.transform.right;
    }
    // If the agent's velocity is aligned with the normal velocity, i.e., orthogonal to the
    // pursuer's velocity, then the normal acceleration should be zero as the agent should continue
    // in the same direction.
    Vector3 normalAccelerationDirection =
        Vector3.ProjectOnPlane(normalVelocity, agentVelocity).normalized;

    // Turn away from the pursuer.
    Vector3 relativePosition = pursuerPosition - agentPosition;
    if (Vector3.Dot(relativePosition, normalAccelerationDirection) > 0) {
      normalAccelerationDirection *= -1;
    }

    // Avoid evading straight down when near the ground.
    float altitude = agentPosition.y;
    float groundProximityThreshold = Mathf.Abs(agentVelocity.y) * groundProximityThresholdFactor;
    if (agentVelocity.y < 0 && altitude < groundProximityThreshold) {
      // Determine the evasion direction based on the angle to pursuer.
      float angle = Vector3.SignedAngle(Agent.transform.forward, relativePosition, Vector3.up);

      // Choose the direction that leads away from the pursuer.
      Vector3 rightDirection = Agent.transform.right;
      Vector3 bestHorizontalDirection = angle > 0 ? -rightDirection : rightDirection;

      // Blend between horizontal evasion and slight upward movement.
      float blendFactor = 1 - (altitude / groundProximityThreshold);
      normalAccelerationDirection =
          Vector3
              .Lerp(normalAccelerationDirection,
                    bestHorizontalDirection + Agent.transform.up * groundAvoidanceUpFactor,
                    blendFactor)
              .normalized;
    }
    Vector3 normalAcceleration = normalAccelerationDirection * Agent.MaxNormalAcceleration();

    // Apply the maximum forward acceleration.
    Vector3 forwardAcceleration = Agent.transform.forward * Agent.MaxForwardAcceleration();
    return normalAcceleration + forwardAcceleration;
  }
}
