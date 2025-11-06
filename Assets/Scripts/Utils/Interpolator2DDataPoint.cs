using System.Collections.Generic;
using UnityEngine;

// 2D interpolator data point.
public class Interpolator2DDataPoint {
  // 2D coordinates.
  public Vector2 Coordinates { get; set; }

  // Arbitrary data consisting of floats.
  public IReadOnlyList<float> Data { get; set; }

  // Validate and parse the data from strings to floats.
  public static (bool, List<float>) ValidateAndParseData(string[] values) {
    var parsedValues = new List<float>();
    for (int i = 0; i < values.Length; ++i) {
      if (!float.TryParse(values[i], out float parsedValue)) {
        return (false, new List<float>());
      }
      parsedValues.Add(parsedValue);
    }
    return (true, parsedValues);
  }
}
