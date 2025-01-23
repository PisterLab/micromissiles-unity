using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// The 2D interpolator class interpolates values on a 2D grid.
// The first two columns specify the coordinates of each data point that are used for interpolating
// the remaining data values.
public class Interpolator2D {
  // 2D data point.
  private sealed class DataPoint {
    // 2D coordinates.
    public Vector2 Coordinates;

    // Arbitrary data consisting of floats.
    public List<float> Data;

    public DataPoint(List<float> data) {
      Coordinates = new Vector2(data[0], data[1]);
      Data = data.TakeLast(data.Count - 2).ToList();
    }

    // Validate and parse the data from strings to floats.
    public static (bool, List<float>) ValidateAndParseData(string[] values) {
      if (values.Length < 2) {
        return (false, new List<float>());
      }
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

  // 2D data points.
  private readonly List<DataPoint> _data = new List<DataPoint>();

  public Interpolator2D(string[] csvLines) {
    foreach (string line in csvLines) {
      string[] values = line.Split(',');
      (bool success, List<float> parsedValues) = DataPoint.ValidateAndParseData(values);
      if (success) {
        _data.Add(new DataPoint(parsedValues));
      }
    }
  }

  // Interpolate the value using nearest neighbor interpolation.
  public List<float> Interpolate(float x, float y) {
    DataPoint closestPoint =
        _data.OrderBy(point => Vector2.Distance(new Vector2(x, y), point.Coordinates))
            .FirstOrDefault();
    if (closestPoint == null) {
      Debug.LogError("No data points available for interpolation.");
      return new List<float>();
    }
    return closestPoint.Data;
  }
  public List<float> Interpolate(Vector2 coordinates) {
    return Interpolate(coordinates.x, coordinates.y);
  }
}
