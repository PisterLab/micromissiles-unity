using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using System.Linq;

public class TransformationTests : AgentTestBase {
  [Test]
  public void Target_At_Boresight() {
    // Create agent
    var agent = new GameObject("Agent").AddComponent<DummyAgentLegacy>();
    Rigidbody agentRb = agent.gameObject.AddComponent<Rigidbody>();
    agent.transform.position = new Vector3(0, 0, 0);
    agentRb.linearVelocity = new Vector3(0, 0, 0);
    InvokePrivateMethod(agent, "Start");

    // Create target
    var target = new GameObject("Target").AddComponent<DummyAgentLegacy>();
    target.SetPosition(new Vector3(0, 0, 20));
    Rigidbody targetRb = target.gameObject.AddComponent<Rigidbody>();
    targetRb.linearVelocity = new Vector3(0, 20, -1);
    InvokePrivateMethod(target, "Start");

    // Find the relative transformation to the target
    Transformation relativeTransformation = agent.GetRelativeTransformation(target);

    // Assert
    Assert.AreEqual(relativeTransformation.Position.Cartesian,
                    target.transform.position - agent.transform.position);
    Assert.AreEqual(relativeTransformation.Position.Range, 20);
    Assert.AreEqual(relativeTransformation.Position.Azimuth, 0);
    Assert.AreEqual(relativeTransformation.Position.Elevation, 0);
    Assert.AreEqual(relativeTransformation.Velocity.Cartesian,
                    target.GetVelocity() - agent.GetVelocity());
    Assert.AreEqual(relativeTransformation.Velocity.Range, -1);
    Assert.AreEqual(relativeTransformation.Velocity.Azimuth, 0);
    Assert.AreEqual(relativeTransformation.Velocity.Elevation, 1);
  }

  [Test]
  public void Target_At_Starboard() {
    // Create agent
    var agent = new GameObject("Agent").AddComponent<DummyAgentLegacy>();
    Rigidbody agentRb = agent.gameObject.AddComponent<Rigidbody>();
    agent.transform.position = new Vector3(0, 0, 0);
    agentRb.linearVelocity = new Vector3(0, 0, 0);

    // Create target
    var target = new GameObject("Target").AddComponent<DummyAgentLegacy>();
    target.SetPosition(new Vector3(20, 0, 0));
    Rigidbody targetRb = target.gameObject.AddComponent<Rigidbody>();
    targetRb.linearVelocity = new Vector3(0, 0, 20);

    // Find the relative transformation to the target
    Transformation relativeTransformation = agent.GetRelativeTransformation(target);

    // Assert
    Assert.AreEqual(relativeTransformation.Position.Cartesian,
                    target.transform.position - agent.transform.position);
    Assert.AreEqual(relativeTransformation.Position.Range, 20);
    Assert.AreEqual(relativeTransformation.Position.Azimuth, Mathf.PI / 2);
    Assert.AreEqual(relativeTransformation.Position.Elevation, 0);
    Assert.AreEqual(relativeTransformation.Velocity.Cartesian,
                    target.GetVelocity() - agent.GetVelocity());
    Assert.AreEqual(relativeTransformation.Velocity.Range, 0);
    Assert.AreEqual(relativeTransformation.Velocity.Azimuth, -1);
    Assert.AreEqual(relativeTransformation.Velocity.Elevation, 0);
  }

  [Test]
  public void Target_At_Elevation() {
    // Create agent
    var agent = new GameObject("Agent").AddComponent<DummyAgentLegacy>();
    Rigidbody agentRb = agent.gameObject.AddComponent<Rigidbody>();
    agent.transform.position = new Vector3(0, 0, 0);
    agentRb.linearVelocity = new Vector3(0, 0, 0);
    InvokePrivateMethod(agent, "Start");

    // Create target
    var target = new GameObject("Target").AddComponent<DummyAgentLegacy>();
    target.SetPosition(new Vector3(0, 20, 0));
    Rigidbody targetRb = target.gameObject.AddComponent<Rigidbody>();
    targetRb.linearVelocity = new Vector3(0, 0, 20);
    InvokePrivateMethod(target, "Start");

    // Find the relative transformation to the target
    Transformation relativeTransformation = agent.GetRelativeTransformation(target);

    // Assert
    Assert.AreEqual(relativeTransformation.Position.Cartesian,
                    target.transform.position - agent.transform.position);
    Assert.AreEqual(relativeTransformation.Position.Range, 20);
    Assert.AreEqual(relativeTransformation.Position.Azimuth, 0);
    Assert.AreEqual(relativeTransformation.Position.Elevation, Mathf.PI / 2);
    Assert.AreEqual(relativeTransformation.Velocity.Cartesian,
                    target.GetVelocity() - agent.GetVelocity());
    Assert.AreEqual(relativeTransformation.Velocity.Range, 0);
    Assert.AreEqual(relativeTransformation.Velocity.Azimuth, 0);
    Assert.AreEqual(relativeTransformation.Velocity.Elevation, -1);
  }
}
