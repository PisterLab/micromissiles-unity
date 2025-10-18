using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class MinDistanceAssignmentTests {
  private MinDistanceAssignment _assignment =
      new MinDistanceAssignment(assignFunction: Assignment.Assignment_EvenAssignment_Assign);

  private static List<FixedHierarchical> GenerateHierarchicals(int numHierarchicals) {
    var hierarchicals = new List<FixedHierarchical>();
    for (int i = 0; i < numHierarchicals; ++i) {
      hierarchicals.Add(new FixedHierarchical());
    }
    return hierarchicals;
  }

  [Test]
  public void Assign_NoFirst_ReturnsEmptyList() {
    var first = new List<FixedHierarchical>();
    var second = new List<FixedHierarchical> { new FixedHierarchical() };
    var assignments = _assignment.Assign(first, second);
    Assert.AreEqual(0, assignments.Count);
  }

  [Test]
  public void Assign_NoSecond_ReturnsEmptyList() {
    var first = new List<FixedHierarchical> { new FixedHierarchical() };
    var second = new List<FixedHierarchical>();
    var assignments = _assignment.Assign(first, second);
    Assert.AreEqual(0, assignments.Count);
  }

  [Test]
  public void Assign_ShouldAssignEachFirst() {
    const int numFirst = 20;
    var first = GenerateHierarchicals(numFirst);
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
  public void Assign_ShouldMinimizeDistanceBetweenAssignedObjects() {
    var first = new List<FixedHierarchical> {
      new FixedHierarchical(position: new Vector3(5, 0, 5)),
      new FixedHierarchical(position: new Vector3(-5, 90, 0)),
      new FixedHierarchical(position: new Vector3(0, 110, 10)),
      new FixedHierarchical(position: new Vector3(20, 10, 0)),
    };
    var second = new List<FixedHierarchical> {
      new FixedHierarchical(position: new Vector3(0, 0, 0)),
      new FixedHierarchical(position: new Vector3(20, 0, 0)),
      new FixedHierarchical(position: new Vector3(0, 100, 0)),
    };
    var assignments = _assignment.Assign(first, second);
    Assert.AreEqual(first.Count, assignments.Count);
    var expectedAssignments = new List<AssignmentItem> {
      new AssignmentItem { First = first[0], Second = second[0] },
      new AssignmentItem { First = first[1], Second = second[2] },
      new AssignmentItem { First = first[2], Second = second[2] },
      new AssignmentItem { First = first[3], Second = second[1] },
    };
    var assignmentItems = new HashSet<AssignmentItem>();
    foreach (var assignment in assignments) {
      Assert.Contains(assignment, expectedAssignments);
      Assert.IsTrue(assignmentItems.Add(assignment));
    }
  }
}
