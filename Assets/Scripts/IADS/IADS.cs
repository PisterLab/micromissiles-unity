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
  private readonly List<IHierarchical> _assets = new List<IHierarchical>();

  // The IADS only manages the launchers at the top level of the interceptor hierarchy.
  private readonly List<IHierarchical> _launchers = new List<IHierarchical>();

  // Coroutine to perform the maintain the agent hierarchy.
  private Coroutine _hierarchyCoroutine;

  // List of threats waiting to be incorporated into the hierarchy.
  private readonly List<IHierarchical> _newThreats = new List<IHierarchical>();

  // Routes mailbox traffic through the IADS proxy agent.
  private IadsCommsAgent _commsAgent;
  private Mailbox _mailboxInstance;
  private bool _mailboxSubscribed = false;

  public static IADS Instance { get; private set; }

  public IReadOnlyList<IHierarchical> Assets => _assets.AsReadOnly();

  public IReadOnlyList<IHierarchical> Launchers => _launchers.AsReadOnly();

  private void Awake() {
    if (Instance != null && Instance != this) {
      Destroy(gameObject);
      return;
    }

    Instance = this;
    _commsAgent = GetComponent<IadsCommsAgent>();
    if (_commsAgent == null) {
      _commsAgent = gameObject.AddComponent<IadsCommsAgent>();
    }
  }

  private void Start() {
    if (SimManager.Instance != null) {
      SimManager.Instance.OnSimulationStarted += RegisterSimulationStarted;
      SimManager.Instance.OnSimulationEnded += RegisterSimulationEnded;
      SimManager.Instance.OnNewAsset += RegisterNewAsset;
      SimManager.Instance.OnNewLauncher += RegisterNewLauncher;
      SimManager.Instance.OnNewThreat += RegisterNewThreat;

      // Just in case the simulation started before IADS subscribed to events.
      foreach (var agent in SimManager.Instance.Interceptors) {
        if (agent is IInterceptor interceptor) {
          RegisterNewAsset(interceptor);
          if (interceptor is LauncherBase) {
            RegisterNewLauncher(interceptor);
          }
        }
      }
      foreach (var agent in SimManager.Instance.Threats) {
        if (agent is IThreat threat) {
          RegisterNewThreat(threat);
        }
      }
      if (SimManager.Instance.IsRunning && _hierarchyCoroutine == null) {
        RegisterSimulationStarted();
      }
    }

    TrySubscribeMailbox();
  }

  private void LateUpdate() {
    if (!_mailboxSubscribed) {
      TrySubscribeMailbox();
    }
  }

  private void OnDestroy() {
    if (_mailboxSubscribed && _mailboxInstance != null) {
      _mailboxInstance.OnMessageDelivered -= HandleMailboxDelivery;
    }
    _mailboxSubscribed = false;
    _mailboxInstance = null;

    if (SimManager.Instance != null) {
      SimManager.Instance.OnSimulationStarted -= RegisterSimulationStarted;
      SimManager.Instance.OnSimulationEnded -= RegisterSimulationEnded;
      SimManager.Instance.OnNewAsset -= RegisterNewAsset;
      SimManager.Instance.OnNewLauncher -= RegisterNewLauncher;
      SimManager.Instance.OnNewThreat -= RegisterNewThreat;
    }

    if (_hierarchyCoroutine != null) {
      StopCoroutine(_hierarchyCoroutine);
      _hierarchyCoroutine = null;
    }
  }

  private void RegisterSimulationStarted() {
    if (_hierarchyCoroutine == null) {
      _hierarchyCoroutine = StartCoroutine(HierarchyManager(_hierarchyUpdatePeriod));
    }
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
    if (asset?.HierarchicalAgent == null || asset.IsPursuer ||
        _assets.Contains(asset.HierarchicalAgent)) {
      return;
    }

    _assets.Add(asset.HierarchicalAgent);
  }

  public void RegisterNewLauncher(IInterceptor launcher) {
    RegisterNewAsset(launcher);
    if (launcher?.HierarchicalAgent == null || _launchers.Contains(launcher.HierarchicalAgent)) {
      return;
    }

    launcher.OnAssignSubInterceptor += AssignSubInterceptor;
    launcher.OnReassignTarget += ReassignTarget;
    _launchers.Add(launcher.HierarchicalAgent);
  }

  public void RegisterNewThreat(IThreat threat) {
    if (threat?.HierarchicalAgent == null || _newThreats.Contains(threat.HierarchicalAgent)) {
      return;
    }

    _newThreats.Add(threat.HierarchicalAgent);
  }

  private void HandleMailboxDelivery(IAgent receiver, Message message) {
    if (!ReferenceEquals(receiver, _commsAgent)) {
      return;
    }

    HandleMailboxMessage(message);
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
    if (_newThreats.Count == 0 || _launchers.Count == 0) {
      return;
    }

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

  private void HandleMailboxMessage(Message message) {
    if (message == null) {
      return;
    }

    switch (message) {
      case AssignTargetRequestMessage assignRequest:
        HandleMailboxAssignTargetRequest(assignRequest.PayloadData.SubInterceptor);
        break;
      case ReassignTargetRequestMessage reassignRequest:
        HandleMailboxReassignTargetRequest(reassignRequest.PayloadData.Target);
        break;
    }
  }

  private void AssignSubInterceptor(IInterceptor subInterceptor) {
    if (subInterceptor == null || subInterceptor.CapacityRemaining <= 0) {
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
    if (target == null) {
      return;
    }

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

  private void HandleMailboxAssignTargetRequest(IInterceptor subInterceptor) {
    if (_commsAgent == null || subInterceptor == null || subInterceptor.CapacityRemaining <= 0) {
      return;
    }

    var sortedLaunchers =
        Launchers.Where(launcher => launcher.Target != null && !launcher.Target.IsTerminated)
            .OrderBy(launcher =>
                         Vector3.Distance(subInterceptor.Position, launcher.Target.Position));
    foreach (var launcher in sortedLaunchers) {
      IHierarchical target = launcher.FindNewTarget(subInterceptor.HierarchicalAgent,
                                                    subInterceptor.CapacityRemaining);
      if (target == null) {
        continue;
      }

      SendMessage(new AssignTargetResponseMessage(_commsAgent, subInterceptor, target));
      break;
    }
  }

  private void HandleMailboxReassignTargetRequest(IHierarchical target) {
    if (_commsAgent == null || target == null) {
      return;
    }

    var closestLauncher =
        Launchers
            .Select(launcher => new {
              Hierarchical = launcher,
              Interceptor = (launcher as HierarchicalAgent)?.Agent as IInterceptor,
            })
            .Where(launcher => launcher.Interceptor?.CapacityPlannedRemaining > 0)
            .OrderBy(launcher => Vector3.Distance(target.Position, launcher.Hierarchical.Position))
            .FirstOrDefault();
    if (closestLauncher?.Interceptor == null) {
      return;
    }

    SendMessage(new ReassignTargetRequestMessage(_commsAgent, closestLauncher.Interceptor, target));
  }

  private void SendMessage(Message message) {
    if (message == null) {
      return;
    }

    Mailbox mailbox = Mailbox.GetOrCreateInstance();
    if (mailbox == null) {
      return;
    }

    mailbox.Send(message);
  }

  private void TrySubscribeMailbox() {
    if (_mailboxSubscribed) {
      return;
    }

    _mailboxInstance = Mailbox.GetOrCreateInstance();
    if (_mailboxInstance == null) {
      return;
    }

    _mailboxInstance.OnMessageDelivered += HandleMailboxDelivery;
    _mailboxSubscribed = true;
  }
}
