using UnityEngine;

// Interface for an agent.
//
// An agent represents a physical entity, such as a ship, an interceptor, or a threat subject to the
// laws of physics.
public interface IAgent {
  HierarchicalAgent HierarchicalAgent { get; set; }

  Configs.StaticConfig StaticConfig { get; set; }
  Configs.AgentConfig AgentConfig { get; set; }

  IMovement Movement { get; set; }
  IController Controller { get; set; }

  Vector3 Position { get; set; }
  Vector3 Velocity { get; set; }
  float Speed { get; }
  Vector3 Acceleration { get; set; }
  Vector3 AccelerationInput { get; set; }

  Transform transform { get; }

  Transformation GetRelativeTransformation(IAgent target);
  Transformation GetRelativeTransformation(IHierarchical target);
  Transformation GetRelativeTransformation(in Vector3 waypoint);
}
