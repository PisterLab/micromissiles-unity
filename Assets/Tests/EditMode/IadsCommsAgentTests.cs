using NUnit.Framework;
using System;
using System.Reflection;
using UnityEngine;

public class IadsCommsAgentTests {
  private const float _epsilon = 1e-6f;

  private IadsCommsAgent _agent;

  [SetUp]
  public void SetUp() {
    _agent = new GameObject("IadsCommsAgent").AddComponent<IadsCommsAgent>();
    SetSimManagerInstance(null);
  }

  [TearDown]
  public void TearDown() {
    SetSimManagerInstance(null);
    if (_agent != null) {
      UnityEngine.Object.DestroyImmediate(_agent.gameObject);
    }
  }

  [Test]
  public void Position_SetterUpdatesTransformPosition() {
    Vector3 position = new Vector3(1f, 2f, 3f);

    _agent.Position = position;

    Assert.AreEqual(position, _agent.Position);
    Assert.AreEqual(position, _agent.transform.position);
  }

  [Test]
  public void Velocity_SetterUpdatesSpeed() {
    Vector3 velocity = new Vector3(3f, 4f, 0f);

    _agent.Velocity = velocity;

    Assert.AreEqual(velocity, _agent.Velocity);
    Assert.AreEqual(5f, _agent.Speed, _epsilon);
  }

  [Test]
  public void ElapsedTime_WithoutSimManager_ThrowsInvalidOperationException() {
    Assert.Throws<InvalidOperationException>(() => { float unused = _agent.ElapsedTime; });
  }

  [Test]
  public void Terminate_InvokesEventOnlyOnceAndSetsIsTerminated() {
    int terminatedCount = 0;
    _agent.OnTerminated +=
        _ => ++terminatedCount;

    _agent.Terminate();
    _agent.Terminate();

    Assert.True(_agent.IsTerminated);
    Assert.AreEqual(1, terminatedCount);
  }

  [Test]
  public void MaxForwardAcceleration_ThrowsNotSupportedException() {
    NotSupportedException exception =
        Assert.Throws<NotSupportedException>(() => _agent.MaxForwardAcceleration());

    StringAssert.Contains("comms-only mailbox proxy", exception.Message);
  }

  private static void SetSimManagerInstance(SimManager simManager) {
    FieldInfo instanceField =
        typeof(SimManager)
            .GetField("<Instance>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static);
    instanceField.SetValue(null, simManager);
  }
}
