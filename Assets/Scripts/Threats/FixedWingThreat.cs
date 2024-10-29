using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixedWingThreat : Threat {
  [SerializeField]
  private float _navigationGain = 50f;

  private Vector3 _accelerationInput;
  private double _elapsedTime = 0;
  private Rigidbody _rigidbody;

  // Start is called before the first frame update
  protected override void Start() {
    base.Start();
    _rigidbody = GetComponent<Rigidbody>();
  }

  // Update is called once per frame
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
      // Update waypoint and power setting
      UpdateWaypointAndPower();

      float sensorUpdatePeriod = 1f / dynamicAgentConfig.dynamic_config.sensor_config.frequency;
      if (_elapsedTime >= sensorUpdatePeriod) {
        // TODO: Implement guidance filter to estimate state from sensor output
        // For now, we'll use the threat's actual state
        _sensorOutput = _sensor.SenseWaypoint(_currentWaypoint);
        _elapsedTime = 0;
      }

      // Calculate the normal acceleration input using PN
      Vector3 normalAcceleration = CalculateAccelerationInput(_sensorOutput);

      // Adjust the speed based on power setting
      Vector3 forwardAcceleration = CalculateForwardAcceleration();

      // Combine the accelerations
      accelerationInput = normalAcceleration + forwardAcceleration;
    }

    // Calculate and set the total acceleration
    Vector3 acceleration = CalculateAcceleration(accelerationInput);
    _rigidbody.AddForce(acceleration, ForceMode.Acceleration);
  }

  private void UpdateWaypointAndPower() {
    // Get the next waypoint and power setting from the attack behavior
    // TODO: Implement support for SENSORS to update the track on the target position
    (_currentWaypoint, _currentPowerSetting) =
        _attackBehavior.GetNextWaypoint(transform.position, _target.transform.position);
  }

  private Vector3 CalculateAccelerationInput(SensorOutput sensorOutput) {
    // TODO(titan): Refactor all controller-related code into a separate controller interface with
    // subclasses. A controller factory will instantiate the correct controller for each dynamic
    // configuration.

    // Implement (Augmented) Proportional Navigation guidance law
    Vector3 accelerationInput = Vector3.zero;
    // Cache the transform and velocity to avoid repeated calls to GetTransform
    // which can be costly at these large scales
    Transform currentTransform = transform;
    Vector3 transformRight = currentTransform.right;
    Vector3 transformUp = currentTransform.up;
    Vector3 transformForward = currentTransform.forward;
    Vector3 transformPosition = currentTransform.position;
    Vector3 transformVelocity = GetVelocity();
    float transformSpeed = transformVelocity.magnitude;
    // Extract relevant information from sensor output
    float losRateAz = sensorOutput.velocity.azimuth;
    float losRateEl = sensorOutput.velocity.elevation;
    float closingVelocity =
        -sensorOutput.velocity
             .range;  // Negative because closing velocity is opposite to range rate

    // Navigation gain (adjust as needed)
    float N = _navigationGain;
    // Normal PN guidance for positive closing velocity
    float turnFactor = closingVelocity;
    // Handle negative closing velocity scenario
    if (closingVelocity < 0) {
      // Target is moving away, apply stronger turn
      turnFactor = Mathf.Max(1f, Mathf.Abs(closingVelocity) * 100f);
    }
    // Handle spiral behavior if the target is at a bearing of 90 degrees +- 10 degrees
    if (Mathf.Abs(Mathf.Abs(sensorOutput.position.azimuth) - Mathf.PI / 2) < 0.2f ||
        Mathf.Abs(Mathf.Abs(sensorOutput.position.elevation) - Mathf.PI / 2) < 0.2f) {
      // Check that the agent is not moving in a spiral by clamping the LOS rate at 0.2 rad/s
      float minLosRate = 0.2f;  // Adjust as necessary
      losRateAz = Mathf.Sign(losRateAz) * Mathf.Max(Mathf.Abs(losRateAz), minLosRate);
      losRateEl = Mathf.Sign(losRateEl) * Mathf.Max(Mathf.Abs(losRateEl), minLosRate);
      turnFactor = Mathf.Abs(closingVelocity) * 100f;
    }
    float accAz = N * turnFactor * losRateAz;
    float accEl = N * turnFactor * losRateEl;
    // Convert acceleration inputs to craft body frame
    accelerationInput = transformRight * accAz + transformUp * accEl;

    // Clamp the normal acceleration input to the maximum normal acceleration
    float maxNormalAcceleration = CalculateMaxNormalAcceleration();
    accelerationInput = Vector3.ClampMagnitude(accelerationInput, maxNormalAcceleration);

    // Avoid the ground when close to the surface and too low on the glideslope
    float altitude = transformPosition.y;
    float sinkRate = -transformVelocity.y;  // Sink rate is opposite to climb rate
    float distanceToTarget = sensorOutput.position.range;
    float groundProximityThreshold =
        Mathf.Abs(transformVelocity.y) * 5f;  // Adjust threshold as necessary
    if (sinkRate > 0 && altitude / sinkRate < distanceToTarget / transformSpeed) {
      // Evade upward normal to the velocity
      Vector3 upwardsDirection = Vector3.Cross(transformForward, transformRight);

      // Blend between the calculated acceleration input and the upward acceleration
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

  // Optional: Add this method to visualize debug information
  protected virtual void OnDrawGizmos() {
    if (Application.isPlaying) {
      Gizmos.color = Color.yellow;
      Gizmos.DrawLine(transform.position, _currentWaypoint);

      Gizmos.color = Color.green;
      Gizmos.DrawRay(transform.position, _accelerationInput);
    }
  }
}
