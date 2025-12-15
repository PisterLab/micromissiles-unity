using NUnit.Framework;
using UnityEngine;

public class FixedHierarchicalTests {
  private FixedHierarchical _fixedHierarchical;
  private IHierarchical _hierarchical;

  [SetUp]
  public void SetUp() {
    _fixedHierarchical =
        new FixedHierarchical(position: new Vector3(0, 1, 2), velocity: new Vector3(0, -4, 3),
                              acceleration: new Vector3(-1, -2, 0));
    _hierarchical = _fixedHierarchical;
  }

  [Test]
  public void Position_IsPolymorphic() {
    Assert.AreEqual(new Vector3(0, 1, 2), _fixedHierarchical.Position);
    Assert.AreEqual(new Vector3(0, 1, 2), _hierarchical.Position);
  }

  [Test]
  public void Velocity_IsPolymorphic() {
    Assert.AreEqual(new Vector3(0, -4, 3), _fixedHierarchical.Velocity);
    Assert.AreEqual(new Vector3(0, -4, 3), _hierarchical.Velocity);
  }

  [Test]
  public void Speed_IsPolymorphic() {
    Assert.AreEqual(5f, _fixedHierarchical.Speed);
    Assert.AreEqual(5f, _hierarchical.Speed);
  }

  [Test]
  public void Acceleration_IsPolymorphic() {
    Assert.AreEqual(new Vector3(-1, -2, 0), _fixedHierarchical.Acceleration);
    Assert.AreEqual(new Vector3(-1, -2, 0), _hierarchical.Acceleration);
  }
}
