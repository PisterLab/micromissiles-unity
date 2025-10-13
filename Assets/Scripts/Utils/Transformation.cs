using UnityEngine;

public struct Transformation {
  public PositionTransformation Position;
  public VelocityTransformation Velocity;
  public AccelerationTransformation Acceleration;
}

public struct PositionTransformation {
  public Vector3 Cartesian;
  public float Range;
  public float Azimuth;
  public float Elevation;
}

public struct VelocityTransformation {
  public Vector3 Cartesian;
  public float Range;
  public float Azimuth;
  public float Elevation;
}

public struct AccelerationTransformation {
  public Vector3 Cartesian;
}
