using System.Collections.Generic;

// Interface for an assignment algorithm.
//
// The assignment algorithm assigns hierarchical objects to each other in a bipartite manner.
// All hierarchical objects on the left side, i.e., the first hierarchical objects, will each
// receive exactly one assignment to a hierarchical object on the right side, i.e., a second
// hierarchical object. It is possible that a second hierarchical object receives multiple
// assignments or no assignment.
public interface IAssignment {
  // Run the assignment algorithm and assign the hierarchical objects.
  List<AssignmentItem> Assign(IReadOnlyList<IHierarchical> first,
                              IReadOnlyList<IHierarchical> second);
}
