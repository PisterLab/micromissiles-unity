using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using UnityEngine;

// The round-robin assignment class assigns interceptors to the targets in a
// round-robin order using the new paradigm.
public class RoundRobinAssignment : IAssignment {
  // Previous target index that was assigned.
  private int prevTargetIndex = -1;

  // Assign a target to each interceptor that has not been assigned a target yet.
  [Pure]
  public IEnumerable<IAssignment.AssignmentItem> Assign(in IReadOnlyList<Interceptor> interceptors,
                                                        in IReadOnlyList<Threat> threats) {
    List<IAssignment.AssignmentItem> assignments = new List<IAssignment.AssignmentItem>();

    // Get the list of interceptors that are available for assignment.
    List<Interceptor> assignableInterceptors = IAssignment.GetAssignableInterceptors(interceptors);
    if (assignableInterceptors.Count == 0) {
      return assignments;
    }

    // Get the list of active threats that need to be addressed.
    List<Threat> activeThreats = IAssignment.GetActiveThreats(threats);
    if (activeThreats.Count == 0) {
      Debug.LogWarning("No active threats found.");
      return assignments;
    }

    // Perform round-robin assignment.
    foreach (Interceptor interceptor in assignableInterceptors) {
      // Determine the next target index in a round-robin fashion.
      int nextTargetIndex = (prevTargetIndex + 1) % activeThreats.Count;
      Threat threat = activeThreats[nextTargetIndex];

      // Assign the interceptor to the selected threat.
      assignments.Add(new IAssignment.AssignmentItem(interceptor, threat));

      // Update the previous target index.
      prevTargetIndex = nextTargetIndex;
    }

    return assignments;
  }
}
