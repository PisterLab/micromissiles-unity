using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotaryWingThreat : Threat {
  private Vector3 _accelerationCommand;

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
    if (HasAssignedTarget()) {
      // Update waypoint and power setting
      UpdateWaypointAndPower();

      // Calculate and apply acceleration
      Vector3 accelerationInput = CalculateAccelerationToWaypoint();
      ApplyAcceleration(accelerationInput);
    }
  }

  private void UpdateWaypointAndPower() {
    (_currentWaypoint, _currentPowerSetting) =
        _attackBehavior.GetNextWaypoint(transform.position, _target.transform.position);
  }

  private Vector3 CalculateAccelerationToWaypoint() {
    Vector3 toWaypoint = _currentWaypoint - transform.position;
    Vector3 currentVelocity = GetVelocity();

    // Calculate desired velocity based on power setting
    float desiredSpeed = PowerTableLookup(_currentPowerSetting);
    Vector3 desiredVelocity = toWaypoint.normalized * desiredSpeed;

    // Calculate acceleration needed to reach desired velocity
    Vector3 accelerationCommand = (desiredVelocity - currentVelocity) / (float)Time.fixedDeltaTime;

    // Limit acceleration magnitude
    float maxAcceleration = CalculateMaxAcceleration();
    accelerationCommand = Vector3.ClampMagnitude(accelerationCommand, maxAcceleration);

    _accelerationCommand = accelerationCommand;  // Store for debugging
    return accelerationCommand;
  }

  private void ApplyAcceleration(Vector3 acceleration) {
    // For RotaryWingThreat, we don't need to compensate for gravity or consider drag
    GetComponent<Rigidbody>().AddForce(acceleration, ForceMode.Acceleration);
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
