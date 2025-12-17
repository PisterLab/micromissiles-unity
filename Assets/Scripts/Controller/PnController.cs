using UnityEngine;

// The proportional navigation controller applies an input that is proportional to the line of
// sight's rotation rate to steer the agent towards its target.
// The controller uses pure proportional navigation, where the acceleration input is normal to
// the agent's velocity.
// Refer to "Basic Principles of Homing Guidance" by N. F. Palumbo (2010) for more information.
public class PnController : ControllerBase {
  private const float _epsilon = 1e-3f;

  // Turn factor for a stronger turn.
  private const float _strongTurnFactor = 100f;

  // Angular threshold in degrees for determining whether the target is abeam to the agent and
  // preventing spiral behavior.
  private const float _abeamThreshold = 0.2f;

  // Minimum line-of-sight rate.
  private const float _minLosRate = 0.2f;

  public float Gain { get; set; }

  public PnController(IAgent agent, float gain) : base(agent) {
    Gain = gain;
  }

  // Controller-dependent implementation of the control law.
  protected override Vector3 Plan(in Transformation relativeTransformation) {
    Vector3 relativePosition = relativeTransformation.Position.Cartesian;
    Vector3 relativeVelocity = relativeTransformation.Velocity.Cartesian;
    Vector3 normalRelativeVelocity = Vector3.ProjectOnPlane(relativeVelocity, relativePosition);

    float distance = relativeTransformation.Position.Range;
    if (distance < _epsilon) {
      return Vector3.zero;
    }

    // Line-of-sight unit vector.
    Vector3 losRHat = relativePosition / distance;
    // Line-of-sight rate vector.
    Vector3 losVHat = normalRelativeVelocity / distance;
    Vector3 losRotation = Vector3.Cross(losRHat, losVHat);

    // The closing velocity is negative because the closing velocity is opposite to the range rate.
    float closingVelocity = -relativeTransformation.Velocity.Range;
    float closingSpeed = Mathf.Abs(closingVelocity);
    // Set the turn factor, which is equal to the closing velocity by default.
    float turnFactor = closingVelocity;
    // Handle a negative closing velocity. If the target is moving away from the agent, negate the
    // turn factor and apply a stronger turn as the agent most likely passed the target already and
    // should turn around.
    if (closingVelocity < 0) {
      turnFactor = Mathf.Max(1f, closingSpeed) * _strongTurnFactor;
    }
    // If the target is abeam to the agent, apply a stronger turn and clamp the line-of-sight rate
    // to avoid spiral behavior.
    bool isAbeam = Mathf.Abs(Vector3.Dot(Agent.Velocity.normalized, relativePosition.normalized)) <
                   _abeamThreshold;
    if (isAbeam) {
      turnFactor = Mathf.Max(1f, closingSpeed) * _strongTurnFactor;
      float clampedLosRate = Mathf.Max(losRotation.magnitude, _minLosRate);
      losRotation = losRotation.normalized * clampedLosRate;
    }
    return Gain * turnFactor * Vector3.Cross(losRotation, Agent.Velocity.normalized);
  }
}
