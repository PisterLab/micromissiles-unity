using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

public class LinearExtrapolatorTest {
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
    Assert.AreEqual(predictedState.Position, agent.GetPosition());
    Assert.AreEqual(predictedState.Velocity, agent.GetVelocity());
  }

  [Test]
  public void TestPast() {
    Agent agent = GenerateAgent();
    LinearExtrapolator predictor = new LinearExtrapolator(agent);
    PredictorState predictedState = predictor.Predict(time: -5f);
    Assert.AreEqual(predictedState.Position, new Vector3(5, 10, -5));
    Assert.AreEqual(predictedState.Velocity, agent.GetVelocity());
  }

  [Test]
  public void TestFuture() {
    Agent agent = GenerateAgent();
    LinearExtrapolator predictor = new LinearExtrapolator(agent);
    PredictorState predictedState = predictor.Predict(time: 10f);
    Assert.AreEqual(predictedState.Position, new Vector3(20, -20, 25));
    Assert.AreEqual(predictedState.Velocity, agent.GetVelocity());
  }
}
