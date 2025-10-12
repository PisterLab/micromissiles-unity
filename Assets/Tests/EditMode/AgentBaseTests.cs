using NUnit.Framework;
using UnityEngine;

public class AgentBaseTests : TestBase {
  private const float _epsilon = 1e-3f;

  [Test]
  public void GetRelativeTransformation_TargetAtBoresight() {
    AgentBase agent = new GameObject("Agent").AddComponent<AgentBase>();
    Rigidbody agentRb = agent.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(agent, "Awake");
    agent.Position = new Vector3(0, 0, 0);
    agent.Velocity = new Vector3(0, 0, 0);

    AgentBase target = new GameObject("Target").AddComponent<AgentBase>();
    Rigidbody targetRb = target.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(target, "Awake");
    target.Position = new Vector3(0, 0, 20);
    target.Velocity = new Vector3(0, 20, -1);

    Transformation relativeTransformation = agent.GetRelativeTransformation(target);

    Assert.AreEqual(relativeTransformation.position.cartesian,
                    target.transform.position - agent.transform.position);
    Assert.AreEqual(relativeTransformation.position.range, 20f, _epsilon);
    Assert.AreEqual(relativeTransformation.position.azimuth, 0f, _epsilon);
    Assert.AreEqual(relativeTransformation.position.elevation, 0f, _epsilon);
    Assert.AreEqual(relativeTransformation.velocity.cartesian, target.Velocity - agent.Velocity);
    Assert.AreEqual(relativeTransformation.velocity.range, -1f, _epsilon);
    Assert.AreEqual(relativeTransformation.velocity.azimuth, 0f, _epsilon);
    Assert.AreEqual(relativeTransformation.velocity.elevation, 1f, _epsilon);
  }

  [Test]
  public void GetRelativeTransformation_TargetAtStarboard() {
    AgentBase agent = new GameObject("Agent").AddComponent<AgentBase>();
    Rigidbody agentRb = agent.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(agent, "Awake");
    agent.Position = new Vector3(0, 0, 0);
    agent.Velocity = new Vector3(0, 0, 0);

    AgentBase target = new GameObject("Target").AddComponent<AgentBase>();
    Rigidbody targetRb = target.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(target, "Awake");
    target.Position = new Vector3(20, 0, 0);
    target.Velocity = new Vector3(0, 0, 20);

    Transformation relativeTransformation = agent.GetRelativeTransformation(target);

    Assert.AreEqual(relativeTransformation.position.cartesian,
                    target.transform.position - agent.transform.position);
    Assert.AreEqual(relativeTransformation.position.range, 20f, _epsilon);
    Assert.AreEqual(relativeTransformation.position.azimuth, Mathf.PI / 2, _epsilon);
    Assert.AreEqual(relativeTransformation.position.elevation, 0f, _epsilon);
    Assert.AreEqual(relativeTransformation.velocity.cartesian, target.Velocity - agent.Velocity);
    Assert.AreEqual(relativeTransformation.velocity.range, 0f, _epsilon);
    Assert.AreEqual(relativeTransformation.velocity.azimuth, -1f, _epsilon);
    Assert.AreEqual(relativeTransformation.velocity.elevation, 0f, _epsilon);
  }

  [Test]
  public void GetRelativeTransformation_TargetWithElevation() {
    AgentBase agent = new GameObject("Agent").AddComponent<AgentBase>();
    Rigidbody agentRb = agent.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(agent, "Awake");
    agent.Position = new Vector3(0, 0, 0);
    agent.Velocity = new Vector3(0, 0, 0);

    AgentBase target = new GameObject("Target").AddComponent<AgentBase>();
    Rigidbody targetRb = target.gameObject.AddComponent<Rigidbody>();
    InvokePrivateMethod(target, "Awake");
    target.Position = new Vector3(0, 20, 0);
    target.Velocity = new Vector3(0, 0, 20);

    Transformation relativeTransformation = agent.GetRelativeTransformation(target);

    Assert.AreEqual(relativeTransformation.position.cartesian,
                    target.transform.position - agent.transform.position);
    Assert.AreEqual(relativeTransformation.position.range, 20f, _epsilon);
    Assert.AreEqual(relativeTransformation.position.azimuth, 0f, _epsilon);
    Assert.AreEqual(relativeTransformation.position.elevation, Mathf.PI / 2, _epsilon);
    Assert.AreEqual(relativeTransformation.velocity.cartesian, target.Velocity - agent.Velocity);
    Assert.AreEqual(relativeTransformation.velocity.range, 0f, _epsilon);
    Assert.AreEqual(relativeTransformation.velocity.azimuth, 0f, _epsilon);
    Assert.AreEqual(relativeTransformation.velocity.elevation, -1f, _epsilon);
  }
}
