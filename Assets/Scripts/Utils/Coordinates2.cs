using UnityEngine;

// Utility functions for 2D coordinates.
// In Cartesian coordinates, the x-axis points right, and the y-axis points up. The coordinates are
// given by (x, y).
// In polar coordinates, the angle is measured in degrees from the x-axis counterclockwise to the
// y-axis. The coordinates are given by (r, theta).
public static class Coordinates2 {
  public static Vector2 ConvertCartesianToPolar(in Vector2 cartesian) {
    float r = cartesian.magnitude;
    float theta = Mathf.Atan2(cartesian.y, cartesian.x) * Mathf.Rad2Deg;
    return new Vector2(r, theta);
  }
  public static Vector2 ConvertCartesianToPolar(float x, float y) {
    return ConvertCartesianToPolar(new Vector2(x, y));
  }

  public static Vector2 ConvertPolarToCartesian(in Vector2 polar) {
    float x = polar.x * Mathf.Cos(polar.y * Mathf.Deg2Rad);
    float y = polar.x * Mathf.Sin(polar.y * Mathf.Deg2Rad);
    return new Vector2(x, y);
  }
  public static Vector2 ConvertPolarToCartesian(float r, float theta) {
    return ConvertPolarToCartesian(new Vector2(r, theta));
  }
}
