using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixedWingThreat : Threat {
  [SerializeField]
  private float _navigationGain = 50f;


  private Vector3 _accelerationCommand;
  private double _elapsedTime = 0;

  // Start is called before the first frame update
  protected override void Start() {
    base.Start();
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

    if (HasAssignedTarget()) {
      // Update waypoint and power setting
      UpdateWaypointAndPower();

      float sensorUpdatePeriod = 1f / _dynamicAgentConfig.dynamic_config.sensor_config.frequency;
      if (_elapsedTime >= sensorUpdatePeriod) {
        // TODO: Implement guidance filter to estimate state from sensor output
        // For now, we'll use the threat's actual state
        _sensorOutput = GetComponent<Sensor>().SenseWaypoint(_currentWaypoint);
        _elapsedTime = 0;
      }

      // Calculate the acceleration input using PN
      Vector3 pnAcceleration = CalculateAccelerationCommand(_sensorOutput);

      // Adjust velocity based on power setting
      Vector3 speedAdjustmentAcceleration = CalculateSpeedAdjustmentAcceleration();

      // Combine the accelerations
      accelerationInput = pnAcceleration + speedAdjustmentAcceleration;

      // Clamp the total acceleration
      float maxAcceleration = CalculateMaxAcceleration();
      if (accelerationInput.magnitude > maxAcceleration) {
        accelerationInput = accelerationInput.normalized * maxAcceleration;
      }
    }

    // Calculate and set the total acceleration
    Vector3 acceleration = CalculateAcceleration(accelerationInput, compensateForGravity: true);
    GetComponent<Rigidbody>().AddForce(acceleration, ForceMode.Acceleration);
  }
  private void UpdateWaypointAndPower() {
    // Get the next waypoint and power setting from the attack behavior
    // TODO: Implement support for SENSORS to update the track on the target position
    (_currentWaypoint, _currentPowerSetting) = _attackBehavior.GetNextWaypoint(transform.position, _target.transform.position);
  }

  private Vector3 CalculateAccelerationCommand(SensorOutput sensorOutput) {
    // Implement Proportional Navigation guidance law
    Vector3 accelerationCommand = Vector3.zero;

    // Extract relevant information from sensor output
    float los_rate_az = sensorOutput.velocity.azimuth;
    float los_rate_el = sensorOutput.velocity.elevation;
    float closing_velocity =
        -sensorOutput.velocity
             .range;  // Negative because closing velocity is opposite to range rate

    // Navigation gain (adjust as needed)
    float N = _navigationGain;

    // Calculate acceleration commands in azimuth and elevation planes
    float acc_az = N * closing_velocity * los_rate_az;
    float acc_el = N * closing_velocity * los_rate_el;

    // Convert acceleration commands to craft body frame
    accelerationCommand = transform.right * acc_az + transform.up * acc_el;

    // Clamp the acceleration command to the maximum acceleration
    float maxAcceleration = CalculateMaxAcceleration();
    accelerationCommand = Vector3.ClampMagnitude(accelerationCommand, maxAcceleration);

    // Update the stored acceleration command for debugging
    _accelerationCommand = accelerationCommand;
    return accelerationCommand;
  }

  private Vector3 CalculateSpeedAdjustmentAcceleration() {
    // Get the target velocity for the current power setting
    float targetSpeed = PowerTableLookup(_currentPowerSetting);

    // Calculate the current speed
    float currentSpeed = GetVelocity().magnitude;

    // Speed error
    float speedError = targetSpeed - currentSpeed;

    // Proportional gain for speed control
    float speedControlGain = 10.0f; // Adjust this gain as necessary

    // Desired acceleration to adjust speed
    float desiredAccelerationMagnitude = speedControlGain * speedError;

    // Limit the desired acceleration
    float maxAcceleration = CalculateMaxAcceleration();
    desiredAccelerationMagnitude = Mathf.Clamp(desiredAccelerationMagnitude, -maxAcceleration, maxAcceleration);

    // Acceleration direction (along current velocity direction)
    Vector3 accelerationDirection = GetVelocity().normalized;

    // Handle zero velocity case
    if (accelerationDirection.magnitude < 0.1f) {
      accelerationDirection = transform.forward; // Default direction
    }

    // Calculate acceleration vector
    Vector3 speedAdjustmentAcceleration = accelerationDirection * desiredAccelerationMagnitude;

    return speedAdjustmentAcceleration;
  }

  // Optional: Add this method to visualize debug information
  protected virtual void OnDrawGizmos() {
    if (Application.isPlaying) {
      Gizmos.color = Color.yellow;
      Gizmos.DrawLine(transform.position, _currentWaypoint);

      Gizmos.color = Color.green;
      Gizmos.DrawRay(transform.position, _accelerationCommand);
    }
  }
}
