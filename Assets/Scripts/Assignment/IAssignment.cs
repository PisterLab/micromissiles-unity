using System.Collections.Generic;

// Interface for an assignment algorithm.
//
// The assignment algorithm assigns hierarchical objects to each other in a bipartite manner.
public interface IAssignment {
  IReadOnlyList<IHierarchical> First { get; }
  IReadOnlyList<IHierarchical> Second { get; }
  IReadOnlyList<AssignmentItem> Assignments { get; }

  // Run the assignment algorithm and assign the hierarchical objects.
  void Assign();
}
