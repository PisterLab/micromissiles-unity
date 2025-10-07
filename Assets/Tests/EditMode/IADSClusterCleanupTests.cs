using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class IADSClusterCleanupTests : AgentTestBase {
  private IADS _iads;

  [SetUp]
  public override void Setup() {
    base.Setup();
    _iads = new GameObject("IADS").AddComponent<IADS>();
    _iads.Start();
  }

  [TearDown]
  public override void Teardown() {
    base.Teardown();
    if (_iads != null) {
      GameObject.DestroyImmediate(_iads.gameObject);
    }
  }

  private DynamicAgentConfig CreateThreatConfig() => new DynamicAgentConfig {
    agent_model = "brahmos.pbtxt", attack_behavior = "brahmos_direct_attack.json",
    initial_state = new InitialState { position = Vector3.zero, velocity = Vector3.zero },
    standard_deviation = new StandardDeviation { position = Vector3.zero, velocity = Vector3.zero },
    dynamic_config = new DynamicConfig { sensor_config = new SensorConfig { type = SensorType.IDEAL,
                                                                            frequency = 10 } }
  };

  private DynamicAgentConfig CreateCarrierConfig() => new DynamicAgentConfig {
    agent_model = "hydra70.pbtxt",
    initial_state = new InitialState { position = Vector3.zero, velocity = Vector3.forward * 50 },
    standard_deviation = new StandardDeviation { position = Vector3.zero, velocity = Vector3.zero },
    dynamic_config =
        new DynamicConfig { sensor_config = new SensorConfig { type = SensorType.IDEAL,
                                                               frequency = 10 },
                            flight_config = new FlightConfig { augmentedPnEnabled = false } }
  };

  private DynamicAgentConfig CreateMissileConfig() => new DynamicAgentConfig {
    agent_model = "micromissile.pbtxt",
    initial_state = new InitialState { position = Vector3.zero, velocity = Vector3.forward * 50 },
    standard_deviation = new StandardDeviation { position = Vector3.zero, velocity = Vector3.zero },
    dynamic_config =
        new DynamicConfig { sensor_config = new SensorConfig { type = SensorType.IDEAL,
                                                               frequency = 10 },
                            flight_config = new FlightConfig { augmentedPnEnabled = false } }
  };

  private (Cluster, ThreatClusterData) MakeClusterWithThreats(params Threat[] threats) {
    var cluster = new Cluster();
    foreach (var threat in threats) {
      cluster.AddObject(threat.gameObject);
    }
    var threatClusterData = new ThreatClusterData(cluster);
    var clusters = new List<Cluster> { cluster };
    SetPrivateField(_iads, "_threatClusters", clusters);
    var map = new Dictionary<Cluster, ThreatClusterData> { [cluster] = threatClusterData };
    SetPrivateField(_iads, "_threatClusterMap", map);
    return (cluster, threatClusterData);
  }

  [Test]
  public void Carrier_Unassigned_WhenClusterFullyDestroyed() {
    var threat = CreateTestThreat(CreateThreatConfig());
    var (cluster, threatClusterData) = MakeClusterWithThreats(threat);

    var carrier = (CarrierInterceptor)CreateTestInterceptor(CreateCarrierConfig());
    var interceptorClusterMap = new Dictionary<Interceptor, Cluster> { [carrier] = cluster };
    SetPrivateField(_iads, "_interceptorClusterMap", interceptorClusterMap);

    carrier.AssignTarget(threatClusterData.Centroid);
    threatClusterData.AssignInterceptor(carrier);

    threat.TerminateAgent();
    _iads.LateUpdate();

    Assert.IsFalse(carrier.HasAssignedTarget(), "Carrier should be unassigned (ballistic).");

    var map = GetPrivateField<Dictionary<Interceptor, Cluster>>(_iads, "_interceptorClusterMap");
    Assert.IsFalse(map.ContainsKey(carrier), "Carrier should be removed from cluster mapping.");

    Assert.IsFalse(_iads.ShouldLaunchSubmunitions(carrier),
                   "Carriers without cluster mapping should not launch submunitions.");
  }
}
