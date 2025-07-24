using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Agent : MonoBehaviour {
  [SerializeField]
  protected List<Agent> _interceptors = new List<Agent>();
  [SerializeField]
  protected Agent _target;
  [SerializeField]
  protected Agent _targetModel;

  public StaticAgentConfig staticAgentConfig;
  public DynamicAgentConfig dynamicAgentConfig;

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

  public virtual void UnassignTarget() {
    if (HasAssignedTarget()) {
      _target.RemoveInterceptor(this);
    }
    _target = null;
    _targetModel = null;
  }

  public virtual bool IsTerminated() {
    return !gameObject.activeSelf;
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
    OnTerminated?.Invoke(this);
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

  public virtual void SetInitialVelocity(Vector3 velocity) {
    // Default implementation for InterceptorOrigin - just set velocity directly
    SetVelocity(velocity);
  }

  public virtual Vector3 GetAcceleration() {
    // This will need to be revisited as it was moved to AirborneAgent
    return Vector3.zero;
  }

  public void SetAcceleration(Vector3 acceleration) {
    // This will need to be revisited as it was moved to AirborneAgent
  }

  public Transformation GetRelativeTransformation(Agent target) {
    Transformation transformation = new Transformation();

    // Get the relative position transformation.
    transformation.position =
        GetRelativePositionTransformation(target.GetPosition() - GetPosition());

    // Get the relative velocity transformation.
    transformation.velocity = GetRelativeVelocityTransformation(
        target.GetPosition() - GetPosition(), target.GetVelocity() - GetVelocity());

    // Get the relative acceleration transformation.
    transformation.acceleration = GetRelativeAccelerationTransformation(target);
    return transformation;
  }

  public Transformation GetRelativeTransformationToWaypoint(Vector3 waypoint) {
    Transformation transformation = new Transformation();

    // Get the relative position transformation.
    transformation.position = GetRelativePositionTransformation(waypoint - GetPosition());

    // Get the relative velocity transformation.
    transformation.velocity =
        GetRelativeVelocityTransformation(waypoint - GetPosition(), -GetVelocity());

    return transformation;
  }

  private PositionTransformation GetRelativePositionTransformation(Vector3 relativePosition) {
    PositionTransformation positionTransformation = new PositionTransformation();

    // Set the relative position in Cartesian coordinates.
    positionTransformation.cartesian = relativePosition;

    // Calculate the distance (range) to the target.
    positionTransformation.range = relativePosition.magnitude;

    Vector3 flatRelativePosition = Vector3.ProjectOnPlane(relativePosition, transform.up);
    Vector3 verticalRelativePosition = relativePosition - flatRelativePosition;

    // Calculate the elevation (vertical angle relative to forward).
    positionTransformation.elevation =
        Mathf.Atan(verticalRelativePosition.magnitude / flatRelativePosition.magnitude);

    // Calculate the azimuth (horizontal angle relative to forward).
    if (flatRelativePosition.magnitude == 0) {
      positionTransformation.azimuth = 0;
    } else {
      positionTransformation.azimuth =
          Vector3.SignedAngle(transform.forward, flatRelativePosition, transform.up) * Mathf.PI /
          180;
    }

    return positionTransformation;
  }

  private VelocityTransformation GetRelativeVelocityTransformation(Vector3 relativePosition,
                                                                   Vector3 relativeVelocity) {
    VelocityTransformation velocityTransformation = new VelocityTransformation();

    // Set the relative velocity in Cartesian coordinates.
    velocityTransformation.cartesian = relativeVelocity;

    // Calculate range rate (radial velocity).
    velocityTransformation.range = Vector3.Dot(relativeVelocity, relativePosition.normalized);

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
    if (targetAzimuth.magnitude == 0) {
      targetAzimuth = Vector3.Cross(targetElevation, relativePosition);
    } else if (targetElevation.magnitude == 0) {
      targetElevation = Vector3.Cross(relativePosition, targetAzimuth);
    }

    // Project the relative velocity vector on the azimuth-elevation sphere onto
    // the target azimuth vector.
    Vector3 tangentialVelocityOnAzimuth = Vector3.Project(tangentialVelocity, targetAzimuth);

    // Calculate the time derivative of the azimuth to the target.
    velocityTransformation.azimuth =
        tangentialVelocityOnAzimuth.magnitude / relativePosition.magnitude;
    if (Vector3.Dot(tangentialVelocityOnAzimuth, targetAzimuth) < 0) {
      velocityTransformation.azimuth *= -1;
    }

    // Project the velocity vector on the azimuth-elevation sphere onto the target
    // elevation vector.
    Vector3 tangentialVelocityOnElevation = Vector3.Project(tangentialVelocity, targetElevation);

    // Calculate the time derivative of the elevation to the target.
    velocityTransformation.elevation =
        tangentialVelocityOnElevation.magnitude / relativePosition.magnitude;
    if (Vector3.Dot(tangentialVelocityOnElevation, targetElevation) < 0) {
      velocityTransformation.elevation *= -1;
    }

    return velocityTransformation;
  }

  private AccelerationTransformation GetRelativeAccelerationTransformation(Agent agent) {
    // Since the agent's acceleration is an input, the relative acceleration is just the agent's
    // acceleration.
    AccelerationTransformation accelerationTransformation = new AccelerationTransformation();
    accelerationTransformation.cartesian = agent.GetAcceleration();
    return accelerationTransformation;
  }

  protected virtual void Awake() {}
  protected virtual void Start() {}
  protected virtual void FixedUpdate() {}
}

public class DummyAgent : Agent {
  protected override void Start() {
    base.Start();
  }

  protected override void FixedUpdate() {
    GetComponent<Rigidbody>().AddForce(GetAcceleration(), ForceMode.Acceleration);
  }
}
