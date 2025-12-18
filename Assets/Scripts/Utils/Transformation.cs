using UnityEngine;

public struct Transformation {
  public PositionTransformation Position { get; init; }
  public VelocityTransformation Velocity { get; init; }
  public AccelerationTransformation Acceleration { get; init; }
}

public struct PositionTransformation {
  public Vector3 Cartesian { get; init; }
  public float Range { get; init; }
  public float Azimuth { get; init; }
  public float Elevation { get; init; }
}

public struct VelocityTransformation {
  public Vector3 Cartesian { get; init; }
  public float Range { get; init; }
  public float Azimuth { get; init; }
  public float Elevation { get; init; }
}

public struct AccelerationTransformation {
  public Vector3 Cartesian { get; init; }
}
