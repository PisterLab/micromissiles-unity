using UnityEngine;

// Base implementation of an agent.
public class AgentBase : MonoBehaviour, IAgent {
  private const float _epsilon = 1e-12f;

  [SerializeField]
  // The agent's position within the hierarchical strategy is given by the hierarchical agent.
  private HierarchicalAgent _hierarchicalAgent;

  [SerializeField]
  // Static configuration of the agent, including agent type, unit cost, acceleration configuration,
  // aerodynamics parameters, power table, and visualization configuration.
  private Configs.StaticConfig _staticConfig;

  [SerializeField]
  // Agent configuration, including initial state, attack behavior configuration (for threats),
  // dynamic configuration, and sub-agent configuration (for interceptors).
  private Configs.AgentConfig _agentConfig;

  // Movement behavior of the agent.
  private IMovement _movement;

  // The controller calculates the acceleration input, given the agent's current state and its
  // target's current state.
  private IController _controller;

  [SerializeField]
  // The acceleration field is not part of the rigid body component, so it is tracked separately.
  // The acceleration is applied as a force during each frame update.
  private Vector3 _acceleration;

  [SerializeField]
  // The acceleration input is calculated by the controller and provided to the movement behavior.
  private Vector3 _accelerationInput;

  // Rigid body component.
  private Rigidbody _rigidbody;

  public HierarchicalAgent HierarchicalAgent {
    get => _hierarchicalAgent;
    set => _hierarchicalAgent = value;
  }
  public Configs.StaticConfig StaticConfig {
    get => _staticConfig;
    set => _staticConfig = value;
  }
  public Configs.AgentConfig AgentConfig {
    get => _agentConfig;
    set => _agentConfig = value;
  }
  public IMovement Movement {
    get => _movement;
    set => _movement = value;
  }
  public IController Controller {
    get => _controller;
    set => _controller = value;
  }
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

  private void Awake() {
    _rigidbody = GetComponent<Rigidbody>();
  }

  // TODO(titan): Event handlers for hits and misses.

  // TODO(titan): Methods for updating the agent and terminating the agent.

  public Transformation GetRelativeTransformation(IAgent target) {
    return GetRelativeTransformation(target.Position, target.Velocity, target.Acceleration);
  }

  public Transformation GetRelativeTransformation(IHierarchical target) {
    return GetRelativeTransformation(target.Position, target.Velocity, target.Acceleration);
  }

  public Transformation GetRelativeTransformation(in Vector3 waypoint) {
    return GetRelativeTransformation(waypoint, Vector3.zero, Vector3.zero);
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
    var transformation = new Transformation();

    // Get the relative position transformation.
    Vector3 relativePosition = position - Position;
    transformation.Position = GetRelativePositionTransformation(relativePosition);

    // Get the relative velocity transformation.
    transformation.Velocity =
        GetRelativeVelocityTransformation(relativePosition, velocity - Velocity);

    // Get the relative acceleration transformation.
    // Since the agent's acceleration is an input and can be set arbitrarily, the relative
    // acceleration is just the other agent's acceleration.
    transformation.Acceleration = GetRelativeAccelerationTransformation(acceleration);
    return transformation;
  }

  private PositionTransformation GetRelativePositionTransformation(in Vector3 relativePosition) {
    PositionTransformation positionTransformation = new PositionTransformation();

    // Set the relative position in Cartesian coordinates.
    positionTransformation.Cartesian = relativePosition;

    // Calculate the distance (range) to the target.
    positionTransformation.Range = relativePosition.magnitude;

    Vector3 flatRelativePosition = Vector3.ProjectOnPlane(relativePosition, transform.up);
    Vector3 verticalRelativePosition = relativePosition - flatRelativePosition;

    // Calculate the elevation (vertical angle relative to forward).
    positionTransformation.Elevation =
        Mathf.Atan2(verticalRelativePosition.magnitude, flatRelativePosition.magnitude);

    // Calculate the azimuth (horizontal angle relative to forward).
    if (flatRelativePosition.sqrMagnitude < _epsilon) {
      positionTransformation.Azimuth = 0;
    } else {
      positionTransformation.Azimuth =
          Vector3.SignedAngle(transform.forward, flatRelativePosition, transform.up) *
          Mathf.Deg2Rad;
    }

    return positionTransformation;
  }

  private VelocityTransformation GetRelativeVelocityTransformation(in Vector3 relativePosition,
                                                                   in Vector3 relativeVelocity) {
    var velocityTransformation = new VelocityTransformation();

    // Set the relative velocity in Cartesian coordinates.
    velocityTransformation.Cartesian = relativeVelocity;

    if (relativePosition.sqrMagnitude < _epsilon) {
      velocityTransformation.Range = relativeVelocity.magnitude;
      velocityTransformation.Azimuth = 0;
      velocityTransformation.Elevation = 0;
      return velocityTransformation;
    }

    // Calculate range rate (radial velocity).
    velocityTransformation.Range = Vector3.Dot(relativeVelocity, relativePosition.normalized);

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
    velocityTransformation.Azimuth =
        tangentialVelocityOnAzimuth.magnitude / relativePosition.magnitude;
    if (Vector3.Dot(tangentialVelocityOnAzimuth, targetAzimuth) < 0) {
      velocityTransformation.Azimuth *= -1;
    }

    // Project the velocity vector on the azimuth-elevation sphere onto the target
    // elevation vector.
    Vector3 tangentialVelocityOnElevation = Vector3.Project(tangentialVelocity, targetElevation);

    // Calculate the time derivative of the elevation to the target.
    velocityTransformation.Elevation =
        tangentialVelocityOnElevation.magnitude / relativePosition.magnitude;
    if (Vector3.Dot(tangentialVelocityOnElevation, targetElevation) < 0) {
      velocityTransformation.Elevation *= -1;
    }

    return velocityTransformation;
  }

  private AccelerationTransformation GetRelativeAccelerationTransformation(
      in Vector3 relativeAcceleration) {
    var accelerationTransformation = new AccelerationTransformation();
    accelerationTransformation.Cartesian = relativeAcceleration;
    return accelerationTransformation;
  }
}
