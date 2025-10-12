using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Configs;

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

  private AgentConfig CreateThreatConfig() => new AgentConfig {
    ConfigFile = "brahmos.pbtxt", AttackBehaviorConfigFile = "brahmos_direct_attack.pbtxt",
    InitialState =
        new Simulation.State {
          Position = new Simulation.CartesianCoordinates { X = 0, Y = 0, Z = 0 },
          Velocity = new Simulation.CartesianCoordinates { X = 0, Y = 0, Z = 0 }
        },
    StandardDeviation =
        new Simulation.State {
          Position = new Simulation.CartesianCoordinates { X = 0, Y = 0, Z = 0 },
          Velocity = new Simulation.CartesianCoordinates { X = 0, Y = 0, Z = 0 }
        },
    DynamicConfig = new DynamicConfig { SensorConfig =
                                            new Simulation.SensorConfig {
                                              Type = Simulation.SensorType.Ideal, Frequency = 10
                                            } }
  };

  private AgentConfig CreateCarrierConfig() => new AgentConfig {
    ConfigFile = "hydra70.pbtxt",
    InitialState =
        new Simulation.State {
          Position = new Simulation.CartesianCoordinates { X = 0, Y = 0, Z = 0 },
          Velocity = new Simulation.CartesianCoordinates { X = 0, Y = 0, Z = 50 }
        },
    StandardDeviation =
        new Simulation.State {
          Position = new Simulation.CartesianCoordinates { X = 0, Y = 0, Z = 0 },
          Velocity = new Simulation.CartesianCoordinates { X = 0, Y = 0, Z = 0 }
        },
    DynamicConfig =
        new DynamicConfig {
          SensorConfig = new Simulation.SensorConfig { Type = Simulation.SensorType.Ideal,
                                                       Frequency = 10 },
          FlightConfig = new FlightConfig { ControllerType = ControllerType.ProportionalNavigation }
        }
  };

  private AgentConfig CreateMissileConfig() => new AgentConfig {
    ConfigFile = "micromissile.pbtxt",
    InitialState =
        new Simulation.State {
          Position = new Simulation.CartesianCoordinates { X = 0, Y = 0, Z = 0 },
          Velocity = new Simulation.CartesianCoordinates { X = 0, Y = 0, Z = 50 }
        },
    StandardDeviation =
        new Simulation.State {
          Position = new Simulation.CartesianCoordinates { X = 0, Y = 0, Z = 0 },
          Velocity = new Simulation.CartesianCoordinates { X = 0, Y = 0, Z = 0 }
        },
    DynamicConfig =
        new DynamicConfig {
          SensorConfig = new Simulation.SensorConfig { Type = Simulation.SensorType.Ideal,
                                                       Frequency = 10 },
          FlightConfig = new FlightConfig { ControllerType = ControllerType.ProportionalNavigation }
        }
  };

  private (ClusterLegacy, ThreatClusterData) MakeClusterWithThreats(params Threat[] threats) {
    var cluster = new ClusterLegacy();
    foreach (var threat in threats) {
      cluster.AddObject(threat.gameObject);
    }
    var threatClusterData = new ThreatClusterData(cluster);
    var clusters = new List<ClusterLegacy> { cluster };
    SetPrivateField(_iads, "_threatClusters", clusters);
    var map = new Dictionary<ClusterLegacy, ThreatClusterData> { [cluster] = threatClusterData };
    SetPrivateField(_iads, "_threatClusterMap", map);
    return (cluster, threatClusterData);
  }

  [Test]
  public void Carrier_Unassigned_WhenClusterFullyDestroyed() {
    var threat = CreateTestThreat(CreateThreatConfig());
    var (cluster, threatClusterData) = MakeClusterWithThreats(threat);

    var carrier = (CarrierInterceptor)CreateTestInterceptor(CreateCarrierConfig());
    var interceptorClusterMap = new Dictionary<Interceptor, ClusterLegacy> { [carrier] = cluster };
    SetPrivateField(_iads, "_interceptorClusterMap", interceptorClusterMap);

    carrier.AssignTarget(threatClusterData.Centroid);
    threatClusterData.AssignInterceptor(carrier);

    threat.TerminateAgent();
    _iads.LateUpdate();

    Assert.IsFalse(carrier.HasAssignedTarget(), "Carrier should be unassigned (ballistic).");

    var map =
        GetPrivateField<Dictionary<Interceptor, ClusterLegacy>>(_iads, "_interceptorClusterMap");
    Assert.IsFalse(map.ContainsKey(carrier), "Carrier should be removed from cluster mapping.");

    Assert.IsFalse(_iads.ShouldLaunchSubmunitions(carrier),
                   "Carriers without cluster mapping should not launch submunitions.");
  }
}
