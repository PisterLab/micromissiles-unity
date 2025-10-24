using System;
using UnityEngine;

// The proportional navigation controller applies proportional navigation to steer the agent towards
// its target.
public class PnControllerLegacy : IControllerLegacy {
  // Negative closing velocity turn factor.
  protected const float negativeClosingVelocityTurnFactor = 100f;

  // Minimum line-of-sight rate.
  protected const float minimumLosRate = 0.2f;

  // Proportional navigation gain.
  protected float _navigationGain;

  public PnControllerLegacy(Agent agent, float navigationGain) : base(agent) {
    _navigationGain = navigationGain;
  }

  protected override Vector3 PlanImpl(in Transformation relativeTransformation) {
    // Cache the transform and velocity.
    Transform agentTransform = _agent.transform;
    Vector3 right = agentTransform.right;
    Vector3 up = agentTransform.up;
    Vector3 forward = agentTransform.forward;
    Vector3 position = agentTransform.position;

    Vector3 velocity = _agent.GetVelocity();
    float speed = velocity.magnitude;

    // Extract the bearing and closing velocity from the relative transformation.
    float losAz = relativeTransformation.Position.Azimuth;
    float losEl = relativeTransformation.Position.Elevation;
    float losRateAz = relativeTransformation.Velocity.Azimuth;
    float losRateEl = relativeTransformation.Velocity.Elevation;
    // The closing velocity is negative because the closing velocity is opposite to the range rate.
    float closingVelocity = -relativeTransformation.Velocity.Range;

    // Set the turn factor, which is equal to the closing velocity by default.
    float turnFactor = closingVelocity;
    // Handle a negative closing velocity. In this case, since the target is moving away from the
    // agent, apply a stronger turn.
    if (closingVelocity < 0) {
      turnFactor = Mathf.Max(1f, Mathf.Abs(closingVelocity) * negativeClosingVelocityTurnFactor);
    }

    // Handle the spiral behavior if the target is at a bearing of 90 degrees +- 10 degrees.
    if (Mathf.Abs(Mathf.Abs(losAz) - 90f * Mathf.Deg2Rad) < 10f * Mathf.Deg2Rad ||
        Mathf.Abs(Mathf.Abs(losEl) - 90f * Mathf.Deg2Rad) < 10f * Mathf.Deg2Rad) {
      // Check that the agent is not moving in a spiral by clamping the LOS rate.
      losRateAz = Mathf.Sign(losRateAz) * Mathf.Max(Mathf.Abs(losRateAz), minimumLosRate);
      losRateEl = Mathf.Sign(losRateEl) * Mathf.Max(Mathf.Abs(losRateEl), minimumLosRate);
      turnFactor = Mathf.Abs(closingVelocity) * negativeClosingVelocityTurnFactor;
    }

    float accelerationAz = _navigationGain * turnFactor * losRateAz;
    float accelerationEl = _navigationGain * turnFactor * losRateEl;
    Vector3 accelerationInput = right * accelerationAz + up * accelerationEl;
    return accelerationInput;
  }
}
