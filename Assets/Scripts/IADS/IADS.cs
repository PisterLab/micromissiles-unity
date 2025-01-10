using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Integrated Air Defense System.
public class IADS : MonoBehaviour {
  public static IADS Instance { get; private set; }
  private IAssignment _assignmentScheme = new ThreatAssignment();

  [SerializeField]
  private List<ThreatData> _threatTable = new List<ThreatData>();
  private Dictionary<Threat, ThreatData> _threatDataMap = new Dictionary<Threat, ThreatData>();
  private List<Cluster> _threatClusters = new List<Cluster>();
  private Dictionary<Cluster, ThreatClusterData> _threatClusterMap =
      new Dictionary<Cluster, ThreatClusterData>();
  private Dictionary<Interceptor, Cluster> _interceptorClusterMap =
      new Dictionary<Interceptor, Cluster>();

  private void Awake() {
    if (Instance == null) {
      Instance = this;
    } else {
      Destroy(gameObject);
    }
  }

  public void Start() {
    SimManager.Instance.OnSimulationEnded += RegisterSimulationEnded;
    SimManager.Instance.OnNewThreat += RegisterNewThreat;
    SimManager.Instance.OnNewInterceptor += RegisterNewInterceptor;
  }

  public void LateUpdate() {
    // Update the cluster centroids.
    foreach (var cluster in _threatClusters) {
      _threatClusterMap[cluster].UpdateCentroid();
    }
  }

  // Cluster the threats.
  public void ClusterThreats(List<Threat> threats) {
    // Maximum number of threats per cluster.
    const int MaxSize = 7;
    // Maximum cluster radius in meters.
    const float MaxRadius = 500;

    // Cluster to threats.
    IClusterer clusterer = new AgglomerativeClusterer(
        threats.ConvertAll(threat => threat.gameObject).ToList(), MaxSize, MaxRadius);
    clusterer.Cluster();
    var clusters = clusterer.Clusters;
    Debug.Log($"Clustered {threats.Count} threats into {clusters.Count} clusters.");

    _threatClusters = clusters.ToList();
    foreach (var cluster in clusters) {
      _threatClusterMap.Add(cluster, new ThreatClusterData(cluster));
    }
  }

  // Check whether an interceptor should be launched at a cluster and launch it.
  public void CheckAndLaunchInterceptors() {
    foreach (var cluster in _threatClusters) {
      // Check whether an interceptor has already been assigned to the cluster.
      if (_threatClusterMap[cluster].Status == ThreatClusterStatus.ASSIGNED) {
        continue;
      }

      // Create a predictor to track the cluster's centroid.
      IPredictor predictor = new LinearExtrapolator(_threatClusterMap[cluster].Centroid);

      // Create a launch planner.
      // TODO(titan): Choose the CSV file based on the interceptor type.
      ILaunchAnglePlanner launchAnglePlanner =
          new LaunchAngleCsvInterpolator(Path.Combine("Planning", "hydra70_launch_angle.csv"));
      ILaunchPlanner planner = new IterativeLaunchPlanner(launchAnglePlanner, predictor);
      LaunchPlan plan = planner.Plan();

      // Check whether an interceptor should be launched.
      if (plan.ShouldLaunch) {
        Debug.Log(
            $"Launching a carrier interceptor at an elevation of {plan.LaunchAngle} degrees to position {plan.InterceptPosition}.");

        // Create a new interceptor and set its initial state.
        DynamicAgentConfig config =
            SimManager.Instance.SimulationConfig.interceptor_swarm_configs[0].dynamic_agent_config;
        config.initial_state = new InitialState();

        // Set the position, which defaults to the origin.
        config.initial_state.position = Vector3.zero;

        // Set the velocity.
        Vector3 interceptDirection =
            Coordinates3.ConvertCartesianToSpherical(plan.InterceptPosition);
        config.initial_state.velocity = Coordinates3.ConvertSphericalToCartesian(
            r: 1e-3f, azimuth: interceptDirection[1], elevation: plan.LaunchAngle);
        Interceptor interceptor = SimManager.Instance.CreateInterceptor(config);

        // Assign the interceptor to the cluster.
        _interceptorClusterMap[interceptor] = cluster;
        interceptor.AssignTarget(_threatClusterMap[cluster].Centroid);
        _threatClusterMap[cluster].AssignInterceptor(interceptor);

        // Create an interceptor swarm.
        SimManager.Instance.AddInterceptorSwarm(new List<Agent> { interceptor as Agent });
      }
    }
  }

  public bool ShouldLaunchSubmunitions(Interceptor carrier) {
    // The carrier interceptor will spawn submunitions when any target is greater than 30 degrees
    // away from the carrier interceptor's current velocity or when any threat is within 500 meters
    // of the interceptor.
    const float SubmunitionSpawnMaxAngularDeviation = 30.0f;
    const float SubmunitionSpawnMinDistanceToThreat = 500.0f;

    Cluster cluster = _interceptorClusterMap[carrier];
    List<Threat> threats =
        cluster.Objects.Select(gameObject => gameObject.GetComponent<Agent>() as Threat).ToList();
    Vector3 carrierPosition = carrier.GetPosition();
    Vector3 carrierVelocity = carrier.GetVelocity();
    foreach (var threat in threats) {
      Vector3 threatPosition = threat.GetPosition();
      Vector3 positionToThreat = threatPosition - carrierPosition;
      float distanceToThreat = positionToThreat.magnitude;

      // Check whether the distance to the threat is less than the minimum distance.
      if (distanceToThreat < SubmunitionSpawnMinDistanceToThreat) {
        return true;
      }

      // Check whether the angular deviation exceeds the maximum angular deviation.
      float distanceDeviation =
          (Vector3.ProjectOnPlane(positionToThreat, carrierVelocity)).magnitude;
      float angularDeviation = Mathf.Asin(distanceDeviation / distanceToThreat) * Mathf.Rad2Deg;
      if (angularDeviation > SubmunitionSpawnMaxAngularDeviation) {
        return true;
      }
    }
    return false;
  }

  public void AssignSubmunitionsToThreats(Interceptor carrier, List<Interceptor> interceptors) {
    // Assign threats to the submunitions.
    Cluster cluster = _interceptorClusterMap[carrier];
    List<ThreatData> threats =
        cluster.Objects
            .Select(gameObject => _threatDataMap[gameObject.GetComponent<Agent>() as Threat])
            .ToList();
    IEnumerable<IAssignment.AssignmentItem> assignments =
        _assignmentScheme.Assign(interceptors, threats);

    // Apply the assignments to the submunitions.
    foreach (var assignment in assignments) {
      assignment.Interceptor.AssignTarget(assignment.Threat);
      _threatDataMap[assignment.Threat].AssignInterceptor(assignment.Interceptor);
    }
  }

  public void RegisterNewThreat(Threat threat) {
    ThreatData threatData = new ThreatData(threat, threat.gameObject.name);
    _threatTable.Add(threatData);
    _threatDataMap.Add(threat, threatData);

    // TODO(titan): Cluster the new threat.

    // Subscribe to the threat's events.
    // TODO(dlovell): If we do not want omniscient IADS, we need to model the IADS's sensors here.
    threat.OnThreatHit += RegisterThreatHit;
    threat.OnThreatMiss += RegisterThreatMiss;
  }

  public void RegisterNewInterceptor(Interceptor interceptor) {
    interceptor.OnInterceptMiss += RegisterInterceptorMiss;
    interceptor.OnInterceptHit += RegisterInterceptorHit;
  }

  private void RegisterInterceptorHit(Interceptor interceptor, Threat threat) {
    ThreatData threatData = _threatDataMap[threat];
    if (threatData != null) {
      threatData.RemoveInterceptor(interceptor);
      MarkThreatDestroyed(threatData);
    }
  }

  private void RegisterInterceptorMiss(Interceptor interceptor, Threat threat) {
    // Remove the interceptor from the threat's assigned interceptors
    _threatDataMap[threat].RemoveInterceptor(interceptor);
  }

  private void MarkThreatDestroyed(ThreatData threatData) {
    if (threatData != null) {
      threatData.MarkDestroyed();
    }
  }

  private void RegisterThreatHit(Threat threat) {
    ThreatData threatData = _threatDataMap[threat];
    if (threatData != null) {
      MarkThreatDestroyed(threatData);
    }
  }

  private void RegisterThreatMiss(Threat threat) {
    // The threat missed.
    ThreatData threatData = _threatDataMap[threat];
    if (threatData != null) {
      MarkThreatDestroyed(threatData);
    }
  }

  private void RegisterSimulationEnded() {
    _threatTable.Clear();
    _threatDataMap.Clear();
    _threatClusters.Clear();
    _threatClusterMap.Clear();
    _interceptorClusterMap.Clear();
  }
}
