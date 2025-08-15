using System;
using System.Collections.Generic;
using UnityEngine;

// The maximum speed assignment class assigns interceptors to the targets to maximize the intercept
// speed by defining a cost of the assignment equal to the speed lost for the maneuver.
public class MaxSpeedAssignment : IAssignment {
  // Assign a target to each interceptor that has not been assigned a target yet.
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

    // Find all pairwise assignment costs.
    float[] assignmentCosts = new float[assignableInterceptors.Count * activeThreats.Count];
    for (int interceptorIndex = 0; interceptorIndex < assignableInterceptors.Count;
         ++interceptorIndex) {
      Interceptor interceptor = assignableInterceptors[interceptorIndex];

      // The speed decays exponentially with the travelled distance and with the bearing change.
      float distanceTimeConstant =
          2 * interceptor.staticConfig.BodyConfig.Mass /
          ((float)Constants.CalculateAirDensityAtAltitude(interceptor.GetPosition().y) *
           interceptor.staticConfig.LiftDragConfig.DragCoefficient *
           interceptor.staticConfig.BodyConfig.CrossSectionalArea);
      float angleTimeConstant = interceptor.staticConfig.LiftDragConfig.LiftDragRatio;
      // During the turn, the minimum radius dictates the minimum distance needed to make the turn.
      float minTurningRadius = (float)(interceptor.GetVelocity().sqrMagnitude /
                                       interceptor.CalculateMaxNormalAcceleration());

      for (int threatIndex = 0; threatIndex < activeThreats.Count; ++threatIndex) {
        Threat threat = activeThreats[threatIndex];
        Vector3 directionToThreat = threat.GetPosition() - interceptor.GetPosition();
        float distanceToThreat = directionToThreat.magnitude;
        float angleToThreat =
            Vector3.Angle(interceptor.GetVelocity(), directionToThreat) * Mathf.Deg2Rad;

        // The fractional speed is the product of the fractional speed after traveling the distance
        // and of the fractional speed after turning.
        float fractionalSpeed = Mathf.Exp(
            -((distanceToThreat + angleToThreat * minTurningRadius) / distanceTimeConstant +
              angleToThreat / angleTimeConstant));
        float cost = (float)interceptor.GetSpeed() / fractionalSpeed;
        assignmentCosts[interceptorIndex * activeThreats.Count + threatIndex] = cost;
      }
    }

    // Solve the assignment problem.
    int[] assignedInterceptorIndices = new int[assignableInterceptors.Count];
    int[] assignedThreatIndices = new int[assignableInterceptors.Count];
    int numAssignments = 0;
    unsafe {
      fixed(int* assignedInterceptorIndicesPtr = assignedInterceptorIndices)
          fixed(int* assignedThreatIndicesPtr = assignedThreatIndices) {
        numAssignments = Assignment.Assignment_EvenAssignment_Assign(
            assignableInterceptors.Count, activeThreats.Count, assignmentCosts,
            (IntPtr)assignedInterceptorIndicesPtr, (IntPtr)assignedThreatIndicesPtr);
      }
    }
    for (int i = 0; i < numAssignments; ++i) {
      int interceptorIndex = assignedInterceptorIndices[i];
      int threatIndex = assignedThreatIndices[i];
      assignments.Add(new IAssignment.AssignmentItem(assignableInterceptors[interceptorIndex],
                                                     activeThreats[threatIndex]));
    }
    return assignments;
  }
}
