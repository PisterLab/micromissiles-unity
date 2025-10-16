using UnityEngine;

// The proportional navigation controller applies an input that is proportional to the line of
// sight's rotation rate to steer the agent towards its target.
public class PnController : ControllerBase {
  // Negative closing velocity turn factor.
  private const float _negativeClosingVelocityTurnFactor = 100f;

  // Angular threshold in degrees for detecting spiral behavior.
  private const float _spiralBearingThreshold = 10f;

  // Minimum line of sight rate.
  private const float _minimumLosRate = 0.2f;

  public float Gain { get; set; }

  public PnController(IAgent agent, float gain) : base(agent) {
    Gain = gain;
  }

  // Controller-dependent implementation of the control law.
  protected override Vector3 Plan(in Transformation relativeTransformation) {
    // Cache the transform, velocity, and speed.
    var agentTransform = Agent.transform;
    var right = agentTransform.right;
    var up = agentTransform.up;
    var velocity = Agent.Velocity;
    var speed = Agent.Speed;

    // Extract the bearing and closing velocity from the relative transformation.
    var losAz = relativeTransformation.Position.Azimuth;
    var losEl = relativeTransformation.Position.Elevation;
    var losRateAz = relativeTransformation.Velocity.Azimuth;
    var losRateEl = relativeTransformation.Velocity.Elevation;
    // The closing velocity is negative because the closing velocity is opposite to the range rate.
    var closingVelocity = -relativeTransformation.Velocity.Range;

    // Set the turn factor, which is equal to the closing velocity by default.
    var turnFactor = closingVelocity;
    // Handle a negative closing velocity. In this case, since the target is moving away from the
    // agent, apply a stronger turn.
    if (closingVelocity < 0) {
      turnFactor = Mathf.Max(1f, Mathf.Abs(closingVelocity) * _negativeClosingVelocityTurnFactor);
    }

    // Handle the spiral behavior if the target is at a bearing of around 90 degrees.
    if (Mathf.Abs(Mathf.Abs(losAz) - 90f * Mathf.Deg2Rad) <
            _spiralBearingThreshold * Mathf.Deg2Rad ||
        Mathf.Abs(Mathf.Abs(losEl) - 90f * Mathf.Deg2Rad) <
            _spiralBearingThreshold * Mathf.Deg2Rad) {
      // Check that the agent is not moving in a spiral by clamping the line of sight rate.
      losRateAz = Mathf.Sign(losRateAz) * Mathf.Max(Mathf.Abs(losRateAz), _minimumLosRate);
      losRateEl = Mathf.Sign(losRateEl) * Mathf.Max(Mathf.Abs(losRateEl), _minimumLosRate);
      turnFactor = Mathf.Abs(closingVelocity) * _negativeClosingVelocityTurnFactor;
    }

    var accelerationAz = Gain * turnFactor * losRateAz;
    var accelerationEl = Gain * turnFactor * losRateEl;
    return right * accelerationAz + up * accelerationEl;
  }
}
