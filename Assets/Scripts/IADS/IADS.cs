using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The Integrated Air Defense System (IADS) manages the air defense strategy.
// It implements the singleton pattern to ensure that only one instance exists.
public class IADS : MonoBehaviour {
  // Clustering parameters.
  private const float _clusterThreatsPeriod = 5f;
  private const float _coverageFactor = 1f;

  // The IADS only manages the launchers in the top level of the interceptor hierarchy.
  private List<IHierarchical> _launchers = new List<IHierarchical>();

  // Coroutine to perform the top-level threat clustering.
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
    if (interceptor.HierarchicalAgent != null) {
      _launchers.Add(interceptor.HierarchicalAgent);
    }
  }

  public void RegisterNewThreat(IThreat threat) {
    if (threat.HierarchicalAgent != null) {
      _threatsToCluster.Add(threat.HierarchicalAgent);
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
    // TODO(titan): The clustering algorithm should be aware of the capacity of the launcher.
    var swarmClusterer = new KMeansClusterer(Mathf.RoundToInt(_launchers.Count / _coverageFactor));
    List<Cluster> swarms = swarmClusterer.Cluster(_threatsToCluster);
    _threatsToCluster.Clear();

    // Assign one swarm to each launcher.
    var swarmToLauncherAssignment =
        new MinDistanceAssignment(Assignment.Assignment_EvenAssignment_Assign);
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
