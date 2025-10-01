using UnityEngine;

public static class Utilities {
  public static Vector3 GenerateRandomNoise(Vector3 standardDeviation) {
    // Prefer the seeded System.Random when available for stronger determinism across runs.
    if (RunContext.SystemRandom != null) {
      return GenerateRandomNoise(standardDeviation, RunContext.SystemRandom);
    }
    return new Vector3(Random.Range(-standardDeviation.x, standardDeviation.x),
                       Random.Range(-standardDeviation.y, standardDeviation.y),
                       Random.Range(-standardDeviation.z, standardDeviation.z));
  }

  public static Vector3 GenerateRandomNoise(Vector3 standardDeviation, System.Random rng) {
    float rx = (float)((rng.NextDouble() * 2.0) - 1.0);
    float ry = (float)((rng.NextDouble() * 2.0) - 1.0);
    float rz = (float)((rng.NextDouble() * 2.0) - 1.0);
    return new Vector3(rx * standardDeviation.x, ry * standardDeviation.y,
                       rz * standardDeviation.z);
  }

  public static float ConvertMpsToKnots(float mps) {
    return mps * 1.94384f;
  }

  public static float ConvertMetersToFeet(float meters) {
    return meters * 3.28084f;
  }
}
