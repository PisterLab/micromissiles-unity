using UnityEngine;

public struct Transformation {
  public PositionTransformation Position { get; set; }
  public VelocityTransformation Velocity { get; set; }
  public AccelerationTransformation Acceleration { get; set; }
}

public struct PositionTransformation {
  public Vector3 Cartesian { get; set; }
  public float Range { get; set; }
  public float Azimuth { get; set; }
  public float Elevation { get; set; }
}

public struct VelocityTransformation {
  public Vector3 Cartesian { get; set; }
  public float Range { get; set; }
  public float Azimuth { get; set; }
  public float Elevation { get; set; }
}

public struct AccelerationTransformation {
  public Vector3 Cartesian { get; set; }
}
