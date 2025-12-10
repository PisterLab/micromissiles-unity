using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Agent : MonoBehaviour {
  // In the initialized phase, the agent is subject to no forces.
  // In the ready phase, the agent is subject to gravity and drag with zero input acceleration.
  // TODO(titan): Replace this enumeration with the Protobuf enumeration.
  public enum FlightPhase { INITIALIZED, READY, BOOST, MIDCOURSE, TERMINAL, TERMINATED }

  private const float _epsilon = 1e-12f;

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
  protected List<Agent> _interceptors = new List<Agent>();
  [SerializeField]
  protected Agent _target;
  [SerializeField]
  protected Agent _targetModel;

  [SerializeField]
  protected double _timeSinceBoost = 0;
  [SerializeField]
  protected double _timeInPhase = 0;

  public Configs.StaticConfig staticConfig;
  public Configs.AgentConfig agentConfig;

  // Define delegates.
  public delegate void TerminatedEventHandler(Agent agent);
  public event TerminatedEventHandler OnTerminated;

  public delegate void InterceptHitEventHandler(Interceptor interceptor, Threat target);
  public delegate void InterceptMissEventHandler(Interceptor interceptor, Threat target);
  public delegate void ThreatHitEventHandler(Threat threat);
  public delegate void ThreatMissEventHandler(Threat threat);

  // Define events.
  public event InterceptHitEventHandler OnInterceptHit;
  public event InterceptMissEventHandler OnInterceptMiss;
  public event ThreatHitEventHandler OnThreatHit;
  public event ThreatMissEventHandler OnThreatMiss;

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

  public bool IsInitialized() {
    return _flightPhase != FlightPhase.INITIALIZED;
  }

  public bool IsTerminated() {
    return _flightPhase == FlightPhase.TERMINATED;
  }

  public virtual void SetAgentConfig(Configs.AgentConfig config) {
    agentConfig = config;
  }

  public virtual void SetStaticConfig(Configs.StaticConfig config) {
    staticConfig = config;
    GetComponent<Rigidbody>().mass = staticConfig.BodyConfig?.Mass ?? 1;
  }

  public virtual bool IsAssignable() {
    return true;
  }

  public virtual void AssignTarget(Agent target) {
    _target = target;
    _target.AddInterceptor(this);
    _targetModel = SimManager.Instance.CreateDummyAgent(target.GetPosition(), target.GetVelocity());
  }

  public Agent GetAssignedTarget() {
    return _target;
  }

  public bool HasAssignedTarget() {
    return _target != null;
  }

  public Agent GetTargetModel() {
    return _targetModel;
  }

  public virtual void UnassignTarget() {
    if (HasAssignedTarget()) {
      _target.RemoveInterceptor(this);
    }
    _target = null;
    _targetModel = null;
  }

  public void AddInterceptor(Agent interceptor) {
    _interceptors.Add(interceptor);
  }

  public void RemoveInterceptor(Agent interceptor) {
    _interceptors.Remove(interceptor);
  }

  public IReadOnlyList<Agent> AssignedInterceptors {
    get { return _interceptors; }
  }

  public virtual void TerminateAgent() {
    UnassignTarget();
    if (_flightPhase != FlightPhase.TERMINATED) {
      OnTerminated?.Invoke(this);
    }
    _flightPhase = FlightPhase.TERMINATED;
    SetPosition(Vector3.zero);
    gameObject.SetActive(false);
  }

  public void HandleInterceptHit(Agent agent) {
    if (this is Interceptor interceptor && agent is Threat threat) {
      OnInterceptHit?.Invoke(interceptor, threat);
      TerminateAgent();
    }
  }

  public void HandleInterceptMiss() {
    if (this is Interceptor interceptor && _target is Threat threat) {
      OnInterceptMiss?.Invoke(interceptor, threat);
    }
  }

  public void HandleTargetIntercepted() {
    if (this is Threat threat) {
      TerminateAgent();
    }
  }

  public void HandleThreatHit() {
    if (this is Threat threat) {
      OnThreatHit?.Invoke(threat);
      TerminateAgent();
    }
  }

  public void HandleHitGround() {
    if (this is Interceptor interceptor && _target is Threat target) {
      OnInterceptMiss?.Invoke(interceptor, target);
    }
    if (this is Threat threat) {
      OnThreatMiss?.Invoke(threat);
    }
    TerminateAgent();
  }

  public void SetInitialVelocity(Vector3 velocity) {
    _initialVelocity = velocity;
  }

  public Vector3 GetPosition() {
    return transform.position;
  }

  public void SetPosition(Vector3 position) {
    transform.position = position;
  }

  public float GetSpeed() {
    return GetComponent<Rigidbody>().linearVelocity.magnitude;
  }

  public Vector3 GetVelocity() {
    return GetComponent<Rigidbody>().linearVelocity;
  }

  public void SetVelocity(Vector3 velocity) {
    GetComponent<Rigidbody>().linearVelocity = velocity;
  }

  public Vector3 GetAcceleration() {
    return _acceleration;
  }

  public void SetAcceleration(Vector3 acceleration) {
    _acceleration = acceleration;
  }

  public Transformation GetRelativeTransformation(Agent target) {
    Vector3 relativePosition = target.GetPosition() - GetPosition();
    // Get the relative position transformation.
    PositionTransformation positionTransformation =
        GetRelativePositionTransformation(relativePosition);

    // Get the relative velocity transformation.
    VelocityTransformation velocityTransformation =
        GetRelativeVelocityTransformation(relativePosition, target.GetVelocity() - GetVelocity());

    // Get the relative acceleration transformation.
    AccelerationTransformation accelerationTransformation =
        GetRelativeAccelerationTransformation(target.GetAcceleration());
    return new Transformation {
      Position = positionTransformation,
      Velocity = velocityTransformation,
      Acceleration = accelerationTransformation,
    };
  }

  public Transformation GetRelativeTransformationToWaypoint(Vector3 waypoint) {
    // Get the relative position transformation.
    PositionTransformation positionTransformation =
        GetRelativePositionTransformation(waypoint - GetPosition());

    // Get the relative velocity transformation.
    VelocityTransformation velocityTransformation =
        GetRelativeVelocityTransformation(waypoint - GetPosition(), -GetVelocity());

    return new Transformation {
      Position = positionTransformation,
      Velocity = velocityTransformation,
    };
  }

  private PositionTransformation GetRelativePositionTransformation(in Vector3 relativePosition) {
    Vector3 flatRelativePosition = Vector3.ProjectOnPlane(relativePosition, transform.up);
    Vector3 verticalRelativePosition = relativePosition - flatRelativePosition;

    // Calculate the elevation (vertical angle relative to forward).
    float elevation =
        Mathf.Atan2(verticalRelativePosition.magnitude, flatRelativePosition.magnitude);

    // Calculate the azimuth (horizontal angle relative to forward).
    float azimuth = 0;
    if (flatRelativePosition.sqrMagnitude >= _epsilon) {
      azimuth = Vector3.SignedAngle(transform.forward, flatRelativePosition, transform.up) *
                Mathf.Deg2Rad;
    }
    return new PositionTransformation {
      Cartesian = relativePosition,
      Range = relativePosition.magnitude,
      Azimuth = azimuth,
      Elevation = elevation,
    };
  }

  private VelocityTransformation GetRelativeVelocityTransformation(in Vector3 relativePosition,
                                                                   in Vector3 relativeVelocity) {
    if (relativePosition.sqrMagnitude < _epsilon) {
      return new VelocityTransformation {
        Cartesian = relativeVelocity,
        Range = relativeVelocity.magnitude,
        Azimuth = 0,
        Elevation = 0,
      };
    }

    // Calculate range rate (radial velocity).
    float rangeRate = Vector3.Dot(relativeVelocity, relativePosition.normalized);

    // Project relative velocity onto the sphere passing through the target.
    Vector3 tangentialVelocity = Vector3.ProjectOnPlane(relativeVelocity, relativePosition);

    // The target azimuth vector is orthogonal to the relative position vector and
    // points to the starboard of the target along the azimuth-elevation sphere.
    Vector3 targetAzimuth = Vector3.Cross(transform.up, relativePosition);
    // The target elevation vector is orthogonal to the relative position vector
    // and points upwards from the target along the azimuth-elevation sphere.
    Vector3 targetElevation = Vector3.Cross(relativePosition, transform.right);
    // If the relative position vector is parallel to the yaw or pitch axis, the
    // target azimuth vector or the target elevation vector will be undefined.
    if (targetAzimuth.sqrMagnitude < _epsilon) {
      targetAzimuth = Vector3.Cross(targetElevation, relativePosition);
    } else if (targetElevation.sqrMagnitude < _epsilon) {
      targetElevation = Vector3.Cross(relativePosition, targetAzimuth);
    }

    // Project the relative velocity vector on the azimuth-elevation sphere onto
    // the target azimuth vector.
    Vector3 tangentialVelocityOnAzimuth = Vector3.Project(tangentialVelocity, targetAzimuth);

    // Calculate the time derivative of the azimuth to the target.
    float azimuth = tangentialVelocityOnAzimuth.magnitude / relativePosition.magnitude;
    if (Vector3.Dot(tangentialVelocityOnAzimuth, targetAzimuth) < 0) {
      azimuth *= -1;
    }

    // Project the velocity vector on the azimuth-elevation sphere onto the target
    // elevation vector.
    Vector3 tangentialVelocityOnElevation = Vector3.Project(tangentialVelocity, targetElevation);

    // Calculate the time derivative of the elevation to the target.
    float elevation = tangentialVelocityOnElevation.magnitude / relativePosition.magnitude;
    if (Vector3.Dot(tangentialVelocityOnElevation, targetElevation) < 0) {
      elevation *= -1;
    }
    return new VelocityTransformation {
      Cartesian = relativeVelocity,
      Range = rangeRate,
      Azimuth = azimuth,
      Elevation = elevation,
    };
  }

  private AccelerationTransformation GetRelativeAccelerationTransformation(
      in Vector3 relativeAcceleration) {
    return new AccelerationTransformation {
      Cartesian = relativeAcceleration,
    };
  }

  public float GetDynamicPressure() {
    var airDensity = Constants.CalculateAirDensityAtAltitude(transform.position.y);
    var flowSpeed = GetSpeed();
    return 0.5f * airDensity * (flowSpeed * flowSpeed);
  }

  protected abstract void UpdateReady(double deltaTime);
  protected abstract void UpdateBoost(double deltaTime);
  protected abstract void UpdateMidCourse(double deltaTime);

  protected virtual void Awake() {}

  protected virtual void Start() {}

  protected virtual void FixedUpdate() {
    _speed = GetSpeed();
    if (_flightPhase != FlightPhase.INITIALIZED && _flightPhase != FlightPhase.READY) {
      _timeSinceBoost += Time.fixedDeltaTime;
    }
    _timeInPhase += Time.fixedDeltaTime;

    var boostTime = staticConfig.BoostConfig?.BoostTime ?? 0;
    if (_flightPhase == FlightPhase.TERMINATED) {
      return;
    }
    if (_flightPhase == FlightPhase.INITIALIZED || _flightPhase == FlightPhase.READY) {
      SetFlightPhase(FlightPhase.BOOST);
    }
    if (_timeSinceBoost > boostTime && _flightPhase == FlightPhase.BOOST) {
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
    // Only align if we have significant velocity.
    if (velocity.magnitude > 0.1f) {
      // Create a rotation with forward along velocity and up along world up.
      Quaternion targetRotation = Quaternion.LookRotation(velocity, Vector3.up);

      // Smoothly rotate towards the target rotation.
      transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation,
                                                    10000f * Time.fixedDeltaTime);
    }
  }

  protected Vector3 CalculateAcceleration(Vector3 accelerationInput) {
    Vector3 gravity = Physics.gravity;
    float airDrag = CalculateDrag();
    float liftInducedDrag = CalculateLiftInducedDrag(accelerationInput + gravity);
    float dragAcceleration = -(airDrag + liftInducedDrag);

    // Project the drag acceleration onto the forward direction.
    Vector3 dragAccelerationAlongRoll = dragAcceleration * transform.forward;
    _dragAcceleration = dragAccelerationAlongRoll;

    return accelerationInput + gravity + dragAccelerationAlongRoll;
  }

  public float CalculateMaxForwardAcceleration() {
    return staticConfig.AccelerationConfig?.MaxForwardAcceleration ?? 0;
  }

  public float CalculateMaxNormalAcceleration() {
    float maxReferenceNormalAcceleration =
        (staticConfig.AccelerationConfig?.MaxReferenceNormalAcceleration ?? 0) * Constants.kGravity;
    float referenceSpeed = staticConfig.AccelerationConfig?.ReferenceSpeed ?? 1;
    return Mathf.Pow(GetSpeed() / referenceSpeed, 2) * maxReferenceNormalAcceleration;
  }

  private float CalculateDrag() {
    float dragCoefficient = staticConfig.LiftDragConfig?.DragCoefficient ?? 0;
    float crossSectionalArea = staticConfig.BodyConfig?.CrossSectionalArea ?? 0;
    float mass = staticConfig.BodyConfig?.Mass ?? 1;
    float dynamicPressure = GetDynamicPressure();
    float dragForce = dragCoefficient * dynamicPressure * crossSectionalArea;
    return dragForce / mass;
  }

  private float CalculateLiftInducedDrag(Vector3 accelerationInput) {
    float liftAcceleration = Vector3.ProjectOnPlane(accelerationInput, transform.forward).magnitude;
    float liftDragRatio = staticConfig.LiftDragConfig?.LiftDragRatio ?? 1;
    return Mathf.Abs(liftAcceleration / liftDragRatio);
  }
}

public class DummyAgentLegacy : Agent {
  protected override void Start() {
    base.Start();
  }

  protected override void FixedUpdate() {
    GetComponent<Rigidbody>().AddForce(_acceleration, ForceMode.Acceleration);
  }

  protected override void UpdateReady(double deltaTime) {
    // Do nothing.
  }

  protected override void UpdateBoost(double deltaTime) {
    // Do nothing.
  }

  protected override void UpdateMidCourse(double deltaTime) {
    // Do nothing.
  }
}
