using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// The Integrated Air Defense System (IADS) manages the air defense strategy.
// It implements the singleton pattern to ensure that only one instance exists.
public class IADS : MonoBehaviour {
  // Hierarchy parameters.


  //TODO Joseph: (solution 1) implement variable update period. Higher frequency on lower level
  //TODO Joseph: (solution 2) implement drift boundary detection.


  private const float _hierarchyUpdatePeriod = 5f; // Var Def: seconds between top-level swarm rebuilds.
  private const float _subHierarchyUpdatePeriod = 1f; // Var Def: seconds between sub-hierarchy refreshes.
  private const float _coverageFactor = 1f; // Var Def: scales number of swarms vs launchers.

  private const float _fuzzyFuzziness = 2f; // Var Def: FCM fuzziness (m) for swarms.
  private const int _fuzzyMaxNumIterations = 25; // Var Def: FCM iteration cap for swarms.
  private const float _fuzzyEpsilon = 1e-2f; // Var Def: FCM convergence threshold for swarms.
  private const float _swarmMembershipThreshold = 0.35f; // Var Def: membership cutoff for swarm overlap.
  private const int _swarmMaxMembershipsPerThreat = 2; // Var Def: max swarms a threat can belong to.

  // The IADS only manages the launchers in the top level of the interceptor hierarchy.
  private List<IHierarchical> _launchers = new List<IHierarchical>(); // Var Def: top-level launcher nodes.

  // Coroutine to perform the maintain the agent hierarchy.
  private Coroutine _hierarchyCoroutine; // Var Def: coroutine handle for hierarchy updates.
  private float _nextHierarchyUpdateTime; // Var Def: next scheduled swarm rebuild time.
  private float _nextSubHierarchyUpdateTime; // Var Def: next scheduled sub-hierarchy refresh time.

  // List of threats waiting to be incorporated into the hierarchy.
  private List<IHierarchical> _newThreats = new List<IHierarchical>(); // Var Def: threats pending incorporation.

  public static IADS Instance { get; private set; }

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
    _hierarchyCoroutine = StartCoroutine(HierarchyManager(_subHierarchyUpdatePeriod));
  }

  private void RegisterSimulationEnded() {
    if (_hierarchyCoroutine != null) {
      StopCoroutine(_hierarchyCoroutine);
      _hierarchyCoroutine = null;
    }
    _launchers.Clear();
    _newThreats.Clear();
  }

  public void RegisterNewLauncher(IInterceptor interceptor) {
    if (interceptor.HierarchicalAgent != null) {
      interceptor.OnAssignSubInterceptor += AssignSubInterceptor;
      interceptor.OnReassignTarget += ReassignTarget;
      _launchers.Add(interceptor.HierarchicalAgent);
    }
  }

  public void RegisterNewThreat(IThreat threat) {
    if (threat.HierarchicalAgent != null) {
      _newThreats.Add(threat.HierarchicalAgent);
    }
  }

  private IEnumerator HierarchyManager(float period) {
    _nextHierarchyUpdateTime = Time.time;
    _nextSubHierarchyUpdateTime = Time.time;
    while (true) {
      float now = Time.time;
      if (now >= _nextHierarchyUpdateTime) {
        BuildHierarchy();
        _nextHierarchyUpdateTime = now + _hierarchyUpdatePeriod;
      }
      if (now >= _nextSubHierarchyUpdateTime) {
        RefreshSubHierarchies();
        _nextSubHierarchyUpdateTime = now + _subHierarchyUpdatePeriod;
      }
      yield return new WaitForSeconds(period);
    }
  }

  private void BuildHierarchy() {
    if (_launchers.Count == 0) {
      _newThreats.Clear();
      return;
    }

    List<IHierarchical> activeThreats = CollectActiveThreats();
    if (activeThreats.Count == 0) {
      _newThreats.Clear();
      return;
    }

    int k = Mathf.RoundToInt(_launchers.Count / _coverageFactor);
    k = Mathf.Clamp(k, 1, activeThreats.Count);

    var swarmClusterer =
        new FuzzyCMeansClusterer(k, _fuzzyFuzziness, _fuzzyMaxNumIterations, _fuzzyEpsilon);
    List<Vector3> initialCentroids = GetExistingSwarmCentroids(k);
    FuzzyCMeansResult swarmResult = swarmClusterer.ClusterFuzzy(
        activeThreats, initialCentroids, _swarmMembershipThreshold, _swarmMaxMembershipsPerThreat);
    List<Cluster> swarms = swarmResult.Clusters;
    _newThreats.Clear();
    if (swarms.Count == 0) {
      return;
    }

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

  private List<IHierarchical> CollectActiveThreats() {
    var threats = new HashSet<IHierarchical>();
    foreach (var launcher in _launchers) {
      if (launcher.Target == null || launcher.Target.IsTerminated) {
        continue;
      }
      foreach (var threat in launcher.Target.ActiveSubHierarchicals) {
        threats.Add(threat);
      }
    }
    foreach (var threat in _newThreats) {
      if (threat == null || threat.IsTerminated) {
        continue;
      }
      threats.Add(threat);
    }
    return threats.ToList();
  }

  private List<Vector3> GetExistingSwarmCentroids(int k) {
    var centroids = new List<Vector3>(k);
    foreach (var launcher in _launchers) {
      if (launcher.Target is Cluster cluster) {
        centroids.Add(cluster.Centroid);
      }
    }
    return centroids.Count == k ? centroids : null;
  }

  private void RefreshSubHierarchies() {
    foreach (var launcher in _launchers) {
      if (launcher is not HierarchicalAgent hierarchicalAgent) {
        continue;
      }
      if (hierarchicalAgent.Target == null || hierarchicalAgent.Target.IsTerminated) {
        continue;
      }
      if (hierarchicalAgent.Agent is not IInterceptor interceptor) {
        continue;
      }
      if (interceptor.CapacityPerSubInterceptor <= 0) {
        continue;
      }
      hierarchicalAgent.RefreshClusters(interceptor.CapacityPerSubInterceptor);
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
      if (launcher.AssignNewTarget(subInterceptor.HierarchicalAgent,
                                   subInterceptor.CapacityRemaining)) {
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
