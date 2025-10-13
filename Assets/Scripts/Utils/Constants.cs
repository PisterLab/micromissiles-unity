using System;

public static class Constants {
  // Constants (these should be defined with appropriate values).
  public const float kAirDensity = 1.204f;            // Sea level air density in kg/m^3.
  public const float kAirDensityScaleHeight = 10.4f;  // Scale height in km.
  public const float kGravity = 9.80665f;             // Standard gravity in m/s^2.
  public const float kEarthMeanRadius = 6378137f;     // Earth's mean radius in meters.

  public static float CalculateAirDensityAtAltitude(float altitude) {
    return kAirDensity * MathF.Exp(-altitude / (kAirDensityScaleHeight * 1000));
  }

  public static float CalculateGravityAtAltitude(float altitude) {
    return kGravity * MathF.Pow(kEarthMeanRadius / (kEarthMeanRadius + altitude), 2);
  }
}
