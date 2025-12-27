using UnityEngine;

// Interface for an agent.
//
// An agent represents a physical entity, such as a ship, an interceptor, or a threat subject to the
// laws of physics.

public delegate void AgentTerminatedEventHandler(IAgent agent);

public interface IAgent {
  event AgentTerminatedEventHandler OnTerminated;

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
  Quaternion InverseRotation { get; }

  float MaxForwardAcceleration();
  float MaxNormalAcceleration();

  // Create a dummy agent to represent the target for sensor tracking and control.
  // This function should be called when a target is assigned to this agent.
  void CreateTargetModel(IHierarchical target);

  // Destroy the target model dummy agent and clear the target model reference.
  // This function should be called when the target is unassigned or no longer valid.
  void DestroyTargetModel();

  // Update the target model dummy agent according to the sensor.
  void UpdateTargetModel();

  void Terminate();

  Transformation GetRelativeTransformation(IAgent target);
  Transformation GetRelativeTransformation(IHierarchical target);
  Transformation GetRelativeTransformation(in Vector3 waypoint);
}
