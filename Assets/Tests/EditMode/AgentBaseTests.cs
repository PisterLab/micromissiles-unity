using NUnit.Framework;
using UnityEngine;

public class AgentBaseTests {
  [Test]
  public void GetRelativeTransformation_TargetAtBoresight() {
    AgentBase agent = new GameObject("Agent").AddComponent<AgentBase>();
    Rigidbody agentRb = agent.gameObject.AddComponent<Rigidbody>();
    agent.Position = new Vector3(0, 0, 0);
    agent.Velocity = new Vector3(0, 0, 0);

    AgentBase target = new GameObject("Target").AddComponent<AgentBase>();
    Rigidbody targetRb = target.gameObject.AddComponent<Rigidbody>();
    target.Position = new Vector3(0, 0, 20);
    target.Velocity = new Vector3(0, 20, -1);

    Transformation relativeTransformation = agent.GetRelativeTransformation(target);

    Assert.AreEqual(relativeTransformation.position.cartesian,
                    target.transform.position - agent.transform.position);
    Assert.AreEqual(relativeTransformation.position.range, 20);
    Assert.AreEqual(relativeTransformation.position.azimuth, 0);
    Assert.AreEqual(relativeTransformation.position.elevation, 0);
    Assert.AreEqual(relativeTransformation.velocity.cartesian, target.Velocity - agent.Velocity);
    Assert.AreEqual(relativeTransformation.velocity.range, -1);
    Assert.AreEqual(relativeTransformation.velocity.azimuth, 0);
    Assert.AreEqual(relativeTransformation.velocity.elevation, 1);
  }

  [Test]
  public void GetRelativeTransformation_TargetAtStarboard() {
    AgentBase agent = new GameObject("Agent").AddComponent<AgentBase>();
    Rigidbody agentRb = agent.gameObject.AddComponent<Rigidbody>();
    agent.Position = new Vector3(0, 0, 0);
    agent.Velocity = new Vector3(0, 0, 0);

    AgentBase target = new GameObject("Target").AddComponent<AgentBase>();
    Rigidbody targetRb = target.gameObject.AddComponent<Rigidbody>();
    target.Position = new Vector3(20, 0, 0);
    target.Velocity = new Vector3(0, 0, 20);

    Transformation relativeTransformation = agent.GetRelativeTransformation(target);

    Assert.AreEqual(relativeTransformation.position.cartesian,
                    target.transform.position - agent.transform.position);
    Assert.AreEqual(relativeTransformation.position.range, 20);
    Assert.AreEqual(relativeTransformation.position.azimuth, Mathf.PI / 2);
    Assert.AreEqual(relativeTransformation.position.elevation, 0);
    Assert.AreEqual(relativeTransformation.velocity.cartesian, target.Velocity - agent.Velocity);
    Assert.AreEqual(relativeTransformation.velocity.range, 0);
    Assert.AreEqual(relativeTransformation.velocity.azimuth, -1);
    Assert.AreEqual(relativeTransformation.velocity.elevation, 0);
  }

  [Test]
  public void GetRelativeTransformation_TargetWithElevation() {
    AgentBase agent = new GameObject("Agent").AddComponent<AgentBase>();
    Rigidbody agentRb = agent.gameObject.AddComponent<Rigidbody>();
    agent.Position = new Vector3(0, 0, 0);
    agent.Velocity = new Vector3(0, 0, 0);

    AgentBase target = new GameObject("Target").AddComponent<AgentBase>();
    Rigidbody targetRb = target.gameObject.AddComponent<Rigidbody>();
    target.Position = new Vector3(0, 20, 0);
    target.Velocity = new Vector3(0, 0, 20);

    Transformation relativeTransformation = agent.GetRelativeTransformation(target);

    Assert.AreEqual(relativeTransformation.position.cartesian,
                    target.transform.position - agent.transform.position);
    Assert.AreEqual(relativeTransformation.position.range, 20);
    Assert.AreEqual(relativeTransformation.position.azimuth, 0);
    Assert.AreEqual(relativeTransformation.position.elevation, Mathf.PI / 2);
    Assert.AreEqual(relativeTransformation.velocity.cartesian, target.Velocity - agent.Velocity);
    Assert.AreEqual(relativeTransformation.velocity.range, 0);
    Assert.AreEqual(relativeTransformation.velocity.azimuth, 0);
    Assert.AreEqual(relativeTransformation.velocity.elevation, -1);
  }
}
