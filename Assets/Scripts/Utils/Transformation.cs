using UnityEngine;

public struct Transformation {
  public PositionTransformation position;
  public VelocityTransformation velocity;
  public AccelerationTransformation acceleration;
}

public struct PositionTransformation {
  public Vector3 cartesian;
  public float range;
  public float azimuth;
  public float elevation;
}

public struct VelocityTransformation {
  public Vector3 cartesian;
  public float range;
  public float azimuth;
  public float elevation;
}

public struct AccelerationTransformation {
  public Vector3 cartesian;
}
