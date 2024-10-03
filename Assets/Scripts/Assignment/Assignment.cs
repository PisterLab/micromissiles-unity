using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;
using System.Diagnostics.Contracts;

// The assignment class is an interface for assigning a threat to each interceptor.
public interface IAssignment {
  // Assignment item type.
  // The first element corresponds to the interceptor index, and the second element
  // corresponds to the threat index.
  public struct AssignmentItem {
    public Interceptor Interceptor;
    public Threat Threat;

    public AssignmentItem(Interceptor interceptor, Threat threat) {
      Interceptor = interceptor;
      Threat = threat;
    }
  }

  // A list containing the interceptor-target assignments.

  // Assign a target to each interceptor that has not been assigned a target yet.
  [Pure]
  public abstract IEnumerable<AssignmentItem> Assign(in IReadOnlyList<Interceptor> interceptors,
                                                     in IReadOnlyList<ThreatData> threatTable);

  // Get the list of assignable interceptor indices.
  [Pure]
  protected static List<Interceptor> GetAssignableInterceptors(
      in IReadOnlyList<Interceptor> interceptors) {
    return interceptors.Where(interceptor => interceptor.IsAssignable()).ToList();
  }

  // Get the list of active threats.
  [Pure]
  protected static List<ThreatData> GetActiveThreats(in IReadOnlyList<ThreatData> threats) {
    return threats.Where(t => t.Status != ThreatStatus.DESTROYED).ToList();
  }
}
