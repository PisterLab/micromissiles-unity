using UnityEngine;

// Base implementation of an agent.
public class AgentBase : MonoBehaviour, IAgent {
  public event AgentEventHandler OnTerminated;

  private const float _epsilon = 1e-12f;

  // Rigid body component.
  protected Rigidbody _rigidbody;

  // The acceleration field is not part of the rigid body component, so it is tracked separately.
  // The acceleration is applied as a force during each frame update.
  [SerializeField]
  protected Vector3 _acceleration;

  // The acceleration input is calculated by the controller and provided to the movement behavior.
  [SerializeField]
  protected Vector3 _accelerationInput;

  // The agent's position within the hierarchical strategy is given by the hierarchical agent.
  [SerializeField]
  private HierarchicalAgent _hierarchicalAgent;

  // Static configuration of the agent, including agent type, unit cost, acceleration configuration,
  // aerodynamics parameters, power table, and visualization configuration.
  [SerializeField]
  private Configs.StaticConfig _staticConfig;

  // Agent configuration, including initial state, attack behavior configuration (for threats),
  // dynamic configuration, and sub-agent configuration (for interceptors).
  [SerializeField]
  private Configs.AgentConfig _agentConfig;

  // Last sensing time.
  private float _lastSensingTime = Mathf.NegativeInfinity;

  public HierarchicalAgent HierarchicalAgent {
    get => _hierarchicalAgent;
    set => _hierarchicalAgent = value;
  }
  public Configs.StaticConfig StaticConfig {
    get => _staticConfig;
    set {
      _staticConfig = value;
      _rigidbody.mass = StaticConfig.BodyConfig?.Mass ?? 1;
    }
  }
  public Configs.AgentConfig AgentConfig {
    get => _agentConfig;
    set {
      _agentConfig = value;
      UpdateAgentConfig();
    }
  }

  // Movement behavior of the agent.
  public IMovement Movement { get; set; }

  // The controller calculates the acceleration input, given the agent's current state and its
  // target's current state.
  public IController Controller { get; set; }

  // The sensor calculates the relative transformation from the current agent to a target.
  public ISensor Sensor { get; set; }

  // Target model. The target model is updated by the sensor and should be used by the controller to
  // model imperfect knowledge of the engagement.
  public IAgent TargetModel { get; set; }

  public Vector3 Position {
    get => transform.position;
    set => transform.position = value;
  }
  public Vector3 Velocity {
    get => _rigidbody.linearVelocity;
    set => _rigidbody.linearVelocity = value;
  }
  public float Speed => Velocity.magnitude;
  public Vector3 Acceleration {
    get => _acceleration;
    set => _acceleration = value;
  }
  public Vector3 AccelerationInput {
    get => _accelerationInput;
    set => _accelerationInput = value;
  }

  // If true, the agent is able to pursue targets.
  public virtual bool IsPursuable => true;

  // Elapsed time since the creation of the agent.
  public float ElapsedTime { get; private set; } = 0f;

  // If true, the agent is terminated.
  public bool IsTerminated { get; private set; } = false;

  public float MaxForwardAcceleration() {
    return StaticConfig.AccelerationConfig?.MaxForwardAcceleration ?? 0;
  }

  public float MaxNormalAcceleration() {
    float maxReferenceNormalAcceleration =
        (StaticConfig.AccelerationConfig?.MaxReferenceNormalAcceleration ?? 0) * Constants.kGravity;
    float referenceSpeed = StaticConfig.AccelerationConfig?.ReferenceSpeed ?? 1;
    return Mathf.Pow(Speed / referenceSpeed, 2) * maxReferenceNormalAcceleration;
  }

  public void UpdateTargetModel() {
    if (HierarchicalAgent == null || HierarchicalAgent.Target == null || Sensor == null) {
      return;
    }
    if (HierarchicalAgent.Target.IsTerminated) {
      HierarchicalAgent.Target = null;
      return;
    }

    // Check whether the sensing period has elapsed.
    float sensingFrequency = AgentConfig?.DynamicConfig?.SensorConfig?.Frequency ?? Mathf.Infinity;
    float sensingPeriod = 1f / sensingFrequency;
    if (ElapsedTime - _lastSensingTime >= sensingPeriod) {
      // Sense the target.
      SensorOutput sensorOutput = Sensor.Sense(HierarchicalAgent.Target);
      TargetModel.Position = Position + sensorOutput.Position.Cartesian;
      TargetModel.Velocity = Velocity + sensorOutput.Velocity.Cartesian;
      TargetModel.Acceleration = Acceleration + sensorOutput.Acceleration.Cartesian;
      _lastSensingTime = ElapsedTime;
    }
  }

  public Transformation GetRelativeTransformation(IAgent target) {
    return GetRelativeTransformation(target.Position, target.Velocity, target.Acceleration);
  }

  public Transformation GetRelativeTransformation(IHierarchical target) {
    return GetRelativeTransformation(target.Position, target.Velocity, target.Acceleration);
  }

  public Transformation GetRelativeTransformation(in Vector3 waypoint) {
    return GetRelativeTransformation(waypoint, Vector3.zero, Vector3.zero);
  }

  public void Terminate() {
    HierarchicalAgent.Target = null;
    if (Movement is MissileMovement movement) {
      movement.FlightPhase = Simulation.FlightPhase.Terminated;
    }
    IsTerminated = true;
    OnTerminated?.Invoke(this);
    Destroy(gameObject);
  }

  // Awake is called before Start and right after a prefab is instantiated.
  protected virtual void Awake() {
    _rigidbody = GetComponent<Rigidbody>();
  }

  // Start is called before the first frame update.
  protected virtual void Start() {}

  // FixedUpdate is called multiple times per frame. All physics calculations and updates occur
  // immediately after FixedUpdate, and all movement values are multiplied by Time.deltaTime.
  protected virtual void FixedUpdate() {
    ElapsedTime += Time.fixedDeltaTime;

    UpdateTargetModel();
    AlignWithVelocity();
  }

  // Update is called every frame.
  protected virtual void Update() {}

  // LateUpdate is called every frame after all Update functions have been called.
  protected virtual void LateUpdate() {}

  // OnDestroy is called when the object is being destroyed.
  protected virtual void OnDestroy() {}

  // UpdateAgentConfig is called whenever the agent configuration is changed.
  protected virtual void UpdateAgentConfig() {
    // Set the sensor.
    switch (AgentConfig.DynamicConfig?.SensorConfig?.Type) {
      case Simulation.SensorType.Ideal: {
        Sensor = new IdealSensor(this);
        break;
      }
      default: {
        Debug.LogError($"Sensor type {AgentConfig.DynamicConfig?.SensorConfig?.Type} not found.");
        break;
      }
    }
  }

  protected virtual void OnDrawGizmos() {
    if (Application.isPlaying) {
      Gizmos.color = Color.green;
      Gizmos.DrawRay(Position, _accelerationInput);
    }
  }

  private void AlignWithVelocity() {
    const float speedThreshold = 0.1f;
    const float rotationSpeedDegreesPerSecond = 10000f;

    // Only align if the velocity is significant.
    if (Speed > speedThreshold) {
      // Create a rotation with the forward direction along the velocity vector and the up direction
      // along world up.
      Quaternion targetRotation = Quaternion.LookRotation(Velocity, Vector3.up);

      // Smoothly rotate towards the target rotation.
      transform.rotation = Quaternion.RotateTowards(
          transform.rotation, targetRotation,
          maxDegreesDelta: rotationSpeedDegreesPerSecond * Time.fixedDeltaTime);
    }
  }

  private Transformation GetRelativeTransformation(in Vector3 position, in Vector3 velocity,
                                                   in Vector3 acceleration) {
    // Get the relative position transformation.
    Vector3 relativePosition = position - Position;
    PositionTransformation positionTransformation =
        GetRelativePositionTransformation(relativePosition);

    // Get the relative velocity transformation.
    VelocityTransformation velocityTransformation =
        GetRelativeVelocityTransformation(relativePosition, velocity - Velocity);

    // Get the relative acceleration transformation.
    // Since the agent's acceleration is an input and can be set arbitrarily, the relative
    // acceleration is just the other agent's acceleration.
    AccelerationTransformation accelerationTransformation =
        GetRelativeAccelerationTransformation(acceleration);
    return new Transformation {
      Position = positionTransformation,
      Velocity = velocityTransformation,
      Acceleration = accelerationTransformation,
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

    // The target azimuth vector is orthogonal to the relative position vector and points to the
    // starboard of the target along the azimuth-elevation sphere.
    Vector3 targetAzimuth = Vector3.Cross(transform.up, relativePosition);
    // The target elevation vector is orthogonal to the relative position vector and points upwards
    // from the target along the azimuth-elevation sphere.
    Vector3 targetElevation = Vector3.Cross(relativePosition, transform.right);
    // If the relative position vector is parallel to the yaw or pitch axis, the target azimuth
    // vector or the target elevation vector will be undefined.
    if (targetAzimuth.sqrMagnitude < _epsilon) {
      targetAzimuth = Vector3.Cross(targetElevation, relativePosition);
    } else if (targetElevation.sqrMagnitude < _epsilon) {
      targetElevation = Vector3.Cross(relativePosition, targetAzimuth);
    }

    // Project the relative velocity vector on the azimuth-elevation sphere onto the target azimuth
    // vector.
    Vector3 tangentialVelocityOnAzimuth = Vector3.Project(tangentialVelocity, targetAzimuth);

    // Calculate the time derivative of the azimuth to the target.
    float azimuth = tangentialVelocityOnAzimuth.magnitude / relativePosition.magnitude;
    if (Vector3.Dot(tangentialVelocityOnAzimuth, targetAzimuth) < 0) {
      azimuth *= -1;
    }

    // Project the velocity vector on the azimuth-elevation sphere onto the target elevation vector.
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
}
