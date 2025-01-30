using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics.Contracts;
using System.Linq;
using UnityEngine;

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

  // Assign a target to each interceptor that has not been assigned a target yet.
  [Pure]
  public abstract IEnumerable<AssignmentItem> Assign(in IReadOnlyList<Interceptor> interceptors,
                                                     in IReadOnlyList<Threat> threats);

  // Get the list of assignable interceptors.
  [Pure]
  public static List<Interceptor> GetAssignableInterceptors(
      in IReadOnlyList<Interceptor> interceptors) {
    return interceptors
        .Where(interceptor => interceptor.IsAssignable() && !interceptor.IsTerminated())
        .ToList();
  }

  // Get the list of active threats.
  [Pure]
  public static List<Threat> GetActiveThreats(in IReadOnlyList<Threat> threats) {
    return threats.Where(threat => !threat.IsTerminated()).ToList();
  }
}
