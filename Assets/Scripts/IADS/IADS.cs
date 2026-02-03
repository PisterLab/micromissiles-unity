using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// The Integrated Air Defense System (IADS) manages the air defense strategy.
// It implements the singleton pattern to ensure that only one instance exists.
public class IADS : MonoBehaviour {
  // Hierarchy parameters.
  private const float _hierarchyUpdatePeriod = 5f;
  private const float _coverageFactor = 1f;

  // List of assets.
  private List<IHierarchical> _assets = new List<IHierarchical>();

  // The IADS only manages the launchers at the top level of the interceptor hierarchy.
  private List<IHierarchical> _launchers = new List<IHierarchical>();

  // Coroutine to perform the maintain the agent hierarchy.
  private Coroutine _hierarchyCoroutine;

  // List of threats waiting to be incorporated into the hierarchy.
  private List<IHierarchical> _newThreats = new List<IHierarchical>();

  public static IADS Instance { get; private set; }

  public IReadOnlyList<IHierarchical> Assets => _assets.AsReadOnly();

  public IReadOnlyList<IHierarchical> Launchers => _launchers.AsReadOnly();

  private void Awake() {
    if (Instance != null && Instance != this) {
      Destroy(gameObject);
    } else {
      Instance = this;
    }
  }

  private void Start() {
    SimManager.Instance.OnSimulationStarted += RegisterSimulationStarted;
    SimManager.Instance.OnSimulationEnded += RegisterSimulationEnded;
    SimManager.Instance.OnNewAsset += RegisterNewAsset;
    SimManager.Instance.OnNewLauncher += RegisterNewLauncher;
    SimManager.Instance.OnNewThreat += RegisterNewThreat;
  }

  private void OnDestroy() {
    if (_hierarchyCoroutine != null) {
      StopCoroutine(_hierarchyCoroutine);
      _hierarchyCoroutine = null;
    }
  }

  private void RegisterSimulationStarted() {
    _hierarchyCoroutine = StartCoroutine(HierarchyManager(_hierarchyUpdatePeriod));
  }

  private void RegisterSimulationEnded() {
    if (_hierarchyCoroutine != null) {
      StopCoroutine(_hierarchyCoroutine);
      _hierarchyCoroutine = null;
    }
    _assets.Clear();
    _launchers.Clear();
    _newThreats.Clear();
  }

  public void RegisterNewAsset(IInterceptor asset) {
    if (asset.HierarchicalAgent != null) {
      _assets.Add(asset.HierarchicalAgent);
    }
  }

  public void RegisterNewLauncher(IInterceptor launcher) {
    if (launcher.HierarchicalAgent != null) {
      launcher.OnAssignSubInterceptor += AssignSubInterceptor;
      launcher.OnReassignTarget += ReassignTarget;
      _launchers.Add(launcher.HierarchicalAgent);
    }
  }

  public void RegisterNewThreat(IThreat threat) {
    if (threat.HierarchicalAgent != null) {
      _newThreats.Add(threat.HierarchicalAgent);
    }
  }

  private IEnumerator HierarchyManager(float period) {
    while (true) {
      if (_newThreats.Count != 0) {
        BuildHierarchy();
      }
      yield return new WaitForSeconds(period);
    }
  }

  private void BuildHierarchy() {
    // TODO(titan): The clustering algorithm should be aware of the capacity of the launcher.
    var swarmClusterer = new KMeansClusterer(Mathf.RoundToInt(_launchers.Count / _coverageFactor));
    List<Cluster> swarms = swarmClusterer.Cluster(_newThreats);
    _newThreats.Clear();

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

      // Find the asset closest to each swarm and assign it as the target to all threats within the
      // swarm. If there are no assets, the threats will target the launcher.
      // TODO(titan): Move threat coordination into a separate module as the IADS should only manage
      // the defense strategy.
      var closestAsset =
          _assets.OrderBy(asset => Vector3.Distance(assignment.Second.Position, asset.Position))
              .FirstOrDefault();
      AssignTarget(assignment.Second, closestAsset ?? assignment.First);
    }
  }

  private void AssignSubInterceptor(IInterceptor subInterceptor) {
    if (subInterceptor.CapacityRemaining <= 0) {
      return;
    }

    // Pass the sub-interceptor through all the launchers in order of increasing distance between
    // the sub-interceptor and the launcher's target.
    var sortedLaunchers =
        Launchers.Where(launcher => launcher.Target != null && !launcher.Target.IsTerminated)
            .OrderBy(launcher =>
                         Vector3.Distance(subInterceptor.Position, launcher.Target.Position));
    foreach (var launcher in sortedLaunchers) {
      IHierarchical target = launcher.FindNewTarget(subInterceptor.HierarchicalAgent,
                                                    subInterceptor.CapacityRemaining);
      if (subInterceptor.EvaluateReassignedTarget(target)) {
        break;
      }
    }
  }

  private void ReassignTarget(IHierarchical target) {
    // Assign the closest launcher with non-zero remaining capacity to pursue the target.
    var closestLauncher =
        Launchers
            .Select(launcher => new {
              Hierarchical = launcher,
              Interceptor = (launcher as HierarchicalAgent)?.Agent as IInterceptor,
            })
            .Where(launcher => launcher.Interceptor?.CapacityPlannedRemaining > 0)
            .OrderBy(launcher => Vector3.Distance(target.Position, launcher.Hierarchical.Position))
            .FirstOrDefault();
    if (closestLauncher == null) {
      return;
    }
    closestLauncher.Interceptor.ReassignTarget(target);
  }
}
