using System.Collections.Generic;
using UnityEngine;

// Interface for a 2D interpolator.
//
// The 2D interpolator class interpolates values on a 2D grid.
// The first two columns specify the coordinates of each data point that are used for interpolating
// the remaining data values.
public interface IInterpolator2D {
  // 2D interpolator data points.
  IReadOnlyList<Interpolator2DDataPoint> Data { get; }

  // Interpolate the value.
  Interpolator2DDataPoint Interpolate(float x, float y);
  Interpolator2DDataPoint Interpolate(in Vector2 coordinates);
}
