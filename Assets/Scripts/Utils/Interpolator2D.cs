using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 2D interpolator data point.
public class Interpolator2DDataPoint {
  // 2D coordinates.
  public Vector2 Coordinates { get; }

  // Arbitrary data consisting of floats.
  public List<float> Data { get; }

  public Interpolator2DDataPoint() {}
  public Interpolator2DDataPoint(in Vector2 coordinates, in List<float> data) {
    Coordinates = coordinates;
    Data = data;
  }

  // Validate and parse the data from strings to floats.
  public static (bool, List<float>) ValidateAndParseData(in string[] values) {
    List<float> parsedValues = new List<float>();
    for (int i = 0; i < values.Length; ++i) {
      if (!float.TryParse(values[i], out float parsedValue)) {
        return (false, new List<float>());
      }
      parsedValues.Add(parsedValue);
    }
    return (true, parsedValues);
  }
}

// The 2D interpolator class interpolates values on a 2D grid.
// The first two columns specify the coordinates of each data point that are used for interpolating
// the remaining data values.
public abstract class IInterpolator2D {
  // 2D interpolator data points.
  protected readonly List<Interpolator2DDataPoint> _data = new List<Interpolator2DDataPoint>();

  public IInterpolator2D(in string[] csvLines) {
    foreach (string line in csvLines) {
      string[] values = line.Split(',');
      (bool success, List<float> parsedValues) =
          Interpolator2DDataPoint.ValidateAndParseData(values);
      if (success && parsedValues.Count >= 2) {
        _data.Add(new Interpolator2DDataPoint(new Vector2(parsedValues[0], parsedValues[1]),
                                              parsedValues.Skip(2).ToList()));
      }
    }
  }
  public IInterpolator2D(List<Interpolator2DDataPoint> data) {
    _data = data;
  }

  // Interpolate the value.
  public abstract Interpolator2DDataPoint Interpolate(float x, float y);
  public Interpolator2DDataPoint Interpolate(in Vector2 coordinates) {
    return Interpolate(coordinates.x, coordinates.y);
  }
}

// The 2D nearest neighbor interpolator class interpolates values on a 2D grid using nearest
// neighbor interpolation.
public class NearestNeighborInterpolator2D : IInterpolator2D {
  // K-D tree for nearest neighbor interpolation.
  private KDTree<Interpolator2DDataPoint> _tree;

  public NearestNeighborInterpolator2D(in string[] csvLines) : base(csvLines) {
    _tree = new KDTree<Interpolator2DDataPoint>(
        _data, (Interpolator2DDataPoint point) => point.Coordinates);
  }
  public NearestNeighborInterpolator2D(List<Interpolator2DDataPoint> data) : base(data) {
    _tree = new KDTree<Interpolator2DDataPoint>(
        _data, (Interpolator2DDataPoint point) => point.Coordinates);
  }

  // Interpolate the value using nearest neighbor interpolation.
  public override Interpolator2DDataPoint Interpolate(float x, float y) {
    Interpolator2DDataPoint closestPoint = _tree.NearestNeighbor(new Vector2(x, y));
    if (closestPoint == null) {
      Debug.LogError("No data points available for interpolation.");
      return new Interpolator2DDataPoint(new Vector2(x, y), new List<float>());
    }
    return closestPoint;
  }
}
