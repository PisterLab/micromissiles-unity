using System.Collections.Generic;
using UnityEngine;

// The 2D nearest neighbor interpolator class interpolates values on a 2D grid using nearest
// neighbor interpolation.
public class NearestNeighborInterpolator2D : Interpolator2DBase {
  // K-D tree for nearest neighbor interpolation.
  private KDTree<Interpolator2DDataPoint> _tree;

  public NearestNeighborInterpolator2D(IEnumerable<Interpolator2DDataPoint> data) : base(data) {
    _tree = new KDTree<Interpolator2DDataPoint>(
        _data, (Interpolator2DDataPoint point) => point.Coordinates);
  }
  public NearestNeighborInterpolator2D(string[] csvLines) : base(csvLines) {
    _tree = new KDTree<Interpolator2DDataPoint>(
        _data, (Interpolator2DDataPoint point) => point.Coordinates);
  }

  // Interpolate the value using nearest neighbor interpolation.
  public override Interpolator2DDataPoint Interpolate(float x, float y) {
    var closestPoint = _tree.NearestNeighbor(new Vector2(x, y));
    if (closestPoint == null) {
      Debug.LogError("No data points available for interpolation.");
      return new Interpolator2DDataPoint { Coordinates = new Vector2(x, y),
                                           Data = new List<float>() };
    }
    return closestPoint;
  }
}
