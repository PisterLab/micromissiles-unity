using System.Collections.Generic;
using System.Linq;

// Base implementation of an assignment algorithm.
public abstract class AssignmentBase : IAssignment {
  // List of hierarchical objects to assign.
  protected List<IHierarchical> _first = new List<IHierarchical>();

  // List of hierarchical objects to assign to.
  protected List<IHierarchical> _second = new List<IHierarchical>();

  // List of assignments.
  protected List<AssignmentItem> _assignments = new List<AssignmentItem>();

  public IReadOnlyList<IHierarchical> First => _first.AsReadOnly();
  public IReadOnlyList<IHierarchical> Second => _second.AsReadOnly();
  public IReadOnlyList<AssignmentItem> Assignments => _assignments.AsReadOnly();

  public AssignmentBase(IEnumerable<IHierarchical> first, IEnumerable<IHierarchical> second) {
    _first = first.ToList();
    _second = second.ToList();
  }

  // Run the assignment algorithm and assign the hierarchical objects.
  public abstract void Assign();
}
