using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class HierarchicalBaseTests {
  private const float Epsilon = 1e-3f;

  private class FixedHierarchical : HierarchicalBase {
    private Vector3 _position;
    private Vector3 _velocity;

    public FixedHierarchical(in Vector3 position, in Vector3 velocity) {
      _position = position;
      _velocity = velocity;
    }

    protected override Vector3 GetPosition() {
      return _position;
    }
    protected override Vector3 GetVelocity() {
      return _velocity;
    }
  }

  [Test]
  public void Target_SetAndGet_WorksCorrectly() {
    var parent = new HierarchicalBase();
    var target = new HierarchicalBase();
    parent.Target = target;
    Assert.AreSame(target, parent.Target);
  }

  [Test]
  public void TargetModel_SetAndGet_WorksCorrectly() {
    var parent = new HierarchicalBase();
    var model = new HierarchicalBase();
    parent.TargetModel = model;
    Assert.AreSame(model, parent.TargetModel);
  }

  [Test]
  public void Position_ReturnsMeanOfSubHierarchicals() {
    var parent = new HierarchicalBase();
    var child1 = new FixedHierarchical(new Vector3(2, 0, 0), new Vector3(2, 1, 1));
    parent.AddSubHierarchical(child1);
    var child2 = new FixedHierarchical(new Vector3(-1, 2, 3), new Vector3(1, -2, 0));
    parent.AddSubHierarchical(child2);

    Assert.AreEqual(new Vector3(0.5f, 1f, 1.5f), parent.Position);
  }

  [Test]
  public void Position_WithNoSubHierarchicals_IsZero() {
    var parent = new HierarchicalBase();
    Assert.AreEqual(Vector3.zero, parent.Position);
  }

  [Test]
  public void Velocity_ReturnsMeanOfSubHierarchicals() {
    var parent = new HierarchicalBase();
    var child1 = new FixedHierarchical(new Vector3(2, 0, 0), new Vector3(2, 1, 1));
    parent.AddSubHierarchical(child1);
    var child2 = new FixedHierarchical(new Vector3(-1, 2, 3), new Vector3(1, -2, 0));
    parent.AddSubHierarchical(child2);

    Assert.AreEqual(new Vector3(1.5f, -0.5f, 0.5f), parent.Velocity);
  }

  [Test]
  public void Velocity_WithNoSubHierarchicals_IsZero() {
    var parent = new HierarchicalBase();
    Assert.AreEqual(Vector3.zero, parent.Velocity);
  }

  [Test]
  public void Speed_ReturnsMagnitudeOfVelocity() {
    var parent = new HierarchicalBase();
    var child = new FixedHierarchical(new Vector3(2, 0, 0), new Vector3(0, -4, 3));
    parent.AddSubHierarchical(child);

    Assert.AreEqual(5f, parent.Speed, Epsilon);
  }

  [Test]
  public void AddSubHierarchical_AddsCorrectly() {
    var parent = new HierarchicalBase();
    var child = new HierarchicalBase();
    parent.AddSubHierarchical(child);

    Assert.AreEqual(1, parent.SubHierarchicals.Count);
    Assert.AreSame(child, parent.SubHierarchicals[0]);
  }

  [Test]
  public void RemoveSubHierarchical_RemovesCorrectly() {
    var parent = new HierarchicalBase();
    var child1 = new HierarchicalBase();
    var child2 = new HierarchicalBase();

    parent.AddSubHierarchical(child1);
    parent.RemoveSubHierarchical(child1);
    parent.RemoveSubHierarchical(child2);

    Assert.AreEqual(0, parent.SubHierarchicals.Count);
  }
}
