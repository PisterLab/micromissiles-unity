using UnityEngine;

// The fixed hierarchical object represents a hierarchical object with a fixed position, velocity,
// and acceleration and should primarily be used for testing.
public class FixedHierarchical : HierarchicalBase, IHierarchical {
  // Shadowing is necessary because the parent's position, velocity, and acceleration are not
  // settable. The speed must be shadowed to prevent using the parent's velocity property.
  public new Vector3 Position { get; set; }
  public new Vector3 Velocity { get; set; }
  public new float Speed => Velocity.magnitude;
  public new Vector3 Acceleration { get; set; }
  public override bool IsTerminated => false;

  public FixedHierarchical() : this(position: Vector3.zero) {}
  public FixedHierarchical(in Vector3 position)
      : this(position: position, velocity: Vector3.zero) {}
  public FixedHierarchical(in Vector3 position, in Vector3 velocity)
      : this(position: position, velocity: velocity, acceleration: Vector3.zero) {}
  public FixedHierarchical(in Vector3 position, in Vector3 velocity, in Vector3 acceleration) {
    Position = position;
    Velocity = velocity;
    Acceleration = acceleration;
  }
}
