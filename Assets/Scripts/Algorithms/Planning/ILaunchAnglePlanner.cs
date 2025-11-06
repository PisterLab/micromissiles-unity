using UnityEngine;

// Interface for a launch angle planner.
//
// The launch angle planner outputs the optimal launch angle and the time-to-target.
public interface ILaunchAnglePlanner {
  // Launcher agent.
  IAgent Agent { get; set; }

  // Calculate the optimal launch angle in degrees and the time-to-target in seconds.
  LaunchAngleOutput Plan(in LaunchAngleInput input);
  LaunchAngleOutput Plan(in Vector3 targetPosition);

  // Return the absolute intercept position given the target position.
  Vector3 InterceptPosition(in Vector3 targetPosition);
}
