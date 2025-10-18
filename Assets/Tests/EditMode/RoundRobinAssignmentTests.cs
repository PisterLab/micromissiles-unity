using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class RoundRobinAssignmentTests {
  private RoundRobinAssignment _assignment = new RoundRobinAssignment();

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
  public void Assign_ShouldRoundRobinAmongSecond() {
    const int numFirst = 50;
    const int numSecond = 13;
    var first = GenerateHierarchicals(numFirst);
    var second = GenerateHierarchicals(numSecond);
    var assignments = _assignment.Assign(first, second);
    Assert.AreEqual(numFirst, assignments.Count);
    int secondIndex = 0;
    for (int i = 0; i < numFirst; ++i) {
      Assert.AreEqual(second[secondIndex], assignments[i].Second);
      secondIndex = (secondIndex + 1) % numSecond;
    }
  }
}
