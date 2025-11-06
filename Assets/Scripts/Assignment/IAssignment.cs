using System.Collections.Generic;

// Interface for an assignment algorithm.
//
// The assignment algorithm assigns hierarchical objects to each other in a bipartite manner.
public interface IAssignment {
  // Run the assignment algorithm and assign the hierarchical objects.
  IEnumerable<AssignmentItem> Assign(IEnumerable<IHierarchical> first,
                                     IEnumerable<IHierarchical> second);
}
