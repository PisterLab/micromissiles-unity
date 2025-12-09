using UnityEngine;

// The minimum distance assignment assigns hierarchical objects to hierarchical objects by
// minimizing the overall distance between assigned objects.
public class MinDistanceAssignment : CostBasedAssignment {
  public MinDistanceAssignment(AssignDelegate assignFunction)
      : base(CalculateDistance, assignFunction) {}

  private static float CalculateDistance(IHierarchical hierarchical, IHierarchical target) {
    return Vector3.Distance(hierarchical.Position, target.Position);
  }
}
