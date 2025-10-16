using UnityEngine;

// The launch planner outputs a launch plan.
public struct LaunchPlan {
  // If true, the interceptor should be launched.
  public bool ShouldLaunch { get; set; }

  // Launch angle in degrees measured from the horizon.
  public float LaunchAngle { get; set; }

  // Intercept position relative to the launcher's position.
  public Vector3 RelativeInterceptPosition { get; set; }

  // No-launch launch plan.
  public static LaunchPlan NoLaunch() {
    return new LaunchPlan { ShouldLaunch = false };
  }

  // Get the normalized launch vector.
  public Vector3 NormalizedLaunchVector() {
    var interceptDirection = Coordinates3.ConvertCartesianToSpherical(RelativeInterceptPosition);
    return Coordinates3.ConvertSphericalToCartesian(r: 1, azimuth: interceptDirection[1],
                                                    elevation: LaunchAngle);
  }
}
