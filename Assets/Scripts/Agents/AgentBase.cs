using UnityEngine;

// Base implementation of an agent.
//
// See the agent interface for property and method documentation.
public class AgentBase : MonoBehaviour, IAgent {
  public event AgentTerminatedEventHandler OnTerminated;

  private const float _epsilon = 1e-12f;

  // Rigid body component.
  protected Rigidbody _rigidbody;

  // The position field is cached and is updated before every fixed update.
  [SerializeField]
  private Vector3 _position;

  // The acceleration field is not part of the rigid body component, so it is tracked separately.
  // The acceleration is applied as a force during each frame update.
  [SerializeField]
  private Vector3 _acceleration;

  // The acceleration input is calculated by the controller and provided to the movement behavior.
  [SerializeField]
  private Vector3 _accelerationInput;

  // The agent's position within the hierarchical strategy is given by the hierarchical agent.
  [SerializeReference]
  private HierarchicalAgent _hierarchicalAgent;

  // Static configuration of the agent, including agent type, unit cost, acceleration configuration,
  // aerodynamics parameters, power table, and visualization configuration.
  private Configs.StaticConfig _staticConfig;

  // Agent configuration, including initial state, attack behavior configuration (for threats),
  // dynamic configuration, and sub-agent configuration (for interceptors).
  private Configs.AgentConfig _agentConfig;

  // Last sensing time.
  [SerializeField]
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

  public IMovement Movement { get; set; }
  public IController Controller { get; set; }
  public ISensor Sensor { get; set; }
  public IAgent TargetModel { get; set; }

  public Vector3 Position {
    get => _position;
    set {
      Transform.position = value;
      _position = value;
    }
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
  public virtual bool IsPursuer => true;

  // Elapsed time since the creation of the agent.
  public float ElapsedTime { get; private set; } = 0f;

  // If true, the agent is terminated.
  public bool IsTerminated { get; private set; } = false;

  // The agent transform is cached.
  public Transform Transform { get; private set; }

  // The up direction is cached and updated before every fixed update.
  public Vector3 Up { get; private set; }

  // The forward direction is cached and updated before every fixed update.
  public Vector3 Forward { get; private set; }

  // The right direction is cached and updated before every fixed update.
  public Vector3 Right { get; private set; }

  // The inverse rotation is cached and updated before every fixed update.
  public Quaternion InverseRotation { get; private set; }

  public float MaxForwardAcceleration() {
    return StaticConfig.AccelerationConfig?.MaxForwardAcceleration ?? 0;
  }

  public float MaxNormalAcceleration() {
    float maxReferenceNormalAcceleration =
        (StaticConfig.AccelerationConfig?.MaxReferenceNormalAcceleration ?? 0) * Constants.kGravity;
    float referenceSpeed = StaticConfig.AccelerationConfig?.ReferenceSpeed ?? 1;
    return Mathf.Pow(Speed / referenceSpeed, 2) * maxReferenceNormalAcceleration;
  }

  public void CreateTargetModel(IHierarchical target) {
    TargetModel = SimManager.Instance.CreateDummyAgent(target.Position, target.Velocity);
  }

  public void DestroyTargetModel() {
    if (TargetModel != null) {
      SimManager.Instance.DestroyDummyAgent(TargetModel);
      TargetModel = null;
    }
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
    return GetRelativeTransformation(waypoint, velocity: Vector3.zero, acceleration: Vector3.zero);
  }

  public void Terminate() {
    if (HierarchicalAgent != null) {
      HierarchicalAgent.Target = null;
    }
    if (Movement is MissileMovement movement) {
      movement.FlightPhase = Simulation.FlightPhase.Terminated;
    }
    IsTerminated = true;
    OnTerminated?.Invoke(this);
    Destroy(gameObject);
  }

  // Awake is called before Start and right after a prefab is instantiated.
  protected virtual void Awake() {
    Transform = transform;
    _rigidbody = GetComponent<Rigidbody>();

    UpdateTransformData();
    if (EarlyFixedUpdateManager.Instance != null) {
      EarlyFixedUpdateManager.Instance.OnEarlyFixedUpdate += UpdateTransformData;
    }
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
  protected virtual void OnDestroy() {
    if (EarlyFixedUpdateManager.Instance != null) {
      EarlyFixedUpdateManager.Instance.OnEarlyFixedUpdate -= UpdateTransformData;
    }
  }

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

  protected bool CheckGroundCollision(Collider other) {
    // Check if the agent hit the ground with a negative vertical speed.
    return other.gameObject.name == "Floor" && Vector3.Dot(Velocity, Vector3.up) < 0;
  }

  protected bool ShouldIgnoreCollision(IAgent otherAgent) {
    // Dummy agents are virtual targets and should not trigger collisions.
    return otherAgent == null || otherAgent is DummyAgent || otherAgent.IsTerminated;
  }

  private void UpdateTransformData() {
    _position = Transform.position;
    Up = Transform.up;
    Forward = Transform.forward;
    Right = Transform.right;
    InverseRotation = Quaternion.Inverse(Transform.rotation);
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
      Transform.rotation = Quaternion.RotateTowards(
          Transform.rotation, targetRotation,
          maxDegreesDelta: rotationSpeedDegreesPerSecond * Time.fixedDeltaTime);
    }
  }

  private Transformation GetRelativeTransformation(in Vector3 position, in Vector3 velocity,
                                                   in Vector3 acceleration) {
    Vector3 relativePosition = position - Position;
    Vector3 relativeLocalPosition = InverseRotation * relativePosition;
    Vector3 relativeVelocity = velocity - Velocity;
    Vector3 relativeLocalVelocity = InverseRotation * relativeVelocity;

    float x = relativeLocalPosition.x;
    float y = relativeLocalPosition.y;
    float z = relativeLocalPosition.z;

    float horizontalSqr = x * x + z * z;
    float horizontal = Mathf.Sqrt(horizontalSqr);
    float rangeSqr = horizontalSqr + y * y;
    float range = Mathf.Sqrt(rangeSqr);

    float azimuth = Mathf.Atan2(x, z);
    float elevation = Mathf.Atan2(y, horizontal);
    var positionTransformation = new PositionTransformation {
      Cartesian = relativePosition,
      Range = range,
      Azimuth = azimuth,
      Elevation = elevation,
    };

    float rangeRate =
        range > _epsilon ? Vector3.Dot(relativeLocalVelocity, relativeLocalPosition) / range : 0f;
    float azimuthRate = 0f;
    float elevationRate = 0f;
    if (horizontal > _epsilon) {
      azimuthRate = -(x * relativeLocalVelocity.z - z * relativeLocalVelocity.x) / horizontalSqr;
      elevationRate =
          (relativeLocalVelocity.y * horizontal -
           y * (x * relativeLocalVelocity.x + z * relativeLocalVelocity.z) / horizontal) /
          rangeSqr;
    } else {
      // The other agent is exactly above or below.
      azimuthRate = 0f;
      float horizontalSpeed = Mathf.Sqrt(relativeLocalVelocity.x * relativeLocalVelocity.x +
                                         relativeLocalVelocity.z * relativeLocalVelocity.z);
      elevationRate = -horizontalSpeed / (Mathf.Abs(y) > _epsilon ? y : Mathf.Sign(y) * _epsilon);
    }
    var velocityTransformation = new VelocityTransformation {
      Cartesian = relativeVelocity,
      Range = rangeRate,
      Azimuth = azimuthRate,
      Elevation = elevationRate,
    };

    var accelerationTransformation = new AccelerationTransformation {
      Cartesian = acceleration,
    };
    return new Transformation {
      Position = positionTransformation,
      Velocity = velocityTransformation,
      Acceleration = accelerationTransformation,
    };
  }
}
