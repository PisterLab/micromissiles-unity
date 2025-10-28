using UnityEngine;

// Utility functions for 3D coordinates.
// In Cartesian coordinates, the x-axis points right, the y-axis points up, and the z-axis points
// forward. The coordinates are given by (x, y, z).
// In spherical coordinates, the azimuth is measured in degrees from the z-axis clockwise to the
// x-axis, and elevation is measured in degrees from the x-z plane up to the y-axis. The coordinates
// are given by (r, azimuth, elevation).
// In cylindrical coordinates, the azimuth is measured in degrees from the z-axis clockwise to the
// x-axis. The coordinates are given by (r, azimuth, height).
public static class Coordinates3 {
  public static Vector3 ConvertCartesianToSpherical(in Vector3 cartesian) {
    float r = cartesian.magnitude;
    float azimuth = Mathf.Atan2(cartesian.x, cartesian.z) * Mathf.Rad2Deg;
    float elevation = Mathf.Atan2(cartesian.y, Mathf.Sqrt(cartesian.x * cartesian.x +
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

  public static Vector3 ConvertCartesianToCylindrical(in Vector3 cartesian) {
    float r = Mathf.Sqrt(cartesian.x * cartesian.x + cartesian.z * cartesian.z);
    float azimuth = Mathf.Atan2(cartesian.x, cartesian.z) * Mathf.Rad2Deg;
    float height = cartesian.y;
    return new Vector3(r, azimuth, height);
  }

  public static Vector3 ConvertCartesianToCylindrical(float x, float y, float z) {
    return ConvertCartesianToCylindrical(new Vector3(x, y, z));
  }

  public static Vector3 ConvertCylindricalToCartesian(in Vector3 cylindrical) {
    float y = cylindrical.z;
    float x = cylindrical.x * Mathf.Sin(cylindrical.y * Mathf.Deg2Rad);
    float z = cylindrical.x * Mathf.Cos(cylindrical.y * Mathf.Deg2Rad);
    return new Vector3(x, y, z);
  }

  public static Vector3 ConvertCylindricalToCartesian(float r, float azimuth, float height) {
    return ConvertCylindricalToCartesian(new Vector3(r, azimuth, height));
  }

  public static Vector3 FromProto(in Simulation.CartesianCoordinates coordinates) {
    return new Vector3(coordinates.X, coordinates.Y, coordinates.Z);
  }

  public static Simulation.CartesianCoordinates ToProto(in Vector3 cartesian) {
    return new Simulation.CartesianCoordinates() {
      X = cartesian.x,
      Y = cartesian.y,
      Z = cartesian.z,
    };
  }
}
