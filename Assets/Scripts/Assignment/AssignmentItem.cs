// The assignment item is the output of an assignment algorithm.
public struct AssignmentItem {
  public IHierarchical First;
  public IHierarchical Second;

  public AssignmentItem(IHierarchical first, IHierarchical second) {
    First = first;
    Second = second;
  }
}
