using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Base implementation of a 2D interpolator.
public abstract class Interpolator2DBase : IInterpolator2D {
  // 2D interpolator data points.
  protected readonly List<Interpolator2DDataPoint> _data;

  public IReadOnlyList<Interpolator2DDataPoint> Data => _data;

  public Interpolator2DBase(IEnumerable<Interpolator2DDataPoint> data) {
    _data = data.ToList() ?? new List<Interpolator2DDataPoint>();
  }
  public Interpolator2DBase(string[] csvLines) : this(ParseCsvLines(csvLines)) {}

  // Interpolate the value.
  public abstract Interpolator2DDataPoint Interpolate(float x, float y);
  public Interpolator2DDataPoint Interpolate(in Vector2 coordinates) {
    return Interpolate(coordinates.x, coordinates.y);
  }

  // Parse the CSV lines into 2D data points.
  private static IEnumerable<Interpolator2DDataPoint> ParseCsvLines(IEnumerable<string> lines) {
    var parsedDataPoints = new List<Interpolator2DDataPoint>();
    foreach (string line in lines) {
      if (string.IsNullOrWhiteSpace(line)) {
        continue;
      }
      var values = line.Split(',');
      var (success, parsedValues) = Interpolator2DDataPoint.ValidateAndParseData(values);
      if (success && parsedValues.Count >= 2) {
        parsedDataPoints.Add(new Interpolator2DDataPoint {
          Coordinates = new Vector2(parsedValues[0], parsedValues[1]),
          Data = parsedValues.Skip(2).ToList(),
        });
      }
    }
    return parsedDataPoints;
  }
}
