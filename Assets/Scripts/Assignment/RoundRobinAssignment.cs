using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Diagnostics.Contracts;
// The round-robin assignment class assigns interceptors to the targets in a
// round-robin order using the new paradigm.
public class RoundRobinAssignment : IAssignment {
  // Previous target index that was assigned.
  private int prevTargetIndex = -1;

  // Assign a target to each interceptor that has not been assigned a target yet.
  [Pure]
  public IEnumerable<IAssignment.AssignmentItem> Assign(in IReadOnlyList<Interceptor> interceptors,
                                                        in IReadOnlyList<ThreatData> targets) {
    List<IAssignment.AssignmentItem> assignments = new List<IAssignment.AssignmentItem>();

    // Get the list of interceptors that are available for assignment.
    List<Interceptor> assignableInterceptors = IAssignment.GetAssignableInterceptors(interceptors);
    if (assignableInterceptors.Count == 0) {
      return assignments;
    }

    // Get the list of active threats that need to be addressed.
    List<ThreatData> activeThreats = IAssignment.GetActiveThreats(targets);
    if (activeThreats.Count == 0) {
      return assignments;
    }

    // Perform round-robin assignment.
    foreach (Interceptor interceptor in assignableInterceptors) {
      // Determine the next target index in a round-robin fashion.
      int nextTargetIndex = (prevTargetIndex + 1) % activeThreats.Count;
      ThreatData selectedThreat = activeThreats[nextTargetIndex];

      // Assign the interceptor to the selected threat.
      assignments.Add(new IAssignment.AssignmentItem(interceptor, selectedThreat.Threat));

      // Update the previous target index.
      prevTargetIndex = nextTargetIndex;
    }

    return assignments;
  }
}
