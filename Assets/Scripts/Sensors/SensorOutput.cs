// The sensor output defines the output of a sensor. The transformations are relative to the sensing
// agent's state.
public struct SensorOutput {
  // Relative position transformation.
  public PositionTransformation Position { get; set; }

  // Relative velocity transformation.
  public VelocityTransformation Velocity { get; set; }
}
