using UnityEngine;
using System.Collections.Generic;

public abstract class AirborneAgent : Agent {
  // In the initialized phase, the agent is subject to no forces.
  // In the ready phase, the agent is subject to gravity and drag with zero input acceleration.
  public enum FlightPhase { INITIALIZED, READY, BOOST, MIDCOURSE, TERMINAL, TERMINATED }

  [SerializeField]
  private FlightPhase _flightPhase = FlightPhase.INITIALIZED;

  [SerializeField]
  protected Vector3 _velocity;

  [SerializeField]
  protected Vector3 _acceleration;

  [SerializeField]
  protected Vector3 _dragAcceleration;

  [SerializeField]
  protected float _speed;
  
  [SerializeField]
  protected double _timeSinceBoost = 0;
  [SerializeField]
  protected double _timeInPhase = 0;

  private Vector3 _initialVelocity;

  public void SetFlightPhase(FlightPhase flightPhase) {
    _timeInPhase = 0;
    _flightPhase = flightPhase;
    if (flightPhase == FlightPhase.INITIALIZED || flightPhase == FlightPhase.TERMINATED) {
      GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
    } else {
      GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
    }
    if (flightPhase == FlightPhase.BOOST) {
      SetVelocity(GetVelocity() + _initialVelocity);
    }
  }

  public FlightPhase GetFlightPhase() {
    return _flightPhase;
  }

  public bool HasLaunched() {
    return _flightPhase != FlightPhase.INITIALIZED;
  }

  public override bool IsTerminated() {
    return _flightPhase == FlightPhase.TERMINATED;
  }
  
  public override void TerminateAgent() {
    if (_flightPhase != FlightPhase.TERMINATED) {
      _flightPhase = FlightPhase.TERMINATED;
      SetPosition(Vector3.zero);
      base.TerminateAgent(); // This will handle UnassignTarget() and OnTerminated event
    }
  }

  public override void SetInitialVelocity(Vector3 velocity) {
    _initialVelocity = velocity;
  }

  public override Vector3 GetAcceleration() {
    return _acceleration;
  }

  protected abstract void UpdateReady(double deltaTime);
  protected abstract void UpdateBoost(double deltaTime);
  protected abstract void UpdateMidCourse(double deltaTime);

  protected override void FixedUpdate() {
    base.FixedUpdate();
    _speed = (float)GetSpeed();
    if (_flightPhase != FlightPhase.INITIALIZED && _flightPhase != FlightPhase.READY) {
      _timeSinceBoost += Time.fixedDeltaTime;
    }
    _timeInPhase += Time.fixedDeltaTime;

    var boost_time = staticAgentConfig.boostConfig.boostTime;
    double elapsedSimulationTime = SimManager.Instance.GetElapsedSimulationTime();

    if (_flightPhase == FlightPhase.TERMINATED) {
      return;
    }
    if (_flightPhase == FlightPhase.INITIALIZED || _flightPhase == FlightPhase.READY) {
      SetFlightPhase(FlightPhase.BOOST);
    }
    if (_timeSinceBoost > boost_time && _flightPhase == FlightPhase.BOOST) {
      SetFlightPhase(FlightPhase.MIDCOURSE);
    }
    AlignWithVelocity();
    switch (_flightPhase) {
      case FlightPhase.INITIALIZED:
        break;
      case FlightPhase.READY:
        UpdateReady(Time.fixedDeltaTime);
        break;
      case FlightPhase.BOOST:
        UpdateBoost(Time.fixedDeltaTime);
        break;
      case FlightPhase.MIDCOURSE:
      case FlightPhase.TERMINAL:
        UpdateMidCourse(Time.fixedDeltaTime);
        break;
      case FlightPhase.TERMINATED:
        break;
    }

    _velocity = GetVelocity();
    // Store the acceleration because it is set to zero after each simulation step
    _acceleration =
        GetComponent<Rigidbody>().GetAccumulatedForce() / GetComponent<Rigidbody>().mass;
  }

  protected virtual void AlignWithVelocity() {
    Vector3 velocity = GetVelocity();
    if (velocity.magnitude > 0.1f)
    {
      Quaternion targetRotation = Quaternion.LookRotation(velocity, Vector3.up);
      transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation,
                                                    10000f * Time.fixedDeltaTime);
    }
  }
  
  protected Vector3 CalculateAcceleration(Vector3 accelerationInput) {
    Vector3 gravity = Physics.gravity;
    float airDrag = CalculateDrag();
    float liftInducedDrag = CalculateLiftInducedDrag(accelerationInput + gravity);
    float dragAcceleration = -(airDrag + liftInducedDrag);

    Vector3 dragAccelerationAlongRoll = dragAcceleration * transform.forward;
    _dragAcceleration = dragAccelerationAlongRoll;

    return accelerationInput + gravity + dragAccelerationAlongRoll;
  }

  public float CalculateMaxForwardAcceleration() {
    return staticAgentConfig.accelerationConfig.maxForwardAcceleration;
  }

  public float CalculateMaxNormalAcceleration() {
    float maxReferenceNormalAcceleration =
        (float)(staticAgentConfig.accelerationConfig.maxReferenceNormalAcceleration *
                Constants.kGravity);
    float referenceSpeed = staticAgentConfig.accelerationConfig.referenceSpeed;
    return Mathf.Pow((float)GetSpeed() / referenceSpeed, 2) * maxReferenceNormalAcceleration;
  }

  private float CalculateDrag() {
    float dragCoefficient = staticAgentConfig.liftDragConfig.dragCoefficient;
    float crossSectionalArea = staticAgentConfig.bodyConfig.crossSectionalArea;
    float mass = staticAgentConfig.bodyConfig.mass;
    float dynamicPressure = (float)GetDynamicPressure();
    float dragForce = dragCoefficient * dynamicPressure * crossSectionalArea;
    return dragForce / mass;
  }

  private float CalculateLiftInducedDrag(Vector3 accelerationInput) {
    float liftAcceleration = Vector3.ProjectOnPlane(accelerationInput, transform.up).magnitude;
    float liftDragRatio = staticAgentConfig.liftDragConfig.liftDragRatio;
    return Mathf.Abs(liftAcceleration / liftDragRatio);
  }

  public double GetDynamicPressure() {
    var airDensity = Constants.CalculateAirDensityAtAltitude(transform.position.y);
    var flowSpeed = GetSpeed();
    return 0.5 * airDensity * (flowSpeed * flowSpeed);
  }
} 