using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;

public class MaxSpeedAssignmentTest {
  public static StaticAgentConfig CreateStaticAgentConfig() {
    StaticAgentConfig config = new StaticAgentConfig();
    config.bodyConfig = new StaticAgentConfig.BodyConfig();
    config.liftDragConfig = new StaticAgentConfig.LiftDragConfig();
    return config;
  }

  [Test]
  public void AssignShouldAssignAllInterceptorsAndThreats() {
    // Define the assignment.
    IAssignment assignment = new MaxSpeedAssignment();

    // Create the interceptors.
    Interceptor interceptor1 = new GameObject("Interceptor 1").AddComponent<MissileInterceptor>();
    interceptor1.staticAgentConfig = CreateStaticAgentConfig();
    Interceptor interceptor2 = new GameObject("Interceptor 2").AddComponent<MissileInterceptor>();
    interceptor2.staticAgentConfig = CreateStaticAgentConfig();
    Interceptor interceptor3 = new GameObject("Interceptor 3").AddComponent<MissileInterceptor>();
    interceptor3.staticAgentConfig = CreateStaticAgentConfig();

    // Add rigid body components to interceptors to set their velocities.
    Rigidbody interceptorRb1 = interceptor1.gameObject.AddComponent<Rigidbody>();
    Rigidbody interceptorRb2 = interceptor2.gameObject.AddComponent<Rigidbody>();
    Rigidbody interceptorRb3 = interceptor3.gameObject.AddComponent<Rigidbody>();

    // Set the interceptor positions and velocities.
    interceptor1.transform.position = Vector3.zero;
    interceptor2.transform.position = Vector3.zero;
    interceptor3.transform.position = new Vector3(0, 100, 0);
    interceptorRb1.linearVelocity = new Vector3(0, 0, 5);
    interceptorRb2.linearVelocity = new Vector3(0, 10, 0);
    interceptorRb3.linearVelocity = new Vector3(0, 0, 5);

    List<Interceptor> interceptors =
        new List<Interceptor> { interceptor1, interceptor2, interceptor3 };

    // Create the threats.
    Threat threat1 = new GameObject("Threat 1").AddComponent<RotaryWingThreat>();
    Threat threat2 = new GameObject("Threat 2").AddComponent<RotaryWingThreat>();
    Threat threat3 = new GameObject("Threat 3").AddComponent<RotaryWingThreat>();

    // Set threat positions.
    threat1.transform.position = new Vector3(0, -1, 0);
    threat2.transform.position = new Vector3(0, 1, 0);
    threat3.transform.position = new Vector3(0, 105, 0);

    List<Threat> threats = new List<Threat> { threat1, threat2, threat3 };

    // Assign the interceptors to the threats.
    IEnumerable<IAssignment.AssignmentItem> assignments = assignment.Assign(interceptors, threats);
    Assert.AreEqual(3, assignments.Count(), "All interceptors should be assigned.");

    HashSet<Interceptor> assignedInterceptors = new HashSet<Interceptor>();
    HashSet<Threat> assignedThreats = new HashSet<Threat>();
    Dictionary<Interceptor, Threat> assignmentMap = new Dictionary<Interceptor, Threat>();

    foreach (var assignmentItem in assignments) {
      Assert.IsNotNull(assignmentItem.Interceptor, "Interceptor should not be null.");
      Assert.IsNotNull(assignmentItem.Threat, "Threat should not be null.");
      assignedInterceptors.Add(assignmentItem.Interceptor);
      assignedThreats.Add(assignmentItem.Threat);
      assignmentMap[assignmentItem.Interceptor] = assignmentItem.Threat;
    }

    Assert.AreEqual(3, assignedInterceptors.Count, "All interceptors should be unique.");
    Assert.AreEqual(3, assignedThreats.Count, "All threats should be assigned.");

    // Verify that threats are assigned to maximize the intercept speed.
    Assert.AreEqual(assignmentMap[interceptor1], threat1);
    Assert.AreEqual(assignmentMap[interceptor2], threat2);
    Assert.AreEqual(assignmentMap[interceptor3], threat3);
  }

  [Test]
  public void AssignShouldHandleMoreInterceptorsThanThreats() {
    // Define the assignment.
    IAssignment assignment = new MaxSpeedAssignment();

    // Create the interceptors.
    Interceptor interceptor1 = new GameObject("Interceptor 1").AddComponent<MissileInterceptor>();
    interceptor1.staticAgentConfig = CreateStaticAgentConfig();
    Interceptor interceptor2 = new GameObject("Interceptor 2").AddComponent<MissileInterceptor>();
    interceptor2.staticAgentConfig = CreateStaticAgentConfig();
    Interceptor interceptor3 = new GameObject("Interceptor 3").AddComponent<MissileInterceptor>();
    interceptor3.staticAgentConfig = CreateStaticAgentConfig();

    // Add rigid body components to interceptors to set their velocities.
    Rigidbody interceptorRb1 = interceptor1.gameObject.AddComponent<Rigidbody>();
    Rigidbody interceptorRb2 = interceptor2.gameObject.AddComponent<Rigidbody>();
    Rigidbody interceptorRb3 = interceptor3.gameObject.AddComponent<Rigidbody>();

    // Set the interceptor positions and velocities.
    interceptor1.transform.position = Vector3.zero;
    interceptor2.transform.position = Vector3.zero;
    interceptor3.transform.position = new Vector3(0, 100, 0);
    interceptorRb1.linearVelocity = new Vector3(0, 0, 5);
    interceptorRb2.linearVelocity = new Vector3(0, 10, 0);
    interceptorRb3.linearVelocity = new Vector3(0, 0, 5);

    List<Interceptor> interceptors =
        new List<Interceptor> { interceptor1, interceptor2, interceptor3 };

    // Create the threats.
    Threat threat1 = new GameObject("Threat 1").AddComponent<RotaryWingThreat>();
    Threat threat2 = new GameObject("Threat 2").AddComponent<RotaryWingThreat>();

    // Set threat positions.
    threat1.transform.position = new Vector3(0, -1, 0);
    threat2.transform.position = new Vector3(0, 1, 0);

    List<Threat> threats = new List<Threat> { threat1, threat2 };

    // Assign the interceptors to the threats.
    IEnumerable<IAssignment.AssignmentItem> assignments = assignment.Assign(interceptors, threats);
    Assert.AreEqual(3, assignments.Count(), "All interceptors should be assigned.");

    HashSet<Interceptor> assignedInterceptors = new HashSet<Interceptor>();
    HashSet<Threat> assignedThreats = new HashSet<Threat>();
    Dictionary<Interceptor, Threat> assignmentMap = new Dictionary<Interceptor, Threat>();

    foreach (var assignmentItem in assignments) {
      Assert.IsNotNull(assignmentItem.Interceptor, "Interceptor should not be null.");
      Assert.IsNotNull(assignmentItem.Threat, "Threat should not be null.");
      assignedInterceptors.Add(assignmentItem.Interceptor);
      assignedThreats.Add(assignmentItem.Threat);
      assignmentMap[assignmentItem.Interceptor] = assignmentItem.Threat;
    }

    Assert.AreEqual(3, assignedInterceptors.Count, "All interceptors should be assigned.");
    Assert.AreEqual(2, assignedThreats.Count, "Both threats should be assigned.");

    // Verify that threats are assigned to maximize the intercept speed.
    Assert.AreEqual(assignmentMap[interceptor1], threat1);
    Assert.AreEqual(assignmentMap[interceptor2], threat2);
    Assert.AreEqual(assignmentMap[interceptor3], threat2);
  }
}
