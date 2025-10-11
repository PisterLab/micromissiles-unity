using System;

public static class Constants {
  // Constants (these should be defined with appropriate values)
  public const double kAirDensity = 1.204;            // Sea level air density in kg/m^3
  public const double kAirDensityScaleHeight = 10.4;  // Scale height in km
  public const double kGravity = 9.80665;             // Standard gravity in m/s^2
  public const double kEarthMeanRadius = 6378137;     // Earth's mean radius in meters

  public static double CalculateAirDensityAtAltitude(double altitude) {
    return kAirDensity * Math.Exp(-altitude / (kAirDensityScaleHeight * 1000));
  }

  public static double CalculateGravityAtAltitude(double altitude) {
    return kGravity * Math.Pow(kEarthMeanRadius / (kEarthMeanRadius + altitude), 2);
  }
}
