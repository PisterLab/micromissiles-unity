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
  protected Sensor _sensor;

  protected override void Awake() {
    base.Awake();
    SetFlightPhase(FlightPhase.INITIALIZED);
  }

  public void SetAttackBehavior(AttackBehavior attackBehavior) {
    _attackBehavior = attackBehavior;
    _target = SimManager.Instance.CreateDummyAgent(attackBehavior.targetPosition,
                                                   attackBehavior.targetVelocity);
  }

  protected float PowerTableLookup(PowerSetting powerSetting) {
    switch (powerSetting) {
      case PowerSetting.IDLE:
        return staticAgentConfig.powerTable.IDLE;
      case PowerSetting.LOW:
        return staticAgentConfig.powerTable.LOW;
      case PowerSetting.CRUISE:
        return staticAgentConfig.powerTable.CRUISE;
      case PowerSetting.MIL:
        return staticAgentConfig.powerTable.MIL;
      case PowerSetting.MAX:
        return staticAgentConfig.powerTable.MAX;
      default:
        Debug.LogError("Invalid power setting.");
        return 0f;
    }
  }

  public override bool IsAssignable() {
    return false;
  }

  protected override void Start() {
    base.Start();
    _sensor = GetComponent<Sensor>();
  }

  protected override void FixedUpdate() {
    base.FixedUpdate();
  }

  protected Vector3 CalculateForwardAcceleration() {
    Vector3 transformVelocity = GetVelocity();

    // Get the target speed for the current power setting.
    float targetSpeed = PowerTableLookup(_currentPowerSetting);

    // Calculate the current speed.
    float currentSpeed = transformVelocity.magnitude;

    // Speed error.
    float speedError = targetSpeed - currentSpeed;

    // Proportional gain for speed control.
    float speedControlGain = 10.0f;  // Adjust this gain as necessary.

    // Desired acceleration to adjust speed.
    float desiredAccelerationMagnitude = speedControlGain * speedError;

    // Limit the desired acceleration.
    float maxForwardAcceleration = CalculateMaxForwardAcceleration();
    desiredAccelerationMagnitude =
        Mathf.Clamp(desiredAccelerationMagnitude, -maxForwardAcceleration, maxForwardAcceleration);

    // Acceleration direction (along current velocity direction).
    Vector3 accelerationDirection = transformVelocity.normalized;

    // Handle the zero velocity case.
    if (accelerationDirection.magnitude < 0.1f) {
      accelerationDirection = transform.forward;  // Default direction.
    }

    // Calculate the acceleration vector.
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
      if (!interceptor.HasTerminated()) {
        SensorOutput sensorOutput = _sensor.Sense(interceptor);
        if (sensorOutput.position.range < minDistance) {
          closestInterceptor = interceptor;
          minDistance = sensorOutput.position.range;
        }
      }
    }
    return closestInterceptor;
  }

  protected bool ShouldEvade() {
    if (!dynamicAgentConfig.dynamic_config.flight_config.evasionEnabled) {
      return false;
    }

    Agent closestInterceptor = GetClosestInterceptor();
    if (closestInterceptor == null) {
      return false;
    }

    float evasionRangeThreshold =
        dynamicAgentConfig.dynamic_config.flight_config.evasionRangeThreshold;
    SensorOutput sensorOutput = _sensor.Sense(closestInterceptor);
    return sensorOutput.position.range <= evasionRangeThreshold && sensorOutput.velocity.range < 0;
  }

  protected Vector3 EvadeInterceptor(Agent interceptor) {
    Vector3 transformVelocity = GetVelocity();
    Vector3 interceptorVelocity = interceptor.GetVelocity();
    Vector3 transformPosition = GetPosition();
    Vector3 interceptorPosition = interceptor.GetPosition();

    // Set power setting to maximum.
    _currentPowerSetting = PowerSetting.MAX;

    // Evade the interceptor by changing the velocity to be normal to the interceptor's velocity.
    Vector3 normalVelocity = Vector3.ProjectOnPlane(transformVelocity, interceptorVelocity);
    Vector3 normalAccelerationDirection =
        Vector3.ProjectOnPlane(normalVelocity, transformVelocity).normalized;

    // Turn away from the interceptor.
    Vector3 relativePosition = interceptorPosition - transformPosition;
    if (Vector3.Dot(relativePosition, normalAccelerationDirection) > 0) {
      normalAccelerationDirection *= -1;
    }

    // Avoid evading straight down when near the ground.
    float altitude = transformPosition.y;
    float groundProximityThreshold =
        Mathf.Abs(transformVelocity.y) * 5f;  // Adjust threshold as necessary.
    if (transformVelocity.y < 0 && altitude < groundProximityThreshold) {
      // Determine evasion direction based on angle to interceptor.
      Vector3 toInterceptor = interceptorPosition - transformPosition;
      Vector3 rightDirection = Vector3.Cross(Vector3.up, transform.forward);
      float angle = Vector3.SignedAngle(transform.forward, toInterceptor, Vector3.up);

      // Choose the direction that leads away from the interceptor.
      Vector3 bestHorizontalDirection = angle > 0 ? -rightDirection : rightDirection;

      // Blend between horizontal evasion and slight upward movement.
      float blendFactor = 1 - (altitude / groundProximityThreshold);
      normalAccelerationDirection =
          Vector3
              .Lerp(normalAccelerationDirection, bestHorizontalDirection + transform.up * 5f,
                    blendFactor)
              .normalized;
    }
    Vector3 normalAcceleration = normalAccelerationDirection * CalculateMaxNormalAcceleration();

    // Adjust the forward speed.
    Vector3 forwardAcceleration = CalculateForwardAcceleration();

    return normalAcceleration + forwardAcceleration;
  }

  private void OnTriggerEnter(Collider other) {
    // Check if the threat hit the floor with a negative vertical speed.
    if (other.gameObject.name == "Floor" && Vector3.Dot(GetVelocity(), Vector3.up) < 0) {
      HandleHitGround();
    }

    // Check if the collision is with the asset.
    DummyAgent otherAgent = other.gameObject.GetComponentInParent<DummyAgent>();
    if (otherAgent != null && _target == otherAgent) {
      HandleThreatHit();
    }
  }
}
