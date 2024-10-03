using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixedWingThreat : Threat {
  [SerializeField]
  private float _navigationGain = 3f;


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

      // Calculate the acceleration input using PN
      accelerationInput = CalculateAccelerationCommand();

      // Adjust velocity based on power setting
      AdjustVelocityForPowerSetting();
    }

    // Calculate and set the total acceleration
    Vector3 acceleration = CalculateAcceleration(accelerationInput, compensateForGravity: true);
    GetComponent<Rigidbody>().AddForce(acceleration, ForceMode.Acceleration);
  }

  private void UpdateWaypointAndPower() {
    // Get the next waypoint and power setting from the attack behavior
    (_currentWaypoint, _currentPowerSetting) = _attackBehavior.GetNextWaypoint(transform.position, _targetPosition);
  }

  private Vector3 CalculateAccelerationCommand() {
    Vector3 accelerationCommand = Vector3.zero;

    // Calculate line of sight (LOS) to the current waypoint
    Vector3 los = _currentWaypoint - transform.position;
    Vector3 losVelocity = Vector3.zero; // We'll use a simplified approach here

    // Calculate closing velocity (simplified)
    float closingVelocity = Vector3.Dot(-los.normalized, GetVelocity());

    // Calculate LOS rates (simplified)
    Vector3 losRate = Vector3.Cross(los.normalized, losVelocity) / los.magnitude;

    // Apply Proportional Navigation
    accelerationCommand = _navigationGain * closingVelocity * Vector3.Cross(los.normalized, losRate);

    // Clamp the acceleration command to the maximum acceleration
    float maxAcceleration = CalculateMaxAcceleration();
    accelerationCommand = Vector3.ClampMagnitude(accelerationCommand, maxAcceleration);

    // Update the stored acceleration command for debugging
    _accelerationCommand = accelerationCommand;

    return accelerationCommand;
  }

  private void AdjustVelocityForPowerSetting() {
    // Get the target velocity for the current power setting
    float targetVelocity = PowerTableLookup(_currentPowerSetting);

    // Calculate the current velocity
    Vector3 currentVelocity = GetVelocity();
    float currentSpeed = currentVelocity.magnitude;

    // Calculate the acceleration needed to reach the target velocity
    float accelerationMagnitude = (targetVelocity - currentSpeed) / Time.fixedDeltaTime;
    Vector3 velocityAdjustment = currentVelocity.normalized * accelerationMagnitude;

    // Apply the velocity adjustment
    GetComponent<Rigidbody>().AddForce(velocityAdjustment, ForceMode.Acceleration);
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
