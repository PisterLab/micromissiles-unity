using UnityEngine;

// The launch planner outputs a launch plan.
public struct LaunchPlan {
  // If true, the interceptor should be launched.
  public bool ShouldLaunch { get; init; }

  // Launch angle in degrees measured from the horizon.
  public float LaunchAngle { get; init; }

  // Absolute intercept position.
  public Vector3 InterceptPosition { get; init; }

  // No-launch launch plan.
  public static LaunchPlan NoLaunch() {
    return new LaunchPlan { ShouldLaunch = false };
  }

  // Get the normalized launch vector given the agent's position.
  public Vector3 NormalizedLaunchVector(in Vector3 position) {
    Vector3 relativeInterceptPosition = InterceptPosition - position;
    Vector3 interceptDirection =
        Coordinates3.ConvertCartesianToSpherical(relativeInterceptPosition);
    return Coordinates3.ConvertSphericalToCartesian(r: 1, azimuth: interceptDirection[1],
                                                    elevation: LaunchAngle);
  }
}
