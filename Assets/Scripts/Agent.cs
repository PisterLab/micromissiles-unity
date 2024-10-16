using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Agent : MonoBehaviour {
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
  // Only for debugging (viewing in editor)
  // Don't bother setting this it won't be used
  protected float _speed;

  [SerializeField]
  protected List<Agent> _interceptors = new List<Agent>();
  [SerializeField]
  protected Agent _target;
  [SerializeField]
  protected Agent _targetModel;
  protected bool _isHit = false;
  protected bool _isMiss = false;

  [SerializeField]
  protected double _timeSinceBoost = 0;
  [SerializeField]
  protected double _timeInPhase = 0;

  public StaticAgentConfig staticAgentConfig;
  public DynamicAgentConfig dynamicAgentConfig;

  // Define delegates
  // MarkDestroyed event handler
  public delegate void TerminatedEventHandler(Agent agent);
  public event TerminatedEventHandler OnTerminated;

  public delegate void InterceptHitEventHandler(Interceptor interceptor, Threat target);
  public delegate void InterceptMissEventHandler(Interceptor interceptor, Threat target);
  public delegate void ThreatHitEventHandler(Threat threat);
  public delegate void ThreatMissEventHandler(Threat threat);

  // Define events
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

  public bool HasLaunched() {
    return _flightPhase != FlightPhase.INITIALIZED;
  }

  public bool HasTerminated() {
    return _flightPhase == FlightPhase.TERMINATED;
  }

  public virtual void SetDynamicAgentConfig(DynamicAgentConfig config) {
    dynamicAgentConfig = config;
  }

  public virtual void SetStaticAgentConfig(StaticAgentConfig config) {
    staticAgentConfig = config;
    GetComponent<Rigidbody>().mass = staticAgentConfig.bodyConfig.mass;
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

  public void CheckTargetHit() {
    if (HasAssignedTarget() && _target.IsHit()) {
      UnassignTarget();
    }
  }

  public virtual void UnassignTarget() {
    _target.RemoveInterceptor(this);
    _target = null;
    _targetModel = null;
  }

  // Return whether the agent has hit or been hit.
  public bool IsHit() {
    return _isHit;
  }

  public void AddInterceptor(Agent interceptor) {
    _interceptors.Add(interceptor);
  }

  public void RemoveInterceptor(Agent interceptor) {
    _interceptors.Remove(interceptor);
  }

  public virtual void TerminateAgent() {
    if (_flightPhase != FlightPhase.TERMINATED) {
      OnTerminated?.Invoke(this);
    }
    _flightPhase = FlightPhase.TERMINATED;
    SetPosition(new Vector3(0, 0, 0));
    gameObject.SetActive(false);
  }

  // Mark the agent as having hit the target or been hit.
  public void HandleInterceptHit(Agent otherAgent) {
    _isHit = true;
    if (this is Interceptor interceptor && otherAgent is Threat threat) {
      OnInterceptHit?.Invoke(interceptor, threat);
    } else if (this is Threat threatAgent && otherAgent is Interceptor interceptorTarget) {
      OnInterceptHit?.Invoke(interceptorTarget, threatAgent);
    }
    TerminateAgent();
  }

  public void HandleInterceptMiss() {
    if (_target != null) {
      if (this is Interceptor interceptor && _target is Threat threat) {
        OnInterceptMiss?.Invoke(interceptor, threat);
      } else if (this is Threat threatAgent && _target is Interceptor interceptorTarget) {
        OnInterceptMiss?.Invoke(interceptorTarget, threatAgent);
      }
      UnassignTarget();
    }
    TerminateAgent();
  }

  // This happens if we, e.g., hit the carrier
  public void HandleThreatHit() {
    _isHit = true;
    if (this is Threat threat) {
      OnThreatHit?.Invoke(threat);
    }
    TerminateAgent();
  }

  // This happens if we, e.g., hit the floor
  public void HandleThreatMiss() {
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

  public double GetSpeed() {
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

  public double GetDynamicPressure() {
    var airDensity = Constants.CalculateAirDensityAtAltitude(transform.position.y);
    var flowSpeed = GetSpeed();
    return 0.5 * airDensity * (flowSpeed * flowSpeed);
  }

  protected abstract void UpdateReady(double deltaTime);
  protected abstract void UpdateBoost(double deltaTime);
  protected abstract void UpdateMidCourse(double deltaTime);

  protected virtual void Awake() {}

  // Start is called before the first frame update
  protected virtual void Start() {}

  // Update is called once per frame
  protected virtual void FixedUpdate() {
    _speed = (float)GetSpeed();
    if (_flightPhase != FlightPhase.INITIALIZED && _flightPhase != FlightPhase.READY) {
      _timeSinceBoost += Time.fixedDeltaTime;
    }
    _timeInPhase += Time.fixedDeltaTime;

    var launch_time = dynamicAgentConfig.dynamic_config.launch_config.launch_time;
    var boost_time = staticAgentConfig.boostConfig.boostTime;
    double elapsedSimulationTime = SimManager.Instance.GetElapsedSimulationTime();

    if (_flightPhase == FlightPhase.TERMINATED) {
      return;
    }

    if (_flightPhase == FlightPhase.INITIALIZED || _flightPhase == FlightPhase.READY) {
      float launchTimeVariance = 0.5f;
      float launchTimeNoise = Random.Range(-launchTimeVariance, launchTimeVariance);
      launch_time += launchTimeNoise;

      if (elapsedSimulationTime >= launch_time) {
        SetFlightPhase(FlightPhase.BOOST);
      }
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
    if (velocity.magnitude > 0.1f)  // Only align if we have significant velocity
    {
      // Create a rotation with forward along velocity and up along world up
      Quaternion targetRotation = Quaternion.LookRotation(velocity, Vector3.up);

      // Smoothly rotate towards the target rotation
      transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation,
                                                    10000f * Time.fixedDeltaTime);
    }
  }

  protected Vector3 CalculateAcceleration(Vector3 accelerationInput) {
    Vector3 gravity = Physics.gravity;
    float airDrag = CalculateDrag();
    float liftInducedDrag = CalculateLiftInducedDrag(accelerationInput + gravity);
    float dragAcceleration = -(airDrag + liftInducedDrag);

    // Project the drag acceleration onto the forward direction
    Vector3 dragAccelerationAlongRoll = dragAcceleration * transform.forward;
    _dragAcceleration = dragAccelerationAlongRoll;

    return accelerationInput + gravity + dragAccelerationAlongRoll;
  }

  protected float CalculateMaxForwardAcceleration() {
    return staticAgentConfig.accelerationConfig.maxForwardAcceleration;
  }

  protected float CalculateMaxNormalAcceleration() {
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
}

public class DummyAgent : Agent {
  protected override void Start() {
    base.Start();
  }

  protected override void FixedUpdate() {
    GetComponent<Rigidbody>().AddForce(_acceleration, ForceMode.Acceleration);
  }

  protected override void UpdateReady(double deltaTime) {
    // Do nothing
  }

  protected override void UpdateBoost(double deltaTime) {
    // Do nothing
  }

  protected override void UpdateMidCourse(double deltaTime) {
    // Do nothing
  }
}
