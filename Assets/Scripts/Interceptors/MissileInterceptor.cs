using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileInterceptor : Interceptor {
  [SerializeField]
  private float _navigationGain = 3f;  // Typically 3-5

  private SensorOutput _sensorOutput;
  private Vector3 _accelerationCommand;
  private double _elapsedTime = 0;
  protected override void UpdateMidCourse(double deltaTime) {
    _elapsedTime += deltaTime;
    Vector3 accelerationInput = Vector3.zero;
    if (HasAssignedTarget()) {
      // Correct the state of the threat model at the sensor frequency
      float sensorUpdatePeriod = 1f / _dynamicAgentConfig.dynamic_config.sensor_config.frequency;
      if (_elapsedTime >= sensorUpdatePeriod) {
        // TODO: Implement guidance filter to estimate state from sensor output
        // For now, we'll use the threat's actual state
        _targetModel.SetPosition(_target.GetPosition());
        _targetModel.SetVelocity(_target.GetVelocity());
        _elapsedTime = 0;
      }

      // Check whether the threat should be considered a miss
      SensorOutput sensorOutput = GetComponent<Sensor>().Sense(_target);
      if (sensorOutput.velocity.range > 1000f) {
        this.HandleInterceptMiss();
      }

      // Calculate the acceleration input
      _sensorOutput = GetComponent<Sensor>().Sense(_targetModel);
      accelerationInput = CalculateAccelerationCommand(_sensorOutput);
    }

    // Calculate and set the total acceleration
    Vector3 acceleration = CalculateAcceleration(accelerationInput, compensateForGravity: true);
    GetComponent<Rigidbody>().AddForce(acceleration, ForceMode.Acceleration);
    base.UpdateMidCourse(deltaTime);
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

    // Clamp the normal acceleration command to the maximum normal acceleration
    float maxNormalAcceleration = CalculateMaxNormalAcceleration();
    accelerationCommand = Vector3.ClampMagnitude(accelerationCommand, maxNormalAcceleration);

    // Update the stored acceleration command for debugging
    _accelerationCommand = accelerationCommand;
    return accelerationCommand;
  }

  protected override void DrawDebugVectors() {
    base.DrawDebugVectors();
    if (_accelerationCommand != null) {
      Debug.DrawRay(transform.position, _accelerationCommand * 1f, Color.green);
    }
  }
}
