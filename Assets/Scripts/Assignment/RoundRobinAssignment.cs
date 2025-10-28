using System.Collections.Generic;
using UnityEngine;

// The round-robin assignment assigns hierarchical objects to each other in a round-robin order.
public class RoundRobinAssignment : AssignmentBase {
  // Run the assignment algorithm and assign the hierarchical objects.
  public override List<AssignmentItem> Assign(IReadOnlyList<IHierarchical> first,
                                              IReadOnlyList<IHierarchical> second) {
    if (first.Count == 0 || second.Count == 0) {
      return new List<AssignmentItem>();
    }

    int secondIndex = 0;
    var assignments = new List<AssignmentItem>();
    foreach (var firstHierarchical in first) {
      var secondHierarchical = second[secondIndex];
      assignments.Add(
          new AssignmentItem { First = firstHierarchical, Second = secondHierarchical });
      secondIndex = (secondIndex + 1) % second.Count;
    }
    return assignments;
  }
}
