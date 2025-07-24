using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

// Tests for Agent hierarchy behavior including acceleration calculation,
// initial velocity handling, and integration between agent types.
public class AgentTests : AgentTestBase {
  private Threat testThreat;
  private Interceptor testInterceptor;

  public override void Setup() {
    base.Setup();
    
    var threatConfig = new DynamicAgentConfig {
      agent_model = "brahmos.json",
      attack_behavior = "brahmos_direct_attack.json",
      initial_state = new InitialState { 
        position = new Vector3(0, 50, 1000),
        velocity = new Vector3(0, 0, -100)
      },
      standard_deviation = new StandardDeviation { position = Vector3.zero, velocity = Vector3.zero },
      dynamic_config = new DynamicConfig { 
        launch_config = new LaunchConfig { launch_time = 0 },
        sensor_config = new SensorConfig { type = SensorType.IDEAL, frequency = 100 }
      }
    };
    testThreat = CreateTestThreat(threatConfig);

    var interceptorConfig = new DynamicAgentConfig {
      agent_model = "hydra70.json",
      initial_state = new InitialState { position = new Vector3(100, 0, 0), velocity = new Vector3(-10, 0, 0) },
      standard_deviation = new StandardDeviation { position = Vector3.zero, velocity = Vector3.zero },
      dynamic_config = new DynamicConfig {
        sensor_config = new SensorConfig { type = SensorType.IDEAL, frequency = 100 },
        flight_config = new FlightConfig { augmentedPnEnabled = false }
      }
    };
    testInterceptor = CreateTestInterceptor(interceptorConfig);
  }

  public override void Teardown() {
    base.Teardown();
    if (testThreat != null) GameObject.DestroyImmediate(testThreat.gameObject);
    if (testInterceptor != null) GameObject.DestroyImmediate(testInterceptor.gameObject);
  }

  [Test]
  public void CreateThreat_ProperlyStoresInitialVelocity() {
    var threatConfig = new DynamicAgentConfig {
      agent_model = "brahmos.json",
      attack_behavior = "brahmos_direct_attack.json", 
      initial_state = new InitialState { 
        position = new Vector3(0, 50, 40000),
        velocity = new Vector3(0, 0, -2000f)
      },
      standard_deviation = new StandardDeviation { position = Vector3.zero, velocity = Vector3.zero },
      dynamic_config = new DynamicConfig { 
        launch_config = new LaunchConfig { launch_time = 0 },
        sensor_config = new SensorConfig { type = SensorType.IDEAL, frequency = 100 },
        flight_config = new FlightConfig { 
          evasionEnabled = false,
          augmentedPnEnabled = false 
        }
      }
    };
    
    Threat threat = CreateTestThreat(threatConfig);
    
    Vector3 storedInitialVelocity = GetPrivateField<Vector3>(threat, "_initialVelocity");
    
    Assert.AreNotEqual(Vector3.zero, storedInitialVelocity,
      "Initial velocity should be stored in _initialVelocity field");
      
    Assert.AreEqual(2000f, storedInitialVelocity.magnitude, 100f,
      "Stored initial velocity should match configured velocity");
    
    threat.SetFlightPhase(AirborneAgent.FlightPhase.INITIALIZED);
    threat.SetFlightPhase(AirborneAgent.FlightPhase.BOOST);
    Vector3 finalVelocity = threat.GetVelocity();
    
    Assert.Greater(finalVelocity.magnitude, 1900f,
      "Initial velocity should be applied during BOOST phase transition");
      
    GameObject.DestroyImmediate(threat.gameObject);
  }

  [Test]
  public void AirborneAgent_GetAcceleration_ReturnsCalculatedAcceleration() {
    var rigidbody = testThreat.GetComponent<Rigidbody>();
    if (rigidbody == null) {
      rigidbody = testThreat.gameObject.AddComponent<Rigidbody>();
    }
    
    testThreat.SetFlightPhase(AirborneAgent.FlightPhase.MIDCOURSE);
    
    testThreat.dynamicAgentConfig = new DynamicAgentConfig {
      agent_model = "brahmos.json",
      attack_behavior = "brahmos_direct_attack.json", 
      initial_state = new InitialState { 
        position = testThreat.GetPosition(),
        velocity = testThreat.GetVelocity()
      },
      standard_deviation = new StandardDeviation { position = Vector3.zero, velocity = Vector3.zero },
      dynamic_config = new DynamicConfig {
        launch_config = new LaunchConfig { launch_time = 0 },
        sensor_config = new SensorConfig { type = SensorType.IDEAL, frequency = 100 },
        flight_config = new FlightConfig { 
          evasionEnabled = false,
          evasionRangeThreshold = 1000f,
          augmentedPnEnabled = false 
        }
      }
    };
    
    Vector3 testForce = new Vector3(1000f, 0f, 0f);
    rigidbody.AddForce(testForce, ForceMode.Force);
    
    InvokePrivateMethod(testThreat, "FixedUpdate");
    
    Vector3 reportedAcceleration = testThreat.GetAcceleration();
    
    Assert.AreNotEqual(Vector3.zero, reportedAcceleration,
      "GetAcceleration should return calculated acceleration");
      
    float expectedMagnitude = testForce.magnitude / rigidbody.mass;
    Assert.Greater(reportedAcceleration.magnitude, expectedMagnitude * 0.1f,
      "Acceleration magnitude should be proportional to applied force");
  }

  [Test]
  public void AirborneAgents_GetAcceleration_ReturnsStoredValue() {
    var airborneAgents = new List<AirborneAgent> { testThreat, testInterceptor };
    
    foreach (var agent in airborneAgents) {
      var rigidbody = agent.GetComponent<Rigidbody>();
      if (rigidbody == null) {
        rigidbody = agent.gameObject.AddComponent<Rigidbody>();
      }
      
      agent.SetFlightPhase(AirborneAgent.FlightPhase.MIDCOURSE);
      
      Vector3 testAcceleration = new Vector3(25f, -9.8f, 10f);
      SetPrivateField(agent, "_acceleration", testAcceleration);
      
      Vector3 acceleration = agent.GetAcceleration();
      
      Assert.AreNotEqual(Vector3.zero, acceleration,
        $"{agent.GetType().Name} should return calculated acceleration");
        
      Assert.IsTrue(IsAccelerationPhysicallyReasonable(acceleration, agent),
        $"{agent.GetType().Name} acceleration should be physically reasonable");
    }
  }

  [Test]
  public void AgentHierarchy_SeparatesAccelerationBehavior() {
    var mockPlatform = new GameObject("MockPlatform").AddComponent<MockNonFlyingAgent>();
    mockPlatform.gameObject.AddComponent<Rigidbody>();
    
    try {
      var threatRb = testThreat.GetComponent<Rigidbody>();
      if (threatRb == null) {
        threatRb = testThreat.gameObject.AddComponent<Rigidbody>();
      }
      
      testThreat.SetFlightPhase(AirborneAgent.FlightPhase.MIDCOURSE);
      
      Vector3 testAcceleration = new Vector3(50f, -9.8f, 20f);
      SetPrivateField(testThreat, "_acceleration", testAcceleration);
      
      Vector3 flyingAcceleration = testThreat.GetAcceleration();
      Vector3 platformAcceleration = mockPlatform.GetAcceleration();
      
      Assert.IsTrue(IsAccelerationPhysicallyReasonable(flyingAcceleration, testThreat),
        "AirborneAgent should provide physics-based acceleration");
        
      Assert.AreEqual(Vector3.zero, platformAcceleration,
        "Platform agents should have zero acceleration");
        
      Assert.AreNotEqual(flyingAcceleration.magnitude, platformAcceleration.magnitude,
        "Agent hierarchy should separate acceleration behaviors");
        
    } finally {
      GameObject.DestroyImmediate(mockPlatform.gameObject);
    }
  }


  private bool IsAccelerationPhysicallyReasonable(Vector3 acceleration, Agent agent) {
    if (!float.IsFinite(acceleration.x) || !float.IsFinite(acceleration.y) || !float.IsFinite(acceleration.z)) {
      return false;
    }
    
    float magnitude = acceleration.magnitude;
    return magnitude >= 0f && magnitude <= 1000f; // 0 to ~100g for missiles
  }
}

public class MockNonFlyingAgent : Agent {
  protected override void Awake() {
    base.Awake();
    if (GetComponent<Rigidbody>() == null) {
      var rb = gameObject.AddComponent<Rigidbody>();
      rb.isKinematic = true;
      rb.useGravity = false;
    }
  }
}