using UnityEngine;

// Utility functions for 2D coordinates.
// In Cartesian coordinates, the x-axis points right, and the y-axis points up. The coordinates are
// given by (x, y).
// In polar coordinates, the angle is measured in degrees from the x-axis counterclockwise to the
// y-axis. The coordinates are given by (r, theta).
public class Coordinates2 {
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

// Utility functions for 3D coordinates.
// In Cartesian coordinates, the x-axis points right, the y-axis points up, and the z-axis points
// forward. The coordinates are given by (x, y, z).
// In spherical coordinates, the azimuth is measured in degrees from the x-axis clockwise to the
// z-axis, and elevation is measured in degrees from the x-z plane up to the y-axis. The coordinates
// are given by (r, azimuth, elevation).
public class Coordinates3 {
  public static Vector3 ConvertCartesianToSpherical(in Vector3 cartesian) {
    float r = cartesian.magnitude;
    float azimuth = Mathf.Atan2(cartesian.x, cartesian.z) * Mathf.Rad2Deg;
    float elevation = Mathf.Atan(cartesian.y / Mathf.Sqrt(cartesian.x * cartesian.x +
                                                          cartesian.z * cartesian.z)) *
                      Mathf.Rad2Deg;
    return new Vector3(r, azimuth, elevation);
  }
  public static Vector3 ConvertCartesianToSpherical(float x, float y, float z) {
    return ConvertCartesianToSpherical(new Vector3(x, y, z));
  }

  public static Vector3 ConvertSphericalToCartesian(in Vector3 spherical) {
    float y = spherical.x * Mathf.Sin(spherical.z * Mathf.Deg2Rad);
    float x = spherical.x * Mathf.Cos(spherical.z * Mathf.Deg2Rad) *
              Mathf.Sin(spherical.y * Mathf.Deg2Rad);
    float z = spherical.x * Mathf.Cos(spherical.z * Mathf.Deg2Rad) *
              Mathf.Cos(spherical.y * Mathf.Deg2Rad);
    return new Vector3(x, y, z);
  }
  public static Vector3 ConvertSphericalToCartesian(float r, float azimuth, float elevation) {
    return ConvertSphericalToCartesian(new Vector3(r, azimuth, elevation));
  }
}
