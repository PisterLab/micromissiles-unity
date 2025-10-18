using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class MaxSpeedAssignmentTests : TestBase {
  private MaxSpeedAssignment _assignment =
      new MaxSpeedAssignment(assignFunction: Assignment.Assignment_EvenAssignment_Assign);

  private HierarchicalAgent GenerateAgent(in Vector3 position, in Vector3 velocity) {
    var agent = new GameObject("Agent").AddComponent<AgentBase>();
    agent.StaticConfig = new Configs.StaticConfig() {
      AccelerationConfig =
          new Configs.AccelerationConfig() {
            MaxForwardAcceleration = 10,
            MaxReferenceNormalAcceleration = 5 / Constants.kGravity,
            ReferenceSpeed = 0.5f,
          },
      LiftDragConfig =
          new Configs.LiftDragConfig() {
            DragCoefficient = 0.7f,
            LiftDragRatio = 5,
          },
      BodyConfig =
          new Configs.BodyConfig() {
            CrossSectionalArea = 1,
            Mass = 1,
          },
    };
    Rigidbody agentRb = agent.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(agent, "Awake");
    agent.Position = position;
    agent.Velocity = velocity;
    return new HierarchicalAgent(agent);
  }

  [Test]
  public void Assign_NoFirst_ReturnsEmptyList() {
    var first = new List<HierarchicalAgent>();
    var second = new List<FixedHierarchical> { new FixedHierarchical() };
    var assignments = _assignment.Assign(first, second);
    Assert.AreEqual(0, assignments.Count);
  }

  [Test]
  public void Assign_NoSecond_ReturnsEmptyList() {
    var first = new List<HierarchicalAgent> { GenerateAgent(position: Vector3.zero,
                                                            velocity: Vector3.zero) };
    var second = new List<FixedHierarchical>();
    var assignments = _assignment.Assign(first, second);
    Assert.AreEqual(0, assignments.Count);
  }

  [Test]
  public void Assign_ShouldAssignEachFirst() {
    const int numFirst = 20;
    var first = new List<HierarchicalAgent>();
    for (int i = 0; i < numFirst; ++i) {
      first.Add(GenerateAgent(position: Vector3.zero, velocity: new Vector3(0, 0, 1)));
    }
    var second = new List<FixedHierarchical>() { new FixedHierarchical() };
    var assignments = _assignment.Assign(first, second);
    Assert.AreEqual(numFirst, assignments.Count);
    var assignedFirsts = new HashSet<IHierarchical>();
    foreach (var assignment in assignments) {
      Assert.Contains(assignment.First, first);
      Assert.IsTrue(assignedFirsts.Add(assignment.First));
    }
  }

  [Test]
  public void Assign_ShouldMaximizeSpeed() {
    var first = new List<HierarchicalAgent> {
      GenerateAgent(position: new Vector3(10, 0, 0), velocity: new Vector3(1, 0, 0)),
      GenerateAgent(position: new Vector3(10, 0, 0), velocity: new Vector3(-1, 0, 0)),
      GenerateAgent(position: new Vector3(0, 110, 0), velocity: new Vector3(1, -1, 0)),
    };
    var second = new List<FixedHierarchical> {
      new FixedHierarchical(position: new Vector3(0, 0, 0)),
      new FixedHierarchical(position: new Vector3(20, 0, 0)),
      new FixedHierarchical(position: new Vector3(0, 100, 0)),
    };
    var assignments = _assignment.Assign(first, second);
    Assert.AreEqual(first.Count, assignments.Count);
    var expectedAssignments = new List<AssignmentItem> {
      new AssignmentItem { First = first[0], Second = second[1] },
      new AssignmentItem { First = first[1], Second = second[0] },
      new AssignmentItem { First = first[2], Second = second[2] },
    };
    var assignmentItems = new HashSet<AssignmentItem>();
    foreach (var assignment in assignments) {
      Assert.Contains(assignment, expectedAssignments);
      Assert.IsTrue(assignmentItems.Add(assignment));
    }
  }
}
