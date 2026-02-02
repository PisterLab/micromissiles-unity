using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Base implementation of an interceptor.
public abstract class InterceptorBase : AgentBase, IInterceptor {
  public event InterceptHitMissEventHandler OnHit;
  public event InterceptHitMissEventHandler OnMiss;
  public event InterceptorAssignEventHandler OnAssignSubInterceptor;
  public event TargetReassignEventHandler OnReassignTarget;

  // Default proportional navigation controller gain.
  private const float _proportionalNavigationGain = 5f;

  // Time to accumulate unassigned targets before launching additional sub-interceptors.
  private const float _unassignedTargetsLaunchPeriod = 2.5f;

  public IEscapeDetector EscapeDetector { get; set; }

  // Maximum number of threats that this interceptor can target.
  [SerializeField]
  private int _capacity;

  // Capacity of each sub-interceptor.
  [SerializeField]
  private int _capacityPerSubInterceptor;

  // Number of sub-interceptors.
  [SerializeField]
  private int _numSubInterceptors;

  // Number of sub-interceptors remaining that can be planned to launch.
  [SerializeField]
  private int _numSubInterceptorsPlannedRemaining;

  // Number of sub-interceptors remaining.
  [SerializeField]
  private int _numSubInterceptorsRemaining;

  public int Capacity {
    get => _capacity;
  protected
    set { _capacity = value; }
  }
  public int CapacityPerSubInterceptor {
    get => _capacityPerSubInterceptor;
  protected
    set { _capacityPerSubInterceptor = value; }
  }
  public virtual int CapacityPlannedRemaining => CapacityPerSubInterceptor *
                                                 NumSubInterceptorsPlannedRemaining;
  public virtual int CapacityRemaining => CapacityPerSubInterceptor * NumSubInterceptorsRemaining;
  public int NumSubInterceptors {
    get => _numSubInterceptors;
  protected
    set { _numSubInterceptors = value; }
  }
  public int NumSubInterceptorsPlannedRemaining {
    get => _numSubInterceptorsPlannedRemaining;
  protected
    set { _numSubInterceptorsPlannedRemaining = value; }
  }
  public int NumSubInterceptorsRemaining {
    get => _numSubInterceptorsRemaining;
  protected
    set { _numSubInterceptorsRemaining = value; }
  }

  // If true, the interceptor can be reassigned to other targets.
  public virtual bool IsReassignable => true;

  // Set of unassigned targets for which an additional sub-interceptor should be launched.
  private HashSet<IHierarchical> _unassignedTargets = new HashSet<IHierarchical>();

  // Coroutine for handling unassigned targets.
  private Coroutine _unassignedTargetsCoroutine;

  public void AssignSubInterceptor(IInterceptor subInterceptor) {
    if (subInterceptor.CapacityRemaining <= 0) {
      return;
    }

    // Assign a new target to the sub-interceptor within the parent interceptor's assigned targets.
    if (!HierarchicalAgent.AssignNewTarget(subInterceptor.HierarchicalAgent,
                                           subInterceptor.CapacityRemaining)) {
      // Propagate the sub-interceptor target assignment to the parent interceptor above.
      OnAssignSubInterceptor?.Invoke(subInterceptor);
    }
  }

  public void ReassignTarget(IHierarchical target) {
    // If a target needs to be re-assigned, the interceptor should in the following order:
    //  1. Queue up the unassigned targets in preparation of launching an additional
    //  sub-interceptor.
    //  2. If no existing sub-interceptor has been assigned to pursue the queued target(s), launch
    //  another sub-interceptor(s) to pursue the target(s).
    //  3. Propagate the target re-assignment to the parent interceptor above.
    if (CapacityPlannedRemaining <= 0) {
      OnReassignTarget?.Invoke(target);
      return;
    }

    _unassignedTargets.Add(target);
  }

  protected override void Start() {
    base.Start();
    _unassignedTargetsCoroutine =
        StartCoroutine(UnassignedTargetsManager(_unassignedTargetsLaunchPeriod));
    OnMiss += RegisterMiss;
  }

  protected override void FixedUpdate() {
    base.FixedUpdate();

    // Check whether the interceptor has a target. If not, request a new target from the parent
    // interceptor.
    if (HierarchicalAgent.Target == null || HierarchicalAgent.Target.IsTerminated) {
      RequestReassignment(this);
    }

    // Check whether any targets are escaping from the interceptor.
    if (EscapeDetector != null && HierarchicalAgent.Target != null &&
        !HierarchicalAgent.Target.IsTerminated) {
      List<IHierarchical> targetHierarchicals =
          HierarchicalAgent.Target.LeafHierarchicals(activeOnly: true, withTargetOnly: false);
      List<IHierarchical> escapingTargets =
          targetHierarchicals.Where(EscapeDetector.IsEscaping).ToList();
      foreach (var target in escapingTargets) {
        OnReassignTarget?.Invoke(target);
      }
      if (escapingTargets.Count == targetHierarchicals.Count) {
        RequestReassignment(this);
      }
    }

    // Update the planned number of sub-interceptors remaining.
    // TODO(titan): Update the planned number of sub-interceptors remaining when the number of leaf
    // hierarchical objects changes, such as when a new target is added.
    List<IHierarchical> leafHierarchicals =
        HierarchicalAgent.LeafHierarchicals(activeOnly: false, withTargetOnly: false);
    NumSubInterceptorsPlannedRemaining =
        Mathf.Min(NumSubInterceptorsRemaining, NumSubInterceptors - leafHierarchicals.Count);

    // Navigate towards the target.
    AccelerationInput = Controller?.Plan() ?? Vector3.zero;
    Acceleration = Movement?.Act(AccelerationInput) ?? Vector3.zero;
    _rigidbody.AddForce(Acceleration, ForceMode.Acceleration);
  }

  protected override void OnDestroy() {
    base.OnDestroy();

    if (_unassignedTargetsCoroutine != null) {
      StopCoroutine(_unassignedTargetsCoroutine);
      _unassignedTargetsCoroutine = null;
    }
  }

  protected override void UpdateAgentConfig() {
    base.UpdateAgentConfig();

    // Calculate the capacity.
    int NumAgents(Configs.AgentConfig config) {
      if (config == null || config.SubAgentConfig == null) {
        return 1;
      }
      return (int)config.SubAgentConfig.NumSubAgents * NumAgents(config.SubAgentConfig.AgentConfig);
    }
    Capacity = NumAgents(AgentConfig);
    CapacityPerSubInterceptor = NumAgents(AgentConfig.SubAgentConfig?.AgentConfig);
    NumSubInterceptors = (int)(AgentConfig.SubAgentConfig?.NumSubAgents ?? 0);
    NumSubInterceptorsPlannedRemaining = NumSubInterceptors;
    NumSubInterceptorsRemaining = NumSubInterceptors;

    // Set the controller.
    switch (AgentConfig.DynamicConfig?.FlightConfig?.ControllerType) {
      case Configs.ControllerType.ProportionalNavigation: {
        Controller = new PnController(this, _proportionalNavigationGain);
        break;
      }
      case Configs.ControllerType.AugmentedProportionalNavigation: {
        Controller = new ApnController(this, _proportionalNavigationGain);
        break;
      }
      default: {
        Debug.LogWarning(
            $"Controller type {AgentConfig.DynamicConfig?.FlightConfig?.ControllerType} not found.");
        Controller = null;
        break;
      }
    }
  }

  protected override void OnDrawGizmos() {
    const float axisLength = 10f;

    base.OnDrawGizmos();

    if (Application.isPlaying) {
      // Target.
      if (HierarchicalAgent.Target != null && !HierarchicalAgent.Target.IsTerminated) {
        Gizmos.color = new Color(1, 1, 1, 0.15f);
        Gizmos.DrawLine(Position, HierarchicalAgent.Target.Position);
      }

      // Up direction.
      Gizmos.color = Color.yellow;
      Gizmos.DrawRay(Position, Up * axisLength);

      // Forward direction.
      Gizmos.color = Color.blue;
      Gizmos.DrawRay(Position, Forward * axisLength);

      // Right direction.
      Gizmos.color = Color.red;
      Gizmos.DrawRay(Position, Right * axisLength);
    }
  }

  // If the interceptor collides with the ground or another agent, it will be terminated. It is
  // possible for an interceptor to collide with another interceptor or with a non-target threat.
  // The interceptor records a hit only if it collides with a threat and destroys it with the
  // threat's kill probability.
  private void OnTriggerEnter(Collider other) {
    if (CheckFloorCollision(other)) {
      OnMiss?.Invoke(this);
      Terminate();
    }

    IAgent otherAgent = other.gameObject.GetComponentInParent<IAgent>();
    if (ShouldIgnoreCollision(otherAgent)) {
      return;
    }
    // Check if the collision is with a threat.
    if (otherAgent is IThreat threat) {
      // Check the kill probability.
      float killProbability = threat.StaticConfig.HitConfig?.KillProbability ?? 1;
      bool isHit = Random.value <= killProbability;
      if (isHit) {
        threat.HandleIntercept();
        OnHit?.Invoke(this);
        Terminate();
      } else {
        OnMiss?.Invoke(this);
      }
    }
  }

  private void RegisterMiss(IInterceptor interceptor) {
    // Request the parent interceptor to re-assign the target to another interceptor if there are no
    // other pursuers.
    IHierarchical target = interceptor.HierarchicalAgent.Target;
    if (target == null || target.IsTerminated) {
      return;
    }
    List<IHierarchical> targetHierarchicals =
        target.LeafHierarchicals(activeOnly: true, withTargetOnly: false);
    foreach (var targetHierarchical in targetHierarchicals) {
      OnReassignTarget?.Invoke(targetHierarchical);
    }

    RequestReassignment(interceptor);
  }

  private void RequestReassignment(IInterceptor interceptor) {
    if (interceptor.IsReassignable) {
      // Request a new target from the parent interceptor.
      OnAssignSubInterceptor?.Invoke(interceptor);
    }
  }

  private IEnumerator UnassignedTargetsManager(float period) {
    while (true) {
      yield return new WaitUntil(() => _unassignedTargets.Count > 0);
      yield return new WaitForSeconds(period);

      IEnumerable<IHierarchical> unassignedTargets = _unassignedTargets.ToList();
      _unassignedTargets.Clear();

      // Check whether the unassigned targets are still unassigned or are escaping the assigned
      // pursuers.
      var filteredTargets =
          unassignedTargets
              .Where(target => !target.IsTerminated && target.ActivePursuers.All(pursuer => {
                var pursuerAgent = pursuer as HierarchicalAgent;
                var interceptor = pursuerAgent?.Agent as IInterceptor;
                return interceptor == null || interceptor.CapacityRemaining == 0 ||
                       (interceptor.EscapeDetector?.IsEscaping(target) ?? true);
              }))
              .ToList();
      if (filteredTargets.Count > CapacityPlannedRemaining) {
        // If there are more unassigned targets than the capacity remaining, propagate the target
        // re-assignment to the parent interceptor for the excess targets.
        var orderedTargets =
            filteredTargets.OrderBy(target => Vector3.Distance(Position, target.Position));
        var excessTargets = orderedTargets.Skip(CapacityPlannedRemaining);
        foreach (var target in excessTargets) {
          OnReassignTarget?.Invoke(target);
        }
        unassignedTargets = orderedTargets.Take(CapacityPlannedRemaining);
      } else {
        unassignedTargets = filteredTargets;
      }
      if (!unassignedTargets.Any()) {
        continue;
      }

      // Create a new hierarchical object with the cluster of unassigned targets as the target.
      var newTargetSubHierarchical = new HierarchicalBase();
      int numUnassignedTargets = 0;
      foreach (var target in unassignedTargets) {
        newTargetSubHierarchical.AddSubHierarchical(target);
        ++numUnassignedTargets;
      }
      var newSubHierarchical = new HierarchicalBase { Target = newTargetSubHierarchical };
      HierarchicalAgent.AddSubHierarchical(newSubHierarchical);
      Debug.Log($"Reclustered {numUnassignedTargets} target(s) into a new cluster for {this}.");
      UIManager.Instance.LogActionMessage(
          $"[IADS] Reclustered {numUnassignedTargets} target(s) into a new cluster for {this}.");

      // Recursively cluster the newly assigned targets.
      newSubHierarchical.RecursiveCluster(maxClusterSize: CapacityPerSubInterceptor);
    }
  }
}
