using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixedWingThreat : Threat {
  [SerializeField]
  private float _navigationGain = 50f;

  private Vector3 _accelerationInput;
  private double _elapsedTime = 0;
  private Rigidbody _rigidbody;

  protected override void Start() {
    base.Start();
    _rigidbody = GetComponent<Rigidbody>();
  }

  protected override void FixedUpdate() {
    base.FixedUpdate();
  }

  protected override void UpdateReady(double deltaTime) {}

  protected override void UpdateBoost(double deltaTime) {}

  protected override void UpdateMidCourse(double deltaTime) {
    _elapsedTime += deltaTime;
    Vector3 accelerationInput = Vector3.zero;

    if (ShouldEvade()) {
      accelerationInput = EvadeInterceptor(GetClosestInterceptor());
    } else if (HasAssignedTarget()) {
      // Update the waypoint and power setting.
      UpdateWaypointAndPower();

      float sensorUpdatePeriod = 1f / agentConfig.DynamicConfig.SensorConfig.Frequency;
      if (_elapsedTime >= sensorUpdatePeriod) {
        // TODO: Implement guidance filter to estimate the state from sensor output.
        _elapsedTime = 0;
      }

      // Calculate the normal acceleration input using proportional navigation.
      Vector3 normalAcceleration = CalculateAccelerationInput();

      // Adjust the speed based on the power setting.
      Vector3 forwardAcceleration = CalculateForwardAcceleration();

      // Combine the accelerations.
      accelerationInput = normalAcceleration + forwardAcceleration;
    }

    // Calculate and set the total acceleration.
    Vector3 acceleration = CalculateAcceleration(accelerationInput);
    _rigidbody.AddForce(acceleration, ForceMode.Acceleration);
  }

  private void UpdateWaypointAndPower() {
    // Get the next waypoint and power setting from the attack behavior.
    // TODO: Implement support for sensors to update the track on the target position.
    (_currentWaypoint, _currentPower) =
        _attackBehavior.GetNextWaypoint(transform.position, _target.transform.position);
  }

  private Vector3 CalculateAccelerationInput() {
    // Cache the transform and velocity.
    Transform threatTransform = transform;
    Vector3 right = threatTransform.right;
    Vector3 forward = threatTransform.forward;
    Vector3 position = threatTransform.position;
    Vector3 velocity = GetVelocity();
    float speed = velocity.magnitude;

    IController controller = new PnController(this, _navigationGain);
    Vector3 accelerationInput = controller.PlanToWaypoint(_currentWaypoint);

    // Counter gravity as much as possible.
    accelerationInput +=
        (float)Constants.kGravity / Vector3.Dot(transform.up, Vector3.up) * transform.up;

    // Clamp the normal acceleration input to the maximum normal acceleration.
    float maxNormalAcceleration = CalculateMaxNormalAcceleration();
    accelerationInput = Vector3.ClampMagnitude(accelerationInput, maxNormalAcceleration);

    // Avoid the ground when close to the surface and too low on the glideslope.
    float altitude = position.y;
    // Sink rate is opposite to climb rate.
    float sinkRate = -velocity.y;
    float distanceToTarget = (_currentWaypoint - position).magnitude;
    float groundProximityThreshold = Mathf.Abs(sinkRate) * 5f;  // Adjust threshold as necessary.
    if (sinkRate > 0 && altitude / sinkRate < distanceToTarget / speed) {
      // Evade upward normal to the velocity.
      Vector3 upwardsDirection = Vector3.Cross(forward, right);

      // Blend between the calculated acceleration input and the upward acceleration.
      float blendFactor = 1 - (altitude / groundProximityThreshold);
      accelerationInput.y =
          Vector3
              .Lerp(accelerationInput, upwardsDirection * CalculateMaxNormalAcceleration(),
                    blendFactor)
              .y;
    }

    accelerationInput = Vector3.ClampMagnitude(accelerationInput, maxNormalAcceleration);
    _accelerationInput = accelerationInput;
    return accelerationInput;
  }

  // Optional: Add this method to visualize debug information.
  protected virtual void OnDrawGizmos() {
    if (Application.isPlaying) {
      Gizmos.color = Color.yellow;
      Gizmos.DrawLine(transform.position, _currentWaypoint);

      Gizmos.color = Color.green;
      Gizmos.DrawRay(transform.position, _accelerationInput);
    }
  }
}
