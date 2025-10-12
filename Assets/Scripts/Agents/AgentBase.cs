using UnityEngine;

// Base implementation of an agent.
public abstract class AgentBase : MonoBehaviour, IAgent {
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
  // The acceleration field is not part of the Rigidbody component, so it is tracked separately. The
  // acceleration is applied as a force during each frame update.
  private Vector3 _acceleration;

  [SerializeField]
  // The acceleration input is calculated by the controller and provided to the movement behavior.
  private Vector3 _accelerationInput;

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
    get => GetComponent<Rigidbody>().linearVelocity;
    set => GetComponent<Rigidbody>().linearVelocity = value;
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

  // TODO(titan): Event handlers for hits and misses.

  // TODO(titan): Methods for updating the agent and terminating the agent.

  public Transformation GetRelativeTransformation(IAgent target) {
    return GetRelativeTransformation(target.HierarchicalAgent);
  }

  public Transformation GetRelativeTransformation(IHierarchical target) {
    Transformation transformation = new Transformation();

    // Get the relative position transformation.
    transformation.position = GetRelativePositionTransformation(target.Position - Position);

    // Get the relative velocity transformation.
    transformation.velocity =
        GetRelativeVelocityTransformation(target.Position - Position, target.Velocity - Velocity);

    // Get the relative acceleration transformation.
    transformation.acceleration = GetRelativeAccelerationTransformation(target.Acceleration);
    return transformation;
  }

  public Transformation GetRelativeTransformation(in Vector3 waypoint) {
    Transformation transformation = new Transformation();

    // Get the relative position transformation.
    transformation.position = GetRelativePositionTransformation(waypoint - Position);

    // Get the relative velocity transformation.
    transformation.velocity = GetRelativeVelocityTransformation(waypoint - Position, -Velocity);
    return transformation;
  }

  private PositionTransformation GetRelativePositionTransformation(in Vector3 relativePosition) {
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

  private VelocityTransformation GetRelativeVelocityTransformation(in Vector3 relativePosition,
                                                                   in Vector3 relativeVelocity) {
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

  private AccelerationTransformation GetRelativeAccelerationTransformation(
      in Vector3 relativeAcceleration) {
    // Since the agent's acceleration is an input, the relative acceleration is just the agent's
    // acceleration.
    AccelerationTransformation accelerationTransformation = new AccelerationTransformation();
    accelerationTransformation.cartesian = relativeAcceleration;
    return accelerationTransformation;
  }
}
