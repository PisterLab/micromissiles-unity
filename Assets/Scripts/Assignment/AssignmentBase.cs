using System.Collections.Generic;

// Base implementation of an assignment algorithm.
public abstract class AssignmentBase : IAssignment {
  // Run the assignment algorithm and assign the hierarchical objects.
  public abstract IEnumerable<AssignmentItem> Assign(IEnumerable<IHierarchical> first,
                                                     IEnumerable<IHierarchical> second);
}
