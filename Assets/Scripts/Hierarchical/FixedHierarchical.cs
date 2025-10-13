using UnityEngine;

// The fixed hierarchical object represents a hierarchical object with a fixed position, velocity,
// and acceleration and should primarily be used for testing.
public class FixedHierarchical : HierarchicalBase {
  private Vector3 _position;
  private Vector3 _velocity;
  private Vector3 _acceleration;

  public FixedHierarchical(in Vector3 position, in Vector3 velocity)
      : this(position, velocity, acceleration: Vector3.zero) {}
  public FixedHierarchical(in Vector3 position, in Vector3 velocity, in Vector3 acceleration) {
    _position = position;
    _velocity = velocity;
    _acceleration = acceleration;
  }

  protected override Vector3 GetPosition() {
    return _position;
  }
  protected override Vector3 GetVelocity() {
    return _velocity;
  }
  protected override Vector3 GetAcceleration() {
    return _acceleration;
  }
}
