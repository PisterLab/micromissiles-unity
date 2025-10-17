using System.Collections.Generic;

// Base implementation of an assignment algorithm.
public abstract class AssignmentBase : IAssignment {
  // Run the assignment algorithm and assign the hierarchical objects.
  public abstract List<AssignmentItem> Assign(IReadOnlyList<IHierarchical> first,
                                              IReadOnlyList<IHierarchical> second);
}
