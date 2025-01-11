using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using UnityEngine;

// The maximum speed assignment class assigns interceptors to the targets to maximize the intercept
// speed by defining a cost of the assignment equal to the speed lost for the maneuver. This
// assignment scheme is slow and should only be used for few interceptors and threats.
public class MaxSpeedAssignment : IAssignment {
  // Assign a target to each interceptor that has not been assigned a target yet.
  [Pure]
  public IEnumerable<IAssignment.AssignmentItem> Assign(in IReadOnlyList<Interceptor> interceptors,
                                                        in IReadOnlyList<Threat> threats) {
    List<IAssignment.AssignmentItem> assignments = new List<IAssignment.AssignmentItem>();

    List<Interceptor> assignableInterceptors = IAssignment.GetAssignableInterceptors(interceptors);
    if (assignableInterceptors.Count == 0) {
      Debug.LogWarning("No assignable interceptors found.");
      return assignments;
    }

    List<Threat> activeThreats = IAssignment.GetActiveThreats(threats);
    if (activeThreats.Count == 0) {
      Debug.LogWarning("No active threats found.");
      return assignments;
    }

    // Greedily assign the interceptor to the threat with the lowest intercept cost if the threat
    // has not been assigned an interceptor yet.
    // TODO(titan): Use the Hungarian algorithm and create dummy nodes that are connected with
    // maximum weight.
    PriorityQueue<IAssignment.AssignmentItem> assignmentCosts =
        new PriorityQueue<IAssignment.AssignmentItem>();

    // Find all pairwise assignment costs.
    foreach (var interceptor in assignableInterceptors) {
      // The speed decays exponentially with distance and with the turn angle.
      float distanceTimeConstant = 2 * interceptor.staticAgentConfig.bodyConfig.mass /
                                   ((float)interceptor.GetDynamicPressure() *
                                    interceptor.staticAgentConfig.liftDragConfig.dragCoefficient *
                                    interceptor.staticAgentConfig.bodyConfig.crossSectionalArea);
      float angleTimeConstant = interceptor.staticAgentConfig.liftDragConfig.liftDragRatio;
      foreach (var threat in activeThreats) {
        Vector3 directionToThreat = threat.GetPosition() - interceptor.GetPosition();
        float distanceToThreat = directionToThreat.magnitude;
        float angleToThreat =
            Vector3.Angle(interceptor.GetVelocity(), directionToThreat) * Mathf.Deg2Rad;

        // The speed decay factor is the product of the speed lost through distance and of the speed
        // lost through turning, and we define the cost to be the (1 - speed decay factor).
        float cost = 1 - Mathf.Exp(-(distanceToThreat / distanceTimeConstant +
                                     angleToThreat / angleTimeConstant));
        assignmentCosts.Enqueue(new IAssignment.AssignmentItem(interceptor, threat), cost);
      }
    }

    // Greedily assign the interceptor to the threat with the lowest intercept cost if the threat
    // has not been assigned an interceptor yet.
    HashSet<Interceptor> assignedInterceptors = new HashSet<Interceptor>();
    HashSet<Threat> targetedThreats = new HashSet<Threat>();
    foreach (var assignmentItem in assignmentCosts) {
      if (!assignedInterceptors.Contains(assignmentItem.Interceptor) &&
          !targetedThreats.Contains(assignmentItem.Threat)) {
        assignments.Add(assignmentItem);
        assignedInterceptors.Add(assignmentItem.Interceptor);
        targetedThreats.Add(assignmentItem.Threat);
      }
    }

    // Iterate through the priority queue again for unassigned interceptors.
    foreach (var assignmentItem in assignmentCosts) {
      if (!assignedInterceptors.Contains(assignmentItem.Interceptor)) {
        assignments.Add(assignmentItem);
        assignedInterceptors.Add(assignmentItem.Interceptor);
        targetedThreats.Add(assignmentItem.Threat);
      }
    }
    return assignments;
  }
}
