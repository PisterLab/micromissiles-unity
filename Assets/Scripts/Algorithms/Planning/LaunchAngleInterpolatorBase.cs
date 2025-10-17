using System;
using System.Collections.Generic;
using UnityEngine;

// Base implementation of a launch angle interpolator.
//
// The interpolator determines the optimal launch angle and the time-to-target given the horizontal
// distance and altitude of the target.
public abstract class LaunchAngleInterpolatorBase : LaunchAnglePlannerBase {
  // Launch angle data interpolator.
  protected IInterpolator2D _interpolator;

  public LaunchAngleInterpolatorBase(IAgent agent) : base(agent) {}

  // Calculate the optimal launch angle in degrees and the time-to-target in seconds.
  public override LaunchAngleOutput Plan(in LaunchAngleInput input) {
    if (_interpolator == null) {
      InitInterpolator();
    }

    var interpolatedDataPoint = _interpolator.Interpolate(input.Distance, input.Altitude);
    if (interpolatedDataPoint == null || interpolatedDataPoint.Data == null ||
        interpolatedDataPoint.Data.Count < 2) {
      throw new InvalidOperationException("Interpolator returned invalid data.");
    }

    return new LaunchAngleOutput { LaunchAngle = interpolatedDataPoint.Data[0],
                                   TimeToPosition = interpolatedDataPoint.Data[1] };
  }

  // Return the absolute intercept position given the absolute target position.
  public override Vector3 InterceptPosition(in Vector3 targetPosition) {
    var direction = ConvertToRelativeDirection(targetPosition);
    var interpolatedDataPoint = _interpolator.Interpolate(direction[0], direction[1]);
    var relativePosition = targetPosition - Agent.Position;
    var cylindricalRelativePosition = Coordinates3.ConvertCartesianToCylindrical(relativePosition);
    return Coordinates3.ConvertCylindricalToCartesian(
               r: interpolatedDataPoint.Coordinates[0], azimuth: cylindricalRelativePosition.y,
               height: interpolatedDataPoint.Coordinates[1]) +
           Agent.Position;
  }

  // Initialize the interpolator.
  protected abstract void InitInterpolator();
}
