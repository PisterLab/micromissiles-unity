using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using System.Diagnostics.Contracts;
// The threat assignment class assigns interceptors to the targets based
// on the threat level of the targets.
public class ThreatAssignment : IAssignment {
  // Assign a target to each interceptor that has not been assigned a target yet.
  [Pure]
  public IEnumerable<IAssignment.AssignmentItem> Assign(in IReadOnlyList<Interceptor> interceptors,
                                                        in IReadOnlyList<ThreatData> targets) {
    List<IAssignment.AssignmentItem> assignments = new List<IAssignment.AssignmentItem>();

    List<Interceptor> assignableInterceptors = IAssignment.GetAssignableInterceptors(interceptors);
    if (assignableInterceptors.Count == 0) {
      Debug.LogWarning("No assignable interceptors found");
      return assignments;
    }

    List<ThreatData> activeThreats = IAssignment.GetActiveThreats(targets);
    if (activeThreats.Count == 0) {
      Debug.LogWarning("No active threats found");
      return assignments;
    }

    Vector3 positionToDefend = Vector3.zero;
    List<ThreatInfo> threatInfos = CalculateThreatLevels(activeThreats, positionToDefend);

    // Sort ThreatInfo first by ThreatData.Status (UNASSIGNED first, then ASSIGNED)
    // Within each group, order by ThreatLevel descending
    threatInfos = threatInfos.OrderByDescending(t => t.ThreatData.Status == ThreatStatus.UNASSIGNED)
                      .ThenByDescending(t => t.ThreatLevel)
                      .ToList();

    var assignableInterceptorsEnumerator = assignableInterceptors.GetEnumerator();
    if (assignableInterceptorsEnumerator.MoveNext())  // Move to the first element
    {
      foreach (ThreatInfo threatInfo in threatInfos) {
        assignments.Add(new IAssignment.AssignmentItem(assignableInterceptorsEnumerator.Current,
                                                       threatInfo.ThreatData.Threat));
        if (!assignableInterceptorsEnumerator.MoveNext()) {
          break;
        }
      }
    }
    return assignments;
  }

  private List<ThreatInfo> CalculateThreatLevels(List<ThreatData> threatTable,
                                                 Vector3 defensePosition) {
    List<ThreatInfo> threatInfos = new List<ThreatInfo>();

    foreach (ThreatData threatData in threatTable) {
      Threat threat = threatData.Threat;
      float distanceToMean = Vector3.Distance(threat.transform.position, defensePosition);
      float velocityMagnitude = threat.GetVelocity().magnitude;

      // Calculate threat level based on proximity and velocity
      float threatLevel = (1 / distanceToMean) * velocityMagnitude;

      threatInfos.Add(new ThreatInfo(threatData, threatLevel));
    }

    // Sort threats in descending order
    return threatInfos.OrderByDescending(t => t.ThreatLevel).ToList();
  }

  private class ThreatInfo {
    public ThreatData ThreatData { get; }
    public float ThreatLevel { get; }

    public ThreatInfo(ThreatData threatData, float threatLevel) {
      ThreatData = threatData;
      ThreatLevel = threatLevel;
    }
  }
}