using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// The cost-based assignment assigns hierarchical objects to each other based on the pairwise costs
// between them.
public class CostBasedAssignment : AssignmentBase {
  // Delegate for determining the cost of an assignment between two hierarchical objects.
  public delegate float CostDelegate(IHierarchical first, IHierarchical second);

  // Delegate for performing the assignment.
  public delegate Plugin.StatusCode AssignDelegate(int numFirst, int numSecond, float[] costs,
                                                   int[] assignedFirsts, int[] assignedSeconds,
                                                   out int numAssignments);

  // Maximum cost to prevent overflow.
  private const float _maxCost = 1e12f;

  private readonly CostDelegate _costFunction;
  private readonly AssignDelegate _assignFunction;

  public CostBasedAssignment(CostDelegate costFunction, AssignDelegate assignFunction) {
    _costFunction = costFunction;
    _assignFunction = assignFunction;
  }

  // Run the assignment algorithm and assign the hierarchical objects.
  public override List<AssignmentItem> Assign(IReadOnlyList<IHierarchical> first,
                                              IReadOnlyList<IHierarchical> second) {
    int numFirst = first.Count;
    int numSecond = second.Count;
    if (numFirst == 0 || numSecond == 0) {
      return new List<AssignmentItem>();
    }

    // Find all pairwise assignment costs.
    var assignmentCosts = new float[numFirst * numSecond];
    for (int firstIndex = 0; firstIndex < numFirst; ++firstIndex) {
      for (int secondIndex = 0; secondIndex < numSecond; ++secondIndex) {
        float cost = _costFunction(first[firstIndex], second[secondIndex]);
        float clampedCost = Mathf.Clamp(cost, -_maxCost, _maxCost);
        if (cost != clampedCost) {
          Debug.LogWarning($"Assignment cost was clamped from {cost} to {clampedCost}.");
        }
        assignmentCosts[firstIndex * numSecond + secondIndex] = clampedCost;
      }
    }

    // Solve the assignment problem.
    var assignedFirstIndices = new int[numFirst];
    var assignedSecondIndices = new int[numFirst];
    Plugin.StatusCode status =
        _assignFunction(numFirst, numSecond, assignmentCosts, assignedFirstIndices,
                        assignedSecondIndices, out int numAssignments);
    if (status != Plugin.StatusCode.StatusOk) {
      Debug.LogError(
          $"Failed to run the assignment with status code {status}. " +
          $"Number of first: {numFirst}, number of second: {numSecond}, " +
          $"minimum cost: {assignmentCosts.Min()}, maximum cost: {assignmentCosts.Max()}.");
      return new List<AssignmentItem>();
    }

    var assignments = new List<AssignmentItem>();
    for (int i = 0; i < numAssignments; ++i) {
      int firstIndex = assignedFirstIndices[i];
      int secondIndex = assignedSecondIndices[i];
      assignments.Add(
          new AssignmentItem { First = first[firstIndex], Second = second[secondIndex] });
    }
    return assignments;
  }
}
