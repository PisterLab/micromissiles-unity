using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HierarchicalBaseTests {
  private const float _epsilon = 1e-3f;

  [Test]
  public void Target_SetAndGet_WorksCorrectly() {
    var parent = new HierarchicalBase();
    var target = new HierarchicalBase();
    parent.Target = target;
    Assert.AreSame(target, parent.Target);
  }

  [Test]
  public void Position_ReturnsMeanOfSubHierarchicals() {
    var parent = new HierarchicalBase();
    var child1 =
        new FixedHierarchical(position: new Vector3(2, 0, 0), velocity: new Vector3(2, 1, 1));
    parent.AddSubHierarchical(child1);
    var child2 =
        new FixedHierarchical(position: new Vector3(-1, 2, 3), velocity: new Vector3(1, -2, 0));
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
    var child1 =
        new FixedHierarchical(position: new Vector3(2, 0, 0), velocity: new Vector3(2, 1, 1));
    parent.AddSubHierarchical(child1);
    var child2 =
        new FixedHierarchical(position: new Vector3(-1, 2, 3), velocity: new Vector3(1, -2, 0));
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
    var child =
        new FixedHierarchical(position: new Vector3(2, 0, 0), velocity: new Vector3(0, -4, 3));
    parent.AddSubHierarchical(child);

    Assert.AreEqual(5f, parent.Speed, _epsilon);
  }

  [Test]
  public void Acceleration_ReturnsMeanOfSubHierarchicals() {
    var parent = new HierarchicalBase();
    var child1 =
        new FixedHierarchical(position: new Vector3(2, 0, 0), velocity: new Vector3(2, 1, 1),
                              acceleration: new Vector3(0, -1, 3));
    parent.AddSubHierarchical(child1);
    var child2 =
        new FixedHierarchical(position: new Vector3(-1, 2, 3), velocity: new Vector3(1, -2, 0),
                              acceleration: new Vector3(0, 2, -2));
    parent.AddSubHierarchical(child2);

    Assert.AreEqual(new Vector3(0f, 0.5f, 0.5f), parent.Acceleration);
  }

  [Test]
  public void Acceleration_WithNoSubHierarchicals_IsZero() {
    var parent = new HierarchicalBase();
    Assert.AreEqual(Vector3.zero, parent.Acceleration);
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
  public void AddSubHierarchical_DoesNotAddDuplicates() {
    var parent = new HierarchicalBase();
    var child = new HierarchicalBase();
    parent.AddSubHierarchical(child);
    parent.AddSubHierarchical(child);

    Assert.AreEqual(1, parent.SubHierarchicals.Count);
    Assert.AreSame(child, parent.SubHierarchicals[0]);
  }

  [Test]
  public void RemoveSubHierarchical_RemovesCorrectly() {
    var parent = new HierarchicalBase();
    var child = new HierarchicalBase();

    parent.AddSubHierarchical(child);
    parent.RemoveSubHierarchical(child);

    Assert.AreEqual(0, parent.SubHierarchicals.Count);
  }

  [Test]
  public void RemoveSubHierarchical_DoesNotRemoveNonExisting() {
    var parent = new HierarchicalBase();
    var child1 = new HierarchicalBase();
    var child2 = new HierarchicalBase();

    parent.AddSubHierarchical(child1);
    parent.RemoveSubHierarchical(child2);

    Assert.AreEqual(1, parent.SubHierarchicals.Count);
    Assert.AreSame(child1, parent.SubHierarchicals[0]);
  }

  [Test]
  public void ClearSubHierarchicals_RemovesAllSubHierarchicals() {
    var parent = new HierarchicalBase();
    var child1 = new HierarchicalBase();
    var child2 = new HierarchicalBase();
    parent.AddSubHierarchical(child1);
    parent.AddSubHierarchical(child2);

    Assert.AreEqual(2, parent.SubHierarchicals.Count);
    parent.ClearSubHierarchicals();
    Assert.AreEqual(0, parent.SubHierarchicals.Count);
  }

  [Test]
  public void AddPursuer_AddsCorrectly() {
    var parent = new HierarchicalBase();
    var child = new HierarchicalBase();
    parent.AddPursuer(child);

    Assert.AreEqual(1, parent.Pursuers.Count);
    Assert.AreSame(child, parent.Pursuers[0]);
  }

  [Test]
  public void AddPursuer_DoesNotAddDuplicates() {
    var parent = new HierarchicalBase();
    var child = new HierarchicalBase();
    parent.AddPursuer(child);
    parent.AddPursuer(child);

    Assert.AreEqual(1, parent.Pursuers.Count);
    Assert.AreSame(child, parent.Pursuers[0]);
  }

  [Test]
  public void RemovePursuer_RemovesCorrectly() {
    var parent = new HierarchicalBase();
    var child = new HierarchicalBase();

    parent.AddPursuer(child);
    parent.RemovePursuer(child);

    Assert.AreEqual(0, parent.Pursuers.Count);
  }

  [Test]
  public void RemovePursuer_DoesNotRemoveNonExisting() {
    var parent = new HierarchicalBase();
    var child1 = new HierarchicalBase();
    var child2 = new HierarchicalBase();

    parent.AddPursuer(child1);
    parent.RemovePursuer(child2);

    Assert.AreEqual(1, parent.Pursuers.Count);
    Assert.AreSame(child1, parent.Pursuers[0]);
  }

  [Test]
  public void AddLaunchedHierarchical_AddsCorrectly() {
    var parent = new HierarchicalBase();
    var child = new HierarchicalBase();
    parent.AddLaunchedHierarchical(child);

    Assert.AreEqual(1, parent.LaunchedHierarchicals.Count);
    Assert.AreSame(child, parent.LaunchedHierarchicals[0]);
  }

  [Test]
  public void AddLaunchedHierarchical_DoesNotAddDuplicates() {
    var parent = new HierarchicalBase();
    var child = new HierarchicalBase();
    parent.AddLaunchedHierarchical(child);
    parent.AddLaunchedHierarchical(child);

    Assert.AreEqual(1, parent.LaunchedHierarchicals.Count);
    Assert.AreSame(child, parent.LaunchedHierarchicals[0]);
  }
}
