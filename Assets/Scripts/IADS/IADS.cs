using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Integrated Air Defense System.
// Manages launchers, threat clustering, and launch coordination.
public class IADS : MonoBehaviour {
  public static IADS Instance { get; private set; }

  private const float LaunchInterceptorsPeriod = 0.4f;
  private const float CheckForEscapingThreatsPeriod = 5.0f;
  private const float ClusterThreatsPeriod = 2.0f;

  // TODO(titan): Choose the CSV file based on the interceptor type.
  private ILaunchAnglePlanner _launchAnglePlanner =
      new LaunchAngleCsvInterpolator(Path.Combine("Planning", "hydra70_launch_angle.csv"));
  private IAssignment _assignmentScheme = new MaxSpeedAssignment();

  private Coroutine _launchInterceptorsCoroutine;

  [SerializeField]
  private List<TrackFileData> _trackFiles = new List<TrackFileData>();
  private Dictionary<Agent, TrackFileData> _trackFileMap = new Dictionary<Agent, TrackFileData>();

  private List<Interceptor> _assignmentQueue = new List<Interceptor>();
  private List<Cluster> _threatClusters = new List<Cluster>();
  private Dictionary<Cluster, ThreatClusterData> _threatClusterMap =
      new Dictionary<Cluster, ThreatClusterData>();
  private Dictionary<Interceptor, Cluster> _interceptorClusterMap =
      new Dictionary<Interceptor, Cluster>();
  private HashSet<Interceptor> _assignableInterceptors = new HashSet<Interceptor>();

  private HashSet<Threat> _threatsToCluster = new HashSet<Threat>();
  private Coroutine _checkForEscapingThreatsCoroutine;
  private Coroutine _clusterThreatsCoroutine;

  private int _trackFileIdTicker = 0;

  private void Awake() {
    if (Instance == null) {
      Instance = this;
    } else {
      Destroy(gameObject);
    }
  }

  private void OnDestroy() {
    if (_launchInterceptorsCoroutine != null) {
      StopCoroutine(_launchInterceptorsCoroutine);
    }
    if (_checkForEscapingThreatsCoroutine != null) {
      StopCoroutine(_checkForEscapingThreatsCoroutine);
    }
    if (_clusterThreatsCoroutine != null) {
      StopCoroutine(_clusterThreatsCoroutine);
    }
  }

  public void Start() {
    SimManager.Instance.OnSimulationStarted += RegisterSimulationStarted;
    SimManager.Instance.OnSimulationEnded += RegisterSimulationEnded;
    SimManager.Instance.OnNewThreat += RegisterNewThreat;
    SimManager.Instance.OnNewInterceptor += RegisterNewInterceptor;
  }

  public void LateUpdate() {
    // Detach interceptors assigned to fully-destroyed clusters.
    CleanupDestroyedClusters();

    // Update the cluster centroids.
    foreach (var cluster in _threatClusters) {
      cluster.Recenter();
      _threatClusterMap[cluster].UpdateCentroid();
    }

    // Assign any interceptors that are no longer assigned to any threat.
    AssignInterceptorToThreat(
        _assignableInterceptors.Where(interceptor => !interceptor.IsTerminated()).ToList());
  }

  private void RegisterSimulationStarted() {
    _launchInterceptorsCoroutine =
        StartCoroutine(LaunchInterceptorsManager(LaunchInterceptorsPeriod));
    _checkForEscapingThreatsCoroutine =
        StartCoroutine(CheckForEscapingThreatsManager(CheckForEscapingThreatsPeriod));
    _clusterThreatsCoroutine = StartCoroutine(ClusterThreatsManager(ClusterThreatsPeriod));
  }

  private IEnumerator LaunchInterceptorsManager(float period) {
    while (true) {
      // Check whether an interceptor should be launched at a cluster and launch it.
      CheckAndLaunchInterceptors();
      yield return new WaitForSeconds(period);
    }
  }

  // Checks each threat cluster and launches interceptors as needed.
  // Now supports origin-aware launch planning with configurable assignment strategies.
  private void CheckAndLaunchInterceptors() {
    foreach (var cluster in _threatClusters) {
      // Check whether an interceptor has already been assigned to the cluster.
      if (_threatClusterMap[cluster].Status != ThreatClusterStatus.UNASSIGNED) {
        continue;
      }

      // Check whether all threats in the cluster have terminated.
      bool allTerminated = cluster.Threats.All(threat => threat.IsTerminated());
      if (allTerminated) {
        continue;
      }

      // Get the cluster's centroid position for origin selection
      Vector3 threatPosition = _threatClusterMap[cluster].Centroid.transform.position;

      // Get the interceptor configuration for this launch
      DynamicAgentConfig config = GetInterceptorConfig();
      if (config == null) {
        Debug.LogWarning("No interceptor configuration available for launch.");
        continue;
      }

      // Select appropriate launcher based on strategy
      Launcher selectedLauncher = SelectLauncherForThreat(threatPosition, config.agent_model);
      if (selectedLauncher == null) {
        Debug.LogWarning(
            $"No suitable launcher available for interceptor type {config.agent_model} against threat at {threatPosition}.");
        continue;
      }

      // Create a predictor to track the cluster's centroid.
      IPredictor predictor = new LinearExtrapolator(_threatClusterMap[cluster].Centroid);

      // Create a launcher-aware launch planner.
      ILaunchPlanner planner = new IterativeLaunchPlanner(_launchAnglePlanner, predictor);

      // Use the runtime object directly for planning
      LaunchPlan plan = planner.Plan(selectedLauncher);

      // Check whether an interceptor should be launched.
      if (plan.ShouldLaunch) {
        // Allocate capacity from the selected launcher
        if (!selectedLauncher.AllocateInterceptor()) {
          Debug.LogWarning(
              $"Failed to allocate interceptor from launcher {selectedLauncher.LauncherId} - capacity exhausted.");
          continue;
        }

        Debug.Log(
            $"Launching interceptor from {selectedLauncher.LauncherId} at {selectedLauncher.GetPosition()} " +
            $"with elevation {plan.LaunchAngle} degrees to intercept at {plan.InterceptPosition}.");
        UIManager.Instance.LogActionMessage(
            $"[IADS] Launching interceptor from {selectedLauncher.LauncherId} at elevation {plan.LaunchAngle} degrees.");

        // Create a new interceptor with launcher-aware initial state
        InitialState initialState =
            CreateInitialStateFromLauncher(selectedLauncher, plan, Time.time);
        Interceptor interceptor = SimManager.Instance.CreateInterceptor(config, initialState);

        // Store launcher reference for capacity management
        interceptor.gameObject.AddComponent<LauncherReference>().SetLauncher(selectedLauncher);

        // Assign the interceptor to the cluster.
        _interceptorClusterMap[interceptor] = cluster;
        interceptor.AssignTarget(_threatClusterMap[cluster].Centroid);
        _threatClusterMap[cluster].AssignInterceptor(interceptor);

        // Create an interceptor swarm.
        SimManager.Instance.AddInterceptorSwarm(new List<Agent> { interceptor as Agent });
      }
    }
  }

  // Gets the interceptor configuration for launching.
  // Supports manual launcher assignment from swarm configuration.
  private DynamicAgentConfig GetInterceptorConfig() {
    var swarmConfigs = SimManager.Instance.SimulationConfig.interceptor_swarm_configs;
    if (swarmConfigs == null || swarmConfigs.Count == 0) {
      Debug.LogError("No interceptor swarm configurations available.");
      return null;
    }

    // For now, use the first configuration. Future work could implement
    // more sophisticated configuration selection based on threat characteristics.
    return swarmConfigs[0].dynamic_agent_config;
  }

  // Selects the most appropriate launcher for engaging a threat.
  // Uses the configured assignment strategy and accounts for launcher capabilities.
  // Returns the runtime launcher object, not just the configuration.
  private Launcher SelectLauncherForThreat(Vector3 threatPosition, string interceptorType) {
    // TODO: Implement the launcher assignment strategy.
    var availableLaunchers =
        SimManager.Instance.GetLaunchers()
            .Where(l => l.SupportsInterceptorType(interceptorType) && l.HasCapacity())
            .ToList();

    if (availableLaunchers.Count == 0) {
      return null;
    }

    // For now, use a simple "closest" strategy.
    return availableLaunchers.OrderBy(l => l.GetDistanceToTarget(threatPosition)).FirstOrDefault();
  }

  // Creates an initial state for an interceptor based on the selected launcher and launch plan.
  // Uses the runtime launcher object to get actual position.
  private InitialState CreateInitialStateFromLauncher(Launcher launcher, LaunchPlan plan,
                                                      float currentTime) {
    // Use the actual GameObject position from runtime object
    Vector3 launcherPosition = launcher.GetPosition();

    InitialState initialState =
        new InitialState { position = launcherPosition,
                           velocity = plan.GetNormalizedLaunchVector(launcherPosition) * 1e-3f,
                           rotation = Vector3.zero };

    return initialState;
  }

  public bool ShouldLaunchSubmunitions(Interceptor carrier) {
    if (!HasClusterAssignment(carrier)) {
      return false;
    }
    // The carrier interceptor will spawn submunitions when any target is greater than 30 degrees
    // away from the carrier interceptor's current velocity or when any threat is within 500 meters
    // of the interceptor.
    const float SubmunitionSpawnMaxAngularDeviation = 30.0f;
    const float SubmunitionSpawnMinDistanceToThreat = 500.0f;
    const float SubmunitionSpawnMaxDistanceToThreat = 2000.0f;
    // TODO(titan): The prediction time should be a function of the submunition characteristic, such
    // as the boost time.
    const float SubmunitionSpawnPredictionTime = 0.6f;

    Cluster cluster = _interceptorClusterMap[carrier];
    List<Threat> threats = cluster.Threats.ToList();
    Vector3 carrierPosition = carrier.GetPosition();
    Vector3 carrierVelocity = carrier.GetVelocity();
    foreach (var threat in threats) {
      IPredictor predictor = new LinearExtrapolator(threat);
      PredictorState predictedState = predictor.Predict(SubmunitionSpawnPredictionTime);
      Vector3 positionToPredictedThreat = predictedState.Position - carrierPosition;
      float predictedDistanceToThreat = positionToPredictedThreat.magnitude;

      // Check whether the distance to the threat is less than the minimum distance.
      if (predictedDistanceToThreat < SubmunitionSpawnMinDistanceToThreat) {
        return true;
      }

      // Check whether the angular deviation exceeds the maximum angular deviation.
      float distanceDeviation =
          (Vector3.ProjectOnPlane(positionToPredictedThreat, carrierVelocity)).magnitude;
      float angularDeviation =
          Mathf.Asin(distanceDeviation / predictedDistanceToThreat) * Mathf.Rad2Deg;
      if (angularDeviation > SubmunitionSpawnMaxAngularDeviation &&
          predictedDistanceToThreat < SubmunitionSpawnMaxDistanceToThreat) {
        return true;
      }
    }
    return false;
  }

  // True if an interceptor still has a valid cluster assignment.
  private bool HasClusterAssignment(Interceptor interceptor) {
    return _interceptorClusterMap.ContainsKey(interceptor);
  }

  // Releases interceptors from clusters that only contain destroyed threats.
  private void CleanupDestroyedClusters() {
    foreach (Cluster cluster in _threatClusters) {
      if (!cluster.IsFullyTerminated()) {
        continue;
      }

      IReadOnlyList<Interceptor> assigned = _threatClusterMap[cluster].AssignedInterceptors;
      if (assigned.Count == 0) {
        continue;
      }

      foreach (Interceptor interceptor in assigned.ToList()) {
        ReleaseInterceptorFromCluster(cluster, interceptor);
      }
    }
  }

  // Detach a single interceptor from a cluster and decide its next action.
  // ALL interceptors are queued for reassignment in the hierarchical architecture,
  // including CarrierInterceptors.
  private void ReleaseInterceptorFromCluster(Cluster cluster, Interceptor interceptor) {
    _threatClusterMap[cluster].RemoveInterceptor(interceptor);
    _interceptorClusterMap.Remove(interceptor);
    RequestAssignInterceptorToThreat(interceptor);
  }

  public void AssignSubmunitionsToThreats(Interceptor carrier, List<Interceptor> interceptors) {
    // Assign threats to the submunitions.
    Cluster cluster = _interceptorClusterMap[carrier];
    List<Threat> threats = cluster.Threats.ToList();
    IEnumerable<IAssignment.AssignmentItem> assignments =
        _assignmentScheme.Assign(interceptors, threats);

    // Mark the cluster as delegated to submunitions.
    _threatClusterMap[cluster].RemoveInterceptor(carrier, delegated: true);

    // Apply the assignments to the submunitions.
    foreach (var assignment in assignments) {
      assignment.Interceptor.AssignTarget(assignment.Threat);
    }

    // Check whether any submunitions were not assigned to a threat.
    foreach (var interceptor in interceptors) {
      if (!interceptor.HasAssignedTarget()) {
        RequestAssignInterceptorToThreat(interceptor);
      }
    }
  }

  public void RequestAssignInterceptorToThreat(Interceptor interceptor) {
    interceptor.UnassignTarget();
    _assignableInterceptors.Add(interceptor);
  }

  private void AssignInterceptorToThreat(in IReadOnlyList<Interceptor> interceptors) {
    if (interceptors.Count == 0) {
      return;
    }

    // The threat originally assigned to the interceptor has been terminated, so assign another
    // threat to the interceptor.

    // This pulls from all available track files, not from our previously assigned cluster.
    List<Threat> threats = _trackFiles.Where(trackFile => trackFile.Agent is Threat)
                               .Select(trackFile => trackFile.Agent as Threat)
                               .ToList();
    if (threats.Count == 0) {
      return;
    }

    IEnumerable<IAssignment.AssignmentItem> assignments =
        _assignmentScheme.Assign(interceptors, threats);

    // Apply the assignments to the submunitions.
    foreach (var assignment in assignments) {
      assignment.Interceptor.AssignTarget(assignment.Threat);
      _assignableInterceptors.Remove(assignment.Interceptor);
    }
  }

  public void RequestClusterThreat(Threat threat) {
    _threatsToCluster.Add(threat);
  }

  private IEnumerator CheckForEscapingThreatsManager(float period) {
    while (true) {
      yield return new WaitForSeconds(period);
      CheckForEscapingThreats();
    }
  }

  private void CheckForEscapingThreats() {
    List<Threat> threats = _trackFiles
                               .Where(trackFile => trackFile.Status == TrackStatus.ASSIGNED &&
                                                   trackFile.Agent is Threat)
                               .Select(trackFile => trackFile.Agent as Threat)
                               .ToList();
    if (threats.Count == 0) {
      return;
    }

    // Check whether the threats are escaping the pursuing interceptors.
    foreach (var threat in threats) {
      bool isEscaping = threat.AssignedInterceptors.All(interceptor => {
        Vector3 interceptorPosition = interceptor.GetPosition();
        Vector3 threatPosition = threat.GetPosition();

        float threatTimeToHit = (float)(threatPosition.magnitude / threat.GetSpeed());
        float interceptorTimeToHit =
            (float)((threatPosition - interceptorPosition).magnitude / interceptor.GetSpeed());
        return interceptorPosition.magnitude > threatPosition.magnitude ||
               threatTimeToHit < interceptorTimeToHit;
      });
      if (isEscaping) {
        RequestClusterThreat(threat);
      }
    }
  }

  private IEnumerator ClusterThreatsManager(float period) {
    while (true) {
      ClusterThreats();
      yield return new WaitForSeconds(period);
    }
  }

  private void ClusterThreats() {
    // Maximum number of threats per cluster.
    const int MaxSize = 7;
    // Maximum cluster radius in meters.
    const float MaxRadius = 500;

    // Filter the threats.
    List<Threat> threats =
        _threatsToCluster
            .Where(threat => !threat.IsTerminated() && threat.AssignedInterceptors.Count == 0)
            .ToList();
    if (threats.Count == 0) {
      return;
    }

    // Cluster threats.
    IClusterer clusterer = new AgglomerativeClusterer(new List<Agent>(threats), MaxSize, MaxRadius);
    clusterer.Cluster();
    var clusters = clusterer.Clusters;
    Debug.Log($"Clustered {threats.Count} threats into {clusters.Count} clusters.");
    UIManager.Instance.LogActionMessage(
        $"[IADS] Clustered {threats.Count} threats into {clusters.Count} clusters.");

    _threatClusters = clusters.ToList();
    foreach (var cluster in clusters) {
      _threatClusterMap.Add(cluster, new ThreatClusterData(cluster));
    }

    _threatsToCluster.Clear();
  }

  public void RegisterNewThreat(Threat threat) {
    string trackID = $"T{1000 + ++_trackFileIdTicker}";
    ThreatData trackFile = new ThreatData(threat, trackID);
    _trackFiles.Add(trackFile);
    _trackFileMap.Add(threat, trackFile);
    RequestClusterThreat(threat);

    threat.OnThreatHit += RegisterThreatHit;
    threat.OnThreatMiss += RegisterThreatMiss;
  }

  public void RegisterNewInterceptor(Interceptor interceptor) {
    string trackID = $"I{2000 + ++_trackFileIdTicker}";
    InterceptorData trackFile = new InterceptorData(interceptor, trackID);
    _trackFiles.Add(trackFile);
    _trackFileMap.Add(interceptor, trackFile);

    interceptor.OnInterceptMiss += RegisterInterceptorMiss;
    interceptor.OnInterceptHit += RegisterInterceptorHit;
  }

  private void RegisterInterceptorHit(Interceptor interceptor, Threat threat) {
    var threatTrack = _trackFileMap[threat] as ThreatData;
    var interceptorTrack = _trackFileMap[interceptor] as InterceptorData;

    if (threatTrack != null) {
      threatTrack.RemoveInterceptor(interceptor);
      threatTrack.MarkDestroyed();
    }

    if (interceptorTrack != null) {
      interceptorTrack.RemoveThreat(threat);
      interceptorTrack.MarkDestroyed();
    }

    // Assign the interceptors to other threats.
    foreach (var assignedInterceptor in threat.AssignedInterceptors.ToList()) {
      if (assignedInterceptor is not CarrierInterceptor) {
        RequestAssignInterceptorToThreat(assignedInterceptor as Interceptor);
      }
    }
  }

  private void RegisterInterceptorMiss(Interceptor interceptor, Threat threat) {
    // Assign the interceptor to another threat.
    RequestAssignInterceptorToThreat(interceptor);

    var threatTrack = _trackFileMap[threat] as ThreatData;
    var interceptorTrack = _trackFileMap[interceptor] as InterceptorData;

    if (threatTrack != null) {
      threatTrack.RemoveInterceptor(interceptor);

      // Check if the threat is being targeted by at least one interceptor.
      if (threatTrack.AssignedInterceptorCount == 0) {
        RequestClusterThreat(threat);
      }
    }

    if (interceptorTrack != null) {
      interceptorTrack.RemoveThreat(threat);
      interceptorTrack.MarkDestroyed();
    }
  }

  private void RegisterThreatHit(Threat threat) {
    var threatTrack = _trackFileMap[threat] as ThreatData;
    if (threatTrack != null) {
      threatTrack.MarkDestroyed();
    }

    // Re-assign the assigned interceptors to other threats.
    foreach (var interceptor in threat.AssignedInterceptors.ToList()) {
      RequestAssignInterceptorToThreat(interceptor as Interceptor);
    }
  }

  private void RegisterThreatMiss(Threat threat) {
    // The threat missed (e.g., it hit the floor).
    // Re-assign the assigned interceptors to other threats.
    foreach (var interceptor in threat.AssignedInterceptors.ToList()) {
      RequestAssignInterceptorToThreat(interceptor as Interceptor);
    }

    var threatTrack = _trackFileMap[threat] as ThreatData;
    if (threatTrack != null) {
      threatTrack.MarkDestroyed();
    }
  }

  public List<ThreatData> GetThreatTracks() => _trackFiles.OfType<ThreatData>().ToList();

  public List<InterceptorData> GetInterceptorTracks() =>
      _trackFiles.OfType<InterceptorData>().ToList();

  private void RegisterSimulationEnded() {
    _trackFiles.Clear();
    _trackFileMap.Clear();
    _assignmentQueue.Clear();
    _threatClusters.Clear();
    _threatClusterMap.Clear();
    _interceptorClusterMap.Clear();
    _assignableInterceptors.Clear();
    _threatsToCluster.Clear();
    _trackFileIdTicker = 0;
  }
}
