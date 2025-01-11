using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;

public class ThreatAssignmentTests {
  [Test]
  public void Assign_Should_Assign_All_Interceptors_And_Threats() {
    // Define the threat assignment.
    ThreatAssignment threatAssignment = new ThreatAssignment();

    // Create interceptors.
    List<Interceptor> interceptors = new List<Interceptor> {
      new GameObject("Interceptor 1").AddComponent<MissileInterceptor>(),
      new GameObject("Interceptor 2").AddComponent<MissileInterceptor>(),
      new GameObject("Interceptor 3").AddComponent<MissileInterceptor>()
    };

    // Create threats.
    Threat threat1 = new GameObject("Threat 1").AddComponent<RotaryWingThreat>();
    Threat threat2 = new GameObject("Threat 2").AddComponent<RotaryWingThreat>();
    Threat threat3 = new GameObject("Threat 3").AddComponent<RotaryWingThreat>();

    // Add rigid body components to threats to set velocities.
    Rigidbody rb1 = threat1.gameObject.AddComponent<Rigidbody>();
    Rigidbody rb2 = threat2.gameObject.AddComponent<Rigidbody>();
    Rigidbody rb3 = threat3.gameObject.AddComponent<Rigidbody>();

    // Set positions and velocities.
    threat1.transform.position = Vector3.forward * -20f;
    threat2.transform.position = Vector3.forward * -20f;
    threat3.transform.position = Vector3.forward * -20f;

    rb1.linearVelocity = Vector3.forward * 5f;
    rb2.linearVelocity = Vector3.forward * 10f;
    rb3.linearVelocity = Vector3.forward * 15f;

    // Create threat.
    List<Threat> threats = new List<Threat> { threat1, threat2, threat3 };

    // Assign the interceptors to the threats.
    IEnumerable<IAssignment.AssignmentItem> assignments =
        threatAssignment.Assign(interceptors, threats);

    Assert.AreEqual(3, assignments.Count(), "All interceptors should be assigned.");

    HashSet<Interceptor> assignedInterceptors = new HashSet<Interceptor>();
    HashSet<Threat> assignedThreats = new HashSet<Threat>();

    foreach (var assignment in assignments) {
      Assert.IsNotNull(assignment.Interceptor, "Interceptor should not be null.");
      Assert.IsNotNull(assignment.Threat, "Threat should not be null.");
      assignedInterceptors.Add(assignment.Interceptor);
      assignedThreats.Add(assignment.Threat);
    }

    Assert.AreEqual(3, assignedInterceptors.Count, "All interceptors should be unique.");
    Assert.AreEqual(3, assignedThreats.Count, "All threats should be assigned.");

    // Verify that threats are assigned in order of their threat level (based on velocity and
    // distance).
    var orderedAssignments =
        assignments
            .OrderByDescending(a => a.Threat.GetVelocity().magnitude /
                                    Vector3.Distance(a.Threat.transform.position, Vector3.zero))
            .ToList();
    Assert.AreEqual(threat3, orderedAssignments[0].Threat,
                    "Highest threat should be assigned first.");
    Assert.AreEqual(threat2, orderedAssignments[1].Threat,
                    "Second highest threat should be assigned second.");
    Assert.AreEqual(threat1, orderedAssignments[2].Threat,
                    "Lowest threat should be assigned last.");
  }

  [Test]
  public void Assign_Should_Handle_More_Interceptors_Than_Threats() {
    // Define the threat assignment.
    ThreatAssignment threatAssignment = new ThreatAssignment();

    // Create interceptors.
    List<Interceptor> interceptors = new List<Interceptor> {
      new GameObject("Interceptor 1").AddComponent<MissileInterceptor>(),
      new GameObject("Interceptor 2").AddComponent<MissileInterceptor>(),
      new GameObject("Interceptor 3").AddComponent<MissileInterceptor>()
    };

    // Create threats.
    Threat threat1 = new GameObject("Threat 1").AddComponent<RotaryWingThreat>();
    Threat threat2 = new GameObject("Threat 2").AddComponent<RotaryWingThreat>();

    // Add rigid body components to threats to set velocities.
    Rigidbody rb1 = threat1.gameObject.AddComponent<Rigidbody>();
    Rigidbody rb2 = threat2.gameObject.AddComponent<Rigidbody>();

    // Set positions and velocities.
    threat1.transform.position = Vector3.up * 10f;
    threat2.transform.position = Vector3.right * 5f;

    rb1.linearVelocity = Vector3.forward * 10f;
    rb2.linearVelocity = Vector3.forward * 15f;

    // Create threats.
    List<Threat> threats = new List<Threat> { threat1, threat2 };

    // Assign the interceptors to the threats.
    IEnumerable<IAssignment.AssignmentItem> assignments =
        threatAssignment.Assign(interceptors, threats);

    Assert.AreEqual(3, assignments.Count(), "All interceptors should be assigned.");

    HashSet<Interceptor> assignedInterceptors = new HashSet<Interceptor>();
    HashSet<Threat> assignedThreats = new HashSet<Threat>();

    foreach (var assignment in assignments) {
      Assert.IsNotNull(assignment.Interceptor, "Interceptor should not be null.");
      Assert.IsNotNull(assignment.Threat, "Threat should not be null.");
      assignedInterceptors.Add(assignment.Interceptor);
      assignedThreats.Add(assignment.Threat);
    }

    Assert.AreEqual(3, assignedInterceptors.Count, "All interceptors should be assigned.");
    Assert.AreEqual(2, assignedThreats.Count, "Both threats should be assigned.");

    // Verify that threats are assigned in order of their threat level (based on velocity and
    // distance).
    var orderedAssignments =
        assignments
            .OrderByDescending(a => a.Threat.GetVelocity().magnitude /
                                    Vector3.Distance(a.Threat.transform.position, Vector3.zero))
            .ToList();
    Assert.AreEqual(threat2, orderedAssignments[0].Threat,
                    "Higher threat should be assigned first.");
    Assert.AreEqual(threat2, orderedAssignments[1].Threat,
                    "Higher threat should be assigned twice.");
    Assert.AreEqual(threat1, orderedAssignments[2].Threat, "Lower threat should be assigned last.");
  }
}
