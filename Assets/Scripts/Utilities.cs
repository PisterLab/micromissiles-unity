using UnityEngine;

public static class Utilities {
  public static Vector3 GenerateRandomNoise(Vector3 standardDeviation) {
    return new Vector3(Random.Range(-standardDeviation.x, standardDeviation.x),
                       Random.Range(-standardDeviation.y, standardDeviation.y),
                       Random.Range(-standardDeviation.z, standardDeviation.z));
  }

  public static float ConvertMpsToKnots(float mps) {
    return mps * 1.94384f;
  }

  public static float ConvertMetersToFeet(float meters) {
    return meters * 3.28084f;
  }
}
