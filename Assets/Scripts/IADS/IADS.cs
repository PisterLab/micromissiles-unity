using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Integrated Air Defense System.
public class IADS : MonoBehaviour {
  public static IADS Instance { get; private set; }
  private IAssignment _assignmentScheme = new MaxSpeedAssignment();

  [SerializeField]
  private List<Threat> _threats = new List<Threat>();
  private List<Cluster> _threatClusters = new List<Cluster>();
  private Dictionary<Cluster, ThreatClusterData> _threatClusterMap =
      new Dictionary<Cluster, ThreatClusterData>();
  private Dictionary<Interceptor, Cluster> _interceptorClusterMap =
      new Dictionary<Interceptor, Cluster>();
  private HashSet<Interceptor> _assignableInterceptors = new HashSet<Interceptor>();

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

    // Assign any interceptors that no longer have a valid target.
    AssignInterceptorToThreat(_assignableInterceptors.ToList());
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
    List<Threat> threats =
        cluster.Objects.Select(gameObject => gameObject.GetComponent<Agent>() as Threat).ToList();
    IEnumerable<IAssignment.AssignmentItem> assignments =
        _assignmentScheme.Assign(interceptors, threats);

    // Apply the assignments to the submunitions.
    foreach (var assignment in assignments) {
      assignment.Interceptor.AssignTarget(assignment.Threat);
    }
  }

  public void RequestAssignInterceptorToThreat(Interceptor interceptor) {
    interceptor.UnassignTarget();
    _assignableInterceptors.Add(interceptor);
  }

  public void AssignInterceptorToThreat(in IReadOnlyList<Interceptor> interceptors) {
    // The threat originally assigned to the interceptor has been terminated, so assign another
    // threat to the interceptor.
    IEnumerable<IAssignment.AssignmentItem> assignments =
        _assignmentScheme.Assign(interceptors, _threats);

    // Apply the assignments to the submunitions.
    foreach (var assignment in assignments) {
      assignment.Interceptor.AssignTarget(assignment.Threat);
      _assignableInterceptors.Remove(assignment.Interceptor);
    }
  }

  public void RegisterNewThreat(Threat threat) {
    _threats.Add(threat);
    // TODO(titan): Cluster the new threat.
  }

  public void RegisterNewInterceptor(Interceptor interceptor) {
    interceptor.OnInterceptMiss += RegisterInterceptorMiss;
    interceptor.OnInterceptHit += RegisterInterceptorHit;
  }

  private void RegisterInterceptorHit(Interceptor interceptor, Threat threat) {
    // Assign the other interceptors to other threats.
    foreach (var otherInterceptor in threat.AssignedInterceptors.ToList()) {
      if (interceptor != otherInterceptor) {
        RequestAssignInterceptorToThreat(otherInterceptor as Interceptor);
      }
    }
  }

  private void RegisterInterceptorMiss(Interceptor interceptor, Threat threat) {
    // Assign the interceptor to another threat.
    RequestAssignInterceptorToThreat(interceptor);
  }

  private void RegisterSimulationEnded() {
    _threats.Clear();
    _threatClusters.Clear();
    _threatClusterMap.Clear();
    _interceptorClusterMap.Clear();
    _assignableInterceptors.Clear();
  }
}
