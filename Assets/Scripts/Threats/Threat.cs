using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Threat : Agent {
  protected AttackBehavior _attackBehavior;
  [SerializeField]
  protected Vector3 _currentWaypoint;
  [SerializeField]
  protected PowerSetting _currentPowerSetting;

  protected SensorOutput _sensorOutput;

  public void SetAttackBehavior(AttackBehavior attackBehavior) {
    _attackBehavior = attackBehavior;
    _target = SimManager.Instance.CreateDummyAgent(attackBehavior.targetPosition,
                                                   attackBehavior.targetVelocity);
  }

  protected float PowerTableLookup(PowerSetting powerSetting) {
    switch (powerSetting) {
      case PowerSetting.IDLE:
        return _staticAgentConfig.powerTable.IDLE;
      case PowerSetting.LOW:
        return _staticAgentConfig.powerTable.LOW;
      case PowerSetting.CRUISE:
        return _staticAgentConfig.powerTable.CRUISE;
      case PowerSetting.MIL:
        return _staticAgentConfig.powerTable.MIL;
      case PowerSetting.MAX:
        return _staticAgentConfig.powerTable.MAX;
      default:
        Debug.LogError("Invalid power setting");
        return 0f;
    }
  }

  public override bool IsAssignable() {
    return false;
  }

  protected override void Start() {
    base.Start();
  }

  protected override void FixedUpdate() {
    base.FixedUpdate();
  }

  protected Vector3 CalculateForwardAcceleration() {
    // Get the target speed for the current power setting
    float targetSpeed = PowerTableLookup(_currentPowerSetting);

    // Calculate the current speed
    float currentSpeed = GetVelocity().magnitude;

    // Speed error
    float speedError = targetSpeed - currentSpeed;

    // Proportional gain for speed control
    float speedControlGain = 10.0f;  // Adjust this gain as necessary

    // Desired acceleration to adjust speed
    float desiredAccelerationMagnitude = speedControlGain * speedError;

    // Limit the desired acceleration
    float maxForwardAcceleration = CalculateMaxForwardAcceleration();
    desiredAccelerationMagnitude =
        Mathf.Clamp(desiredAccelerationMagnitude, -maxForwardAcceleration, maxForwardAcceleration);

    // Acceleration direction (along current velocity direction)
    Vector3 accelerationDirection = GetVelocity().normalized;

    // Handle zero velocity case
    if (accelerationDirection.magnitude < 0.1f) {
      accelerationDirection = transform.forward;  // Default direction
    }

    // Calculate acceleration vector
    Vector3 speedAdjustmentAcceleration = accelerationDirection * desiredAccelerationMagnitude;
    return speedAdjustmentAcceleration;
  }

  protected Agent GetClosestInterceptor() {
    if (_interceptors.Count == 0) {
      return null;
    }

    Agent closestInterceptor = null;
    float minDistance = float.MaxValue;
    foreach (var interceptor in _interceptors) {
      SensorOutput sensorOutput = GetComponent<Sensor>().Sense(interceptor);
      if (sensorOutput.position.range < minDistance) {
        closestInterceptor = interceptor;
        minDistance = sensorOutput.position.range;
      }
    }
    return closestInterceptor;
  }

  protected bool ShouldEvade(float rangeThreshold) {
    Agent closestInterceptor = GetClosestInterceptor();
    if (closestInterceptor == null) {
      return false;
    }

    SensorOutput sensorOutput = GetComponent<Sensor>().Sense(closestInterceptor);
    return sensorOutput.position.range <= rangeThreshold && sensorOutput.velocity.range < 0;
  }

  protected Vector3 EvadeInterceptor(Agent interceptor) {
    // Set power setting to maximum
    _currentPowerSetting = PowerSetting.MAX;

    // Evade the interceptor by changing the velocity to be normal to the interceptor's velocity
    Vector3 normalVelocity = Vector3.ProjectOnPlane(GetVelocity(), interceptor.GetVelocity());
    Vector3 normalAccelerationDirection =
        Vector3.ProjectOnPlane(normalVelocity, GetVelocity()).normalized;

    // Turn away from the interceptor
    Vector3 relativePosition = interceptor.GetPosition() - GetPosition();
    if (Vector3.Dot(relativePosition, normalAccelerationDirection) > 0) {
      normalAccelerationDirection *= -1;
    }
    Vector3 normalAcceleration = normalAccelerationDirection * CalculateMaxNormalAcceleration();

    // Adjust forward speed
    Vector3 forwardAcceleration = CalculateForwardAcceleration();
    return normalAcceleration + forwardAcceleration;
  }
}
