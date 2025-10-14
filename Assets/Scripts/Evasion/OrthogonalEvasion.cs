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

    var evasionRangeThreshold =
        Agent.AgentConfig?.DynamicConfig?.FlightConfig?.EvasionConfig?.RangeThreshold ??
        defaultEvasionRangeThreshold;
    var sensorOutput = Agent.Sensor.Sense(pursuer);
    return sensorOutput.Position.Range <= evasionRangeThreshold && sensorOutput.Velocity.Range < 0;
  }

  // Calculate the acceleration input to evade the pursuer.
  public override Vector3 Evade(IAgent pursuer) {
    const float epsilon = 1e-12f;
    const float groundProximityThresholdFactor = 5f;
    const float groundAvoidanceUpFactor = 5f;

    var agentPosition = Agent.Position;
    var agentVelocity = Agent.Velocity;
    var pursuerPosition = pursuer.Position;
    var pursuerVelocity = pursuer.Velocity;

    // Evade the pursuer by turning the velocity to be orthogonal to the pursuer's velocity.
    var normalVelocity = Vector3.ProjectOnPlane(agentVelocity, pursuerVelocity);
    if (normalVelocity.sqrMagnitude < epsilon) {
      // If the agent's velocity is aligned with the pursuer's velocity, choose a random normal
      // direction in which to evade.
      normalVelocity = pursuer.transform.right;
    }
    var normalAccelerationDirection =
        Vector3.ProjectOnPlane(normalVelocity, agentVelocity).normalized;

    // Turn away from the pursuer.
    var relativePosition = pursuerPosition - agentPosition;
    if (Vector3.Dot(relativePosition, normalAccelerationDirection) > 0) {
      normalAccelerationDirection *= -1;
    }

    // Avoid evading straight down when near the ground.
    float altitude = agentPosition.y;
    float groundProximityThreshold = Mathf.Abs(agentVelocity.y) * groundProximityThresholdFactor;
    if (agentVelocity.y < 0 && altitude < groundProximityThreshold) {
      // Determine the evasion direction based on the angle to pursuer.
      var rightDirection = Vector3.Cross(Vector3.up, Agent.transform.forward);
      var angle = Vector3.SignedAngle(Agent.transform.forward, relativePosition, Vector3.up);

      // Choose the direction that leads away from the pursuer.
      var bestHorizontalDirection = angle > 0 ? -rightDirection : rightDirection;

      // Blend between horizontal evasion and slight upward movement.
      var blendFactor = 1 - (altitude / groundProximityThreshold);
      normalAccelerationDirection =
          Vector3
              .Lerp(normalAccelerationDirection,
                    bestHorizontalDirection + Agent.transform.up * groundAvoidanceUpFactor,
                    blendFactor)
              .normalized;
    }
    var normalAcceleration = normalAccelerationDirection * Agent.MaxNormalAcceleration();

    // Set the forward acceleration.
    var forwardAcceleration = Vector3.zero;
    if (Agent is IThreat threat) {
      // Set the forward acceleration to reach the target speed within one time step.
      var targetSpeed = threat.LookupPowerTable(Configs.Power.Max);
      var speedError = targetSpeed - Agent.Speed;
      forwardAcceleration = speedError / Time.fixedDeltaTime * Agent.transform.forward;
    }
    return normalAcceleration + forwardAcceleration;
  }
}
