using UnityEngine;

// Interface for an agent.
//
// An agent represents a physical entity, such as a ship, an interceptor, or a threat subject to the
// laws of physics.

public delegate void AgentEventHandler(IAgent agent);

public interface IAgent {
  event AgentEventHandler OnTerminated;

  HierarchicalAgent HierarchicalAgent { get; set; }

  Configs.StaticConfig StaticConfig { get; set; }
  Configs.AgentConfig AgentConfig { get; set; }

  // Movement behavior of the agent.
  IMovement Movement { get; set; }

  // The controller calculates the acceleration input, given the agent's current state and its
  // target's current state.
  IController Controller { get; set; }

  // The sensor calculates the relative transformation from the current agent to a target.
  ISensor Sensor { get; set; }

  // Target model. The target model is updated by the sensor and should be used by the controller to
  // model imperfect knowledge of the engagement.
  IAgent TargetModel { get; set; }

  Vector3 Position { get; set; }
  Vector3 Velocity { get; set; }
  float Speed { get; }
  Vector3 Acceleration { get; set; }
  Vector3 AccelerationInput { get; set; }

  bool IsPursuer { get; }
  float ElapsedTime { get; }
  bool IsTerminated { get; }

  GameObject gameObject { get; }
  Transform Transform { get; }
  Vector3 Up { get; }
  Vector3 Forward { get; }
  Vector3 Right { get; }

  float MaxForwardAcceleration();
  float MaxNormalAcceleration();

  void CreateTargetModel(IHierarchical target);
  void DestroyTargetModel();
  void UpdateTargetModel();

  void Terminate();

  Transformation GetRelativeTransformation(IAgent target);
  Transformation GetRelativeTransformation(IHierarchical target);
  Transformation GetRelativeTransformation(in Vector3 waypoint);
}
