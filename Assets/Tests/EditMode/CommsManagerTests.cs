using NUnit.Framework;
using UnityEngine;

public class CommsManagerTests : TestBase {
  // Verifies that registering an interceptor attaches a comms node and that the manager's
  // termination callback cleans it up.
  [Test]
  public void RegisterNewInterceptor_AttachesAndTerminatesCommsNode() {
    var managerObject = new GameObject("CommsManagerTest");
    var interceptorObject = new GameObject("InterceptorTest");

    try {
      var manager = managerObject.AddComponent<CommsManager>();
      interceptorObject.AddComponent<Rigidbody>();
      var interceptor = interceptorObject.AddComponent<TestInterceptor>();

      InvokePrivateMethod(manager, "RegisterNewInterceptor", interceptor);

      Assert.NotNull(interceptor.CommsNode);
      Assert.IsFalse(interceptor.CommsNode.IsTerminated);

      InvokePrivateMethod(manager, "RegisterAgentTerminated", interceptor);

      Assert.IsTrue(interceptor.CommsNode.IsTerminated);
    } finally {
      if (interceptorObject != null) {
        Object.DestroyImmediate(interceptorObject);
      }
      if (managerObject != null) {
        Object.DestroyImmediate(managerObject);
      }
    }
  }

  // Verifies that simulation shutdown terminates every comms node currently tracked by the
  // manager.
  [Test]
  public void RegisterSimulationEnded_TerminatesTrackedCommsNodes() {
    var managerObject = new GameObject("CommsManagerTest");
    var interceptorObject = new GameObject("InterceptorTest");

    try {
      var manager = managerObject.AddComponent<CommsManager>();
      interceptorObject.AddComponent<Rigidbody>();
      var interceptor = interceptorObject.AddComponent<TestInterceptor>();

      InvokePrivateMethod(manager, "RegisterNewInterceptor", interceptor);
      InvokePrivateMethod(manager, "RegisterSimulationEnded");

      Assert.NotNull(interceptor.CommsNode);
      Assert.IsTrue(interceptor.CommsNode.IsTerminated);
    } finally {
      if (interceptorObject != null) {
        Object.DestroyImmediate(interceptorObject);
      }
      if (managerObject != null) {
        Object.DestroyImmediate(managerObject);
      }
    }
  }

  private sealed class TestInterceptor : InterceptorBase {}
}
