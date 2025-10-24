using UnityEngine;

// Base implementation of a launch angle planner.
public abstract class LaunchAnglePlannerBase : ILaunchAnglePlanner {
  public IAgent Agent { get; set; }

  public LaunchAnglePlannerBase(IAgent agent) {
    Agent = agent;
  }

  // Calculate the optimal launch angle in degrees and the time-to-target in seconds.
  public abstract LaunchAngleOutput Plan(in LaunchAngleInput input);
  public LaunchAngleOutput Plan(in Vector3 targetPosition) {
    Direction direction = ConvertToRelativeDirection(targetPosition);
    return Plan(
        new LaunchAngleInput { Distance = direction.Distance, Altitude = direction.Altitude });
  }

  // Return the absolute intercept position given the absolute target position.
  public abstract Vector3 InterceptPosition(in Vector3 targetPosition);

  // Convert from a 3D vector to a 2D direction that ignores the azimuth and that is relative to the
  // agent's position.
  protected Direction ConvertToRelativeDirection(in Vector3 position) {
    Vector3 relativePosition = position - Agent.Position;
    return new Direction { Distance =
                               Vector3.ProjectOnPlane(relativePosition, Vector3.up).magnitude,
                           Altitude = Vector3.Project(relativePosition, Vector3.up).y };
  }
}
