using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

public class LinearExtrapolatorTests {
  public static Agent GenerateAgent() {
    Agent agent = new GameObject().AddComponent<DummyAgent>();
    Rigidbody rb = agent.gameObject.AddComponent<Rigidbody>();
    agent.transform.position = new Vector3(10, 0, 5);
    rb.linearVelocity = new Vector3(1, -2, 2);
    return agent;
  }

  [Test]
  public void TestPresent() {
    Agent agent = GenerateAgent();
    LinearExtrapolator predictor = new LinearExtrapolator(agent);
    PredictorState predictedState = predictor.Predict(time: 0f);
    Assert.AreEqual(agent.GetPosition(), predictedState.Position);
    Assert.AreEqual(agent.GetVelocity(), predictedState.Velocity);
  }

  [Test]
  public void TestPast() {
    Agent agent = GenerateAgent();
    LinearExtrapolator predictor = new LinearExtrapolator(agent);
    PredictorState predictedState = predictor.Predict(time: -5f);
    Assert.AreEqual(new Vector3(5, 10, -5), predictedState.Position);
    Assert.AreEqual(agent.GetVelocity(), predictedState.Velocity);
  }

  [Test]
  public void TestFuture() {
    Agent agent = GenerateAgent();
    LinearExtrapolator predictor = new LinearExtrapolator(agent);
    PredictorState predictedState = predictor.Predict(time: 10f);
    Assert.AreEqual(new Vector3(20, -20, 25), predictedState.Position);
    Assert.AreEqual(agent.GetVelocity(), predictedState.Velocity);
  }
}
