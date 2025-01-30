using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotaryWingThreat : Threat {
  private Vector3 _accelerationInput;

  // Start is called before the first frame update.
  protected override void Start() {
    base.Start();
  }

  // Update is called once per frame.
  protected override void FixedUpdate() {
    base.FixedUpdate();
  }

  protected override void UpdateReady(double deltaTime) {}

  protected override void UpdateBoost(double deltaTime) {}

  protected override void UpdateMidCourse(double deltaTime) {
    Vector3 accelerationInput = Vector3.zero;

    if (ShouldEvade()) {
      accelerationInput = EvadeInterceptor(GetClosestInterceptor());
    } else if (HasAssignedTarget()) {
      // Update waypoint and power setting.
      UpdateWaypointAndPower();

      // Calculate and apply acceleration.
      accelerationInput = CalculateAccelerationToWaypoint();
    }

    // For RotaryWingThreat, we don't need to compensate for gravity or consider drag.
    GetComponent<Rigidbody>().AddForce(accelerationInput, ForceMode.Acceleration);
  }

  private void UpdateWaypointAndPower() {
    (_currentWaypoint, _currentPowerSetting) =
        _attackBehavior.GetNextWaypoint(transform.position, _target.transform.position);
  }

  private Vector3 CalculateAccelerationToWaypoint() {
    float desiredSpeed = PowerTableLookup(_currentPowerSetting);

    IController controller = new WaypointController(this, desiredSpeed);
    Vector3 accelerationInput = controller.PlanToWaypoint(_currentWaypoint);

    Vector3 forwardAccelerationInput = Vector3.Project(accelerationInput, transform.forward);
    Vector3 normalAccelerationInput = accelerationInput - forwardAccelerationInput;

    // Limit the acceleration magnitude.
    float maxForwardAcceleration = CalculateMaxForwardAcceleration();
    forwardAccelerationInput =
        Vector3.ClampMagnitude(forwardAccelerationInput, maxForwardAcceleration);
    float maxNormalAcceleration = CalculateMaxNormalAcceleration();
    normalAccelerationInput =
        Vector3.ClampMagnitude(normalAccelerationInput, maxNormalAcceleration);
    accelerationInput = forwardAccelerationInput + normalAccelerationInput;

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
