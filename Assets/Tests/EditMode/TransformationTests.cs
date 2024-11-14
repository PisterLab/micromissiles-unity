using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using System.Linq;

public class TransformationTests : AgentTestBase {
  [Test]
  public void Target_At_Boresight() {
    // Create agent
    DummyAgent agent = new GameObject("Agent").AddComponent<DummyAgent>();
    Rigidbody agentRb = agent.gameObject.AddComponent<Rigidbody>();
    agent.transform.position = new Vector3(0, 0, 0);
    agentRb.linearVelocity = new Vector3(0, 0, 0);
    InvokePrivateMethod(agent, "Start");

    // Create target
    DummyAgent target = new GameObject("Target").AddComponent<DummyAgent>();
    target.SetPosition(new Vector3(0, 0, 20));
    Rigidbody targetRb = target.gameObject.AddComponent<Rigidbody>();
    targetRb.linearVelocity = new Vector3(0, 20, -1);
    InvokePrivateMethod(target, "Start");

    // Find the relative transformation to the target
    Transformation relativeTransformation = agent.GetRelativeTransformation(target);

    // Assert
    Assert.AreEqual(relativeTransformation.position.cartesian,
                    target.transform.position - agent.transform.position);
    Assert.AreEqual(relativeTransformation.position.range, 20);
    Assert.AreEqual(relativeTransformation.position.azimuth, 0);
    Assert.AreEqual(relativeTransformation.position.elevation, 0);
    Assert.AreEqual(relativeTransformation.velocity.cartesian,
                    target.GetVelocity() - agent.GetVelocity());
    Assert.AreEqual(relativeTransformation.velocity.range, -1);
    Assert.AreEqual(relativeTransformation.velocity.azimuth, 0);
    Assert.AreEqual(relativeTransformation.velocity.elevation, 1);
  }

  [Test]
  public void Target_At_Starboard() {
    // Create agent
    DummyAgent agent = new GameObject("Agent").AddComponent<DummyAgent>();
    Rigidbody agentRb = agent.gameObject.AddComponent<Rigidbody>();
    agent.transform.position = new Vector3(0, 0, 0);
    agentRb.linearVelocity = new Vector3(0, 0, 0);

    // Create target
    DummyAgent target = new GameObject("Target").AddComponent<DummyAgent>();
    target.SetPosition(new Vector3(20, 0, 0));
    Rigidbody targetRb = target.gameObject.AddComponent<Rigidbody>();
    targetRb.linearVelocity = new Vector3(0, 0, 20);

    // Find the relative transformation to the target
    Transformation relativeTransformation = agent.GetRelativeTransformation(target);

    // Assert
    Assert.AreEqual(relativeTransformation.position.cartesian,
                    target.transform.position - agent.transform.position);
    Assert.AreEqual(relativeTransformation.position.range, 20);
    Assert.AreEqual(relativeTransformation.position.azimuth, Mathf.PI / 2);
    Assert.AreEqual(relativeTransformation.position.elevation, 0);
    Assert.AreEqual(relativeTransformation.velocity.cartesian,
                    target.GetVelocity() - agent.GetVelocity());
    Assert.AreEqual(relativeTransformation.velocity.range, 0);
    Assert.AreEqual(relativeTransformation.velocity.azimuth, -1);
    Assert.AreEqual(relativeTransformation.velocity.elevation, 0);
  }

  [Test]
  public void Target_At_Elevation() {
    // Create agent
    DummyAgent agent = new GameObject("Agent").AddComponent<DummyAgent>();
    Rigidbody agentRb = agent.gameObject.AddComponent<Rigidbody>();
    agent.transform.position = new Vector3(0, 0, 0);
    agentRb.linearVelocity = new Vector3(0, 0, 0);
    InvokePrivateMethod(agent, "Start");

    // Create target
    DummyAgent target = new GameObject("Target").AddComponent<DummyAgent>();
    target.SetPosition(new Vector3(0, 20, 0));
    Rigidbody targetRb = target.gameObject.AddComponent<Rigidbody>();
    targetRb.linearVelocity = new Vector3(0, 0, 20);
    InvokePrivateMethod(target, "Start");

    // Find the relative transformation to the target
    Transformation relativeTransformation = agent.GetRelativeTransformation(target);

    // Assert
    Assert.AreEqual(relativeTransformation.position.cartesian,
                    target.transform.position - agent.transform.position);
    Assert.AreEqual(relativeTransformation.position.range, 20);
    Assert.AreEqual(relativeTransformation.position.azimuth, 0);
    Assert.AreEqual(relativeTransformation.position.elevation, Mathf.PI / 2);
    Assert.AreEqual(relativeTransformation.velocity.cartesian,
                    target.GetVelocity() - agent.GetVelocity());
    Assert.AreEqual(relativeTransformation.velocity.range, 0);
    Assert.AreEqual(relativeTransformation.velocity.azimuth, 0);
    Assert.AreEqual(relativeTransformation.velocity.elevation, -1);
  }
}
