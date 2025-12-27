using System.Collections.Generic;
using UnityEngine;

// Base implementation of a launch angle data interpolator.
//
// The launch angle data interpolator interpolates the values from a static list of values.
public abstract class LaunchAngleDataInterpolatorBase : LaunchAngleInterpolatorBase {
  public LaunchAngleDataInterpolatorBase(IAgent agent) : base(agent) {}

  // Initialize the interpolator.
  protected override void InitInterpolator() {
    var interpolatorDataPoints = new List<Interpolator2DDataPoint>();
    foreach (var dataPoint in GenerateData()) {
      var interpolatorDataPoint = new Interpolator2DDataPoint {
        Coordinates = new Vector2(dataPoint.Input.Distance, dataPoint.Input.Altitude),
        Data = new List<float> { dataPoint.Output.LaunchAngle, dataPoint.Output.TimeToPosition },
      };
      interpolatorDataPoints.Add(interpolatorDataPoint);
    }
    _interpolator = new NearestNeighborInterpolator2D(interpolatorDataPoints);
  }

  // Generate the launch angle data points to interpolate.
  protected abstract IEnumerable<LaunchAngleDataPoint> GenerateData();
}
