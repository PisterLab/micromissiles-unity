using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

// The threat assignment class assigns interceptors to the targets based
// on the threat level of the targets.
public class ThreatAssignment : IAssignment {
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

    Vector3 positionToDefend = Vector3.zero;
    List<ThreatInfo> threatInfos = CalculateThreatLevels(activeThreats, positionToDefend);

    // Sort the threats first by whether an interceptor is assigned to them already and then by
    // their threat level in descending order.
    threatInfos = threatInfos.OrderBy(threat => threat.Threat.AssignedInterceptors.Count)
                      .ThenByDescending(threat => threat.ThreatLevel)
                      .ToList();

    var assignableInterceptorsEnumerator = assignableInterceptors.GetEnumerator();
    int threatIndex = 0;
    while (assignableInterceptorsEnumerator.MoveNext()) {
      assignments.Add(new IAssignment.AssignmentItem(assignableInterceptorsEnumerator.Current,
                                                     threatInfos[threatIndex].Threat));
      threatIndex = (threatIndex + 1) % threatInfos.Count;
    }
    return assignments;
  }

  private List<ThreatInfo> CalculateThreatLevels(List<Threat> threats, Vector3 defensePosition) {
    List<ThreatInfo> threatInfos = new List<ThreatInfo>();

    foreach (var threat in threats) {
      float distanceToMean = Vector3.Distance(threat.transform.position, defensePosition);
      float velocityMagnitude = threat.GetVelocity().magnitude;

      // Calculate the threat level based on proximity and velocity.
      float threatLevel = (1 / distanceToMean) * velocityMagnitude;

      threatInfos.Add(new ThreatInfo(threat, threatLevel));
    }

    // Sort threats in descending order.
    return threatInfos.OrderByDescending(threat => threat.ThreatLevel).ToList();
  }

  private class ThreatInfo {
    public Threat Threat { get; }
    public float ThreatLevel { get; }

    public ThreatInfo(Threat threat, float threatLevel) {
      Threat = threat;
      ThreatLevel = threatLevel;
    }
  }
}
