using UnityEngine;

public static class Utilities {
  public static Vector3 GenerateRandomNoise(Vector3 standardDeviation) {
    return new Vector3(SampleStandardNormal() * standardDeviation.x,
                       SampleStandardNormal() * standardDeviation.y,
                       SampleStandardNormal() * standardDeviation.z);
  }

  public static Vector3 GenerateRandomNoise(Simulation.CartesianCoordinates standardDeviation) {
    return GenerateRandomNoise(Coordinates3.FromProto(standardDeviation));
  }

  public static float SampleStandardNormal() {
    // Use the Box-Muller transform to sample from a standard normal distribution with mean = 0 and
    // standard deviation = 1.
    float u1 = 0f;
    // Avoid taking the logarithm of 0.
    while (u1 == 0f) {
      u1 = Random.value;
    }
    float u2 = Random.value;
    return Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
  }

  public static float ConvertMpsToKnots(float mps) {
    return mps * 1.94384f;
  }

  public static float ConvertMetersToFeet(float meters) {
    return meters * 3.28084f;
  }
}
