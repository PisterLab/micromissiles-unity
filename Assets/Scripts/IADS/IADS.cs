using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The Integrated Air Defense System (IADS) manages the air defense strategy.
// It implements the singleton pattern to ensure that only one instance exists.
public class IADS : MonoBehaviour {
  // Clustering parameters.
  private const float _clusterThreatsPeriod = 5f;
  private const int _clusterMaxSize = 7;
  private const float _clusterMaxRadius = 500f;

  // The IADS only manages the launchers in the top level of the interceptor hierarchy.
  private List<IHierarchical> _launchers = new List<IHierarchical>();

  // Coroutine to perform the hierarchical threat clustering.
  private Coroutine _clusterThreatsCoroutine;

  // List of threats awaiting clustering.
  private List<IHierarchical> _threatsToCluster = new List<IHierarchical>();

  public static IADS Instance { get; private set; }

  public IReadOnlyList<IHierarchical> Launchers => _launchers.AsReadOnly();

  private void Awake() {
    if (Instance == null) {
      Instance = this;
    } else {
      Destroy(gameObject);
    }
  }

  private void Start() {
    SimManager.Instance.OnSimulationStarted += RegisterSimulationStarted;
    SimManager.Instance.OnSimulationEnded += RegisterSimulationEnded;
    SimManager.Instance.OnNewLauncher += RegisterNewLauncher;
    SimManager.Instance.OnNewThreat += RegisterNewThreat;
  }

  private void OnDestroy() {
    if (_clusterThreatsCoroutine != null) {
      StopCoroutine(_clusterThreatsCoroutine);
    }
  }

  private void RegisterSimulationStarted() {
    _clusterThreatsCoroutine = StartCoroutine(ClusterThreatsManager(_clusterThreatsPeriod));
  }

  private void RegisterSimulationEnded() {
    _launchers.Clear();
    _threatsToCluster.Clear();
  }

  public void RegisterNewLauncher(IInterceptor interceptor) {
    if (interceptor is IAgent agent && agent.HierarchicalAgent != null) {
      _launchers.Add(agent.HierarchicalAgent);
    }
  }

  public void RegisterNewThreat(IThreat threat) {
    if (threat is IAgent agent && agent.HierarchicalAgent != null) {
      _threatsToCluster.Add(agent.HierarchicalAgent);
    }
  }

  private IEnumerator ClusterThreatsManager(float period) {
    while (true) {
      if (_threatsToCluster.Count != 0) {
        ClusterThreats();
      }
      yield return new WaitForSeconds(period);
    }
  }

  private void ClusterThreats() {
    // TODO(titan): Implement recursive clustering. Currently, the IADS clusters the threats into
    // clusters of no more than seven threats, one cluster for each carrier interceptor. It then
    // clusters the threat clusters into threat swarms, one for each launcher. Instead, the IADS
    // should cluster the threats according to the hierarchy of the interceptors with possible
    // additional hierarchy levels if there are too many threats to handle. Cluster the threats into
    // threat clusters.
    var threatClusterer = new AgglomerativeClusterer(_clusterMaxSize, _clusterMaxRadius);
    List<Cluster> clusters = threatClusterer.Cluster(_threatsToCluster);
    Debug.Log($"Clustered {_threatsToCluster.Count} threats into {clusters.Count} clusters.");
    UIManager.Instance.LogActionMessage(
        $"[IADS] Clustered {_threatsToCluster.Count} threats into {clusters.Count} clusters.");
    _threatsToCluster.Clear();

    // Cluster the threat clusters into threat swarms.
    if (_launchers.Count == 0) {
      return;
    }
    var clusterClusterer = new KMeansClusterer(_launchers.Count);
    List<Cluster> swarms = clusterClusterer.Cluster(clusters);
    Debug.Log($"Clustered {clusters.Count} clusters into {swarms.Count} swarms.");
    UIManager.Instance.LogActionMessage(
        $"[IADS] Clustered {clusters.Count} clusters into {swarms.Count} swarms.");

    // Assign one swarm to each launcher.
    var swarmToLauncherAssignment =
        new MinDistanceAssignment(Assignment.Assignment_CoverAssignment_Assign);
    List<AssignmentItem> swarmToLauncherAssignments =
        swarmToLauncherAssignment.Assign(_launchers, swarms);
    void AssignTarget(IHierarchical hierarchical, IHierarchical target) {
      hierarchical.Target = target;
      foreach (var subHierarchical in hierarchical.ActiveSubHierarchicals) {
        AssignTarget(subHierarchical, target);
      }
    }
    foreach (var assignment in swarmToLauncherAssignments) {
      // Assign the swarm as the target to the launcher.
      assignment.First.Target = assignment.Second;
      // Assign the launcher as the target to all threats within the swarm.
      // TODO(titan): The threats would normally target the aircraft carrier within the strike
      // group.
      AssignTarget(assignment.Second, assignment.First);
    }
  }
}
