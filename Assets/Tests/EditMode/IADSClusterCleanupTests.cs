using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class IADSClusterCleanupTests : AgentTestBase {
  private IADS iads;

  [SetUp]
  public override void Setup() {
    base.Setup();
    iads = new GameObject("IADS").AddComponent<IADS>();
    iads.Start();
  }

  [TearDown]
  public override void Teardown() {
    base.Teardown();
    if (iads != null)
      GameObject.DestroyImmediate(iads.gameObject);
  }

  private DynamicAgentConfig ThreatCfg() => new DynamicAgentConfig {
    agent_model = "brahmos.pbtxt", attack_behavior = "brahmos_direct_attack.json",
    initial_state = new InitialState { position = Vector3.zero, velocity = Vector3.zero },
    standard_deviation = new StandardDeviation { position = Vector3.zero, velocity = Vector3.zero },
    dynamic_config = new DynamicConfig { sensor_config = new SensorConfig { type = SensorType.IDEAL,
                                                                            frequency = 10 } }
  };

  private DynamicAgentConfig CarrierCfg() => new DynamicAgentConfig {
    agent_model = "hydra70.pbtxt",
    initial_state = new InitialState { position = Vector3.zero, velocity = Vector3.forward * 50 },
    standard_deviation = new StandardDeviation { position = Vector3.zero, velocity = Vector3.zero },
    dynamic_config =
        new DynamicConfig { sensor_config = new SensorConfig { type = SensorType.IDEAL,
                                                               frequency = 10 },
                            flight_config = new FlightConfig { augmentedPnEnabled = false } }
  };

  private DynamicAgentConfig MissileCfg() => new DynamicAgentConfig {
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
    foreach (var t in threats) cluster.AddObject(t.gameObject);
    var tcd = new ThreatClusterData(cluster);
    var clusters = new List<Cluster> { cluster };
    SetPrivateField(iads, "_threatClusters", clusters);
    var map = new Dictionary<Cluster, ThreatClusterData> { [cluster] = tcd };
    SetPrivateField(iads, "_threatClusterMap", map);
    return (cluster, tcd);
  }

  [Test]
  public void Carrier_Unassigned_WhenClusterFullyDestroyed() {
    var thA = CreateTestThreat(ThreatCfg());
    var (cluster, tcd) = MakeClusterWithThreats(thA);

    var carrier = (CarrierInterceptor)CreateTestInterceptor(CarrierCfg());
    var icm = new Dictionary<Interceptor, Cluster> { [carrier] = cluster };
    SetPrivateField(iads, "_interceptorClusterMap", icm);

    carrier.AssignTarget(tcd.Centroid);
    tcd.AssignInterceptor(carrier);

    thA.TerminateAgent();

    iads.LateUpdate();

    Assert.IsFalse(carrier.HasAssignedTarget(), "Carrier should be unassigned (ballistic).");

    var map = GetPrivateField<Dictionary<Interceptor, Cluster>>(iads, "_interceptorClusterMap");
    Assert.IsFalse(map.ContainsKey(carrier), "Carrier should be removed from cluster mapping.");

    Assert.IsFalse(iads.ShouldLaunchSubmunitions(carrier),
                   "Carriers without cluster mapping should not launch submunitions.");
  }
}
