using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixedWingThreat : Threat {
  [SerializeField]
  private float _navigationGain = 50f;

  private Vector3 _accelerationInput;
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

    if (ShouldEvade()) {
      accelerationInput = EvadeInterceptor(GetClosestInterceptor());
    } else if (HasAssignedTarget()) {
      // Update waypoint and power setting
      UpdateWaypointAndPower();

      float sensorUpdatePeriod = 1f / dynamicAgentConfig.dynamic_config.sensor_config.frequency;
      if (_elapsedTime >= sensorUpdatePeriod) {
        // TODO: Implement guidance filter to estimate state from sensor output
        // For now, we'll use the threat's actual state
        _sensorOutput = GetComponent<Sensor>().SenseWaypoint(_currentWaypoint);
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
    GetComponent<Rigidbody>().AddForce(acceleration, ForceMode.Acceleration);
  }

  private void UpdateWaypointAndPower() {
    // Get the next waypoint and power setting from the attack behavior
    // TODO: Implement support for SENSORS to update the track on the target position
    (_currentWaypoint, _currentPowerSetting) =
        _attackBehavior.GetNextWaypoint(transform.position, _target.transform.position);
  }

  private Vector3 CalculateAccelerationInput(SensorOutput sensorOutput) {
    // Implement Proportional Navigation guidance law
    Vector3 accelerationInput = Vector3.zero;

    // Extract relevant information from sensor output
    float los_rate_az = sensorOutput.velocity.azimuth;
    float los_rate_el = sensorOutput.velocity.elevation;
    float closing_velocity =
        -sensorOutput.velocity
             .range;  // Negative because closing velocity is opposite to range rate

    // Navigation gain (adjust as needed)
    float N = _navigationGain;

    // Calculate acceleration inputs in azimuth and elevation planes
    float accAz = N * closing_velocity * los_rate_az;
    float accEl = N * closing_velocity * los_rate_el;

    // Convert acceleration inputs to craft body frame
    accelerationInput = transform.right * accAz + transform.up * accEl;

    // Clamp the normal acceleration input to the maximum normal acceleration
    float maxNormalAcceleration = CalculateMaxNormalAcceleration();
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
