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
    // Default evasion range threshold in meters.
    const float defaultEvasionRangeThreshold = 1000f;

    if (!(Agent.AgentConfig?.DynamicConfig?.FlightConfig?.EvasionConfig?.Enabled ?? false)) {
      return false;
    }

    float evasionRangeThreshold =
        Agent.AgentConfig?.DynamicConfig?.FlightConfig?.EvasionConfig?.RangeThreshold ??
        defaultEvasionRangeThreshold;
    SensorOutput sensorOutput = Agent.Sensor.Sense(pursuer);
    return sensorOutput.Position.Range <= evasionRangeThreshold && sensorOutput.Velocity.Range < 0;
  }

  // Calculate the acceleration input to evade the pursuer.
  public override Vector3 Evade(IAgent pursuer) {
    const float epsilon = 1e-12f;
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

    // Set the forward acceleration.
    Vector3 forwardAcceleration = Vector3.zero;
    if (Agent is IThreat threat) {
      // Set the forward acceleration to reach the target speed within one time step.
      float targetSpeed = threat.LookupPowerTable(Configs.Power.Max);
      float speedError = targetSpeed - Agent.Speed;
      forwardAcceleration = speedError / Time.fixedDeltaTime * Agent.transform.forward;
    }
    return normalAcceleration + forwardAcceleration;
  }
}
