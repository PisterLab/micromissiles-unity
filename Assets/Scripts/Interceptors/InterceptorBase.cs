using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Base implementation of an interceptor.
public abstract class InterceptorBase : AgentBase, IInterceptor {
  public event Action<IInterceptor> OnHit;
  public event Action<IInterceptor> OnMiss;
  public event Action<IInterceptor> OnDestroyed;

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
  private CommsNode _assignTargetRequestReceiver;

  public bool EvaluateReassignedTarget(IHierarchical target) {
    // Continue searching for targets if no target was found.
    if (target == null) {
      return false;
    }

    // If the interceptor has no target, always accept the new target.
    if (HierarchicalAgent.Target == null || HierarchicalAgent.Target.IsTerminated) {
      HierarchicalAgent.Target = target;
      return true;
    }

    // Accept the new target if the intercept speed is higher.
    float currentFractionalSpeed =
        FractionalSpeed.Calculate(this, HierarchicalAgent.Target.Position);
    float newFractionalSpeed = FractionalSpeed.Calculate(this, target.Position);
    if (newFractionalSpeed > currentFractionalSpeed) {
      HierarchicalAgent.Target = target;
      return true;
    }
    return false;
  }

  public void AssignSubInterceptor(IInterceptor subInterceptor) {
    if (subInterceptor == null || subInterceptor.IsTerminated ||
        subInterceptor.CapacityRemaining <= 0) {
      return;
    }

    // Find a new target for the sub-interceptor within the parent interceptor's assigned targets.
    IHierarchical target = HierarchicalAgent.FindNewTarget(subInterceptor.HierarchicalAgent,
                                                           subInterceptor.CapacityRemaining);
    if (target != null) {
      SendAssignTargetResponse(subInterceptor, target);
      return;
    }
    SendAssignTargetRequest(subInterceptor);
  }

  private void SendAssignTargetRequest(IInterceptor subInterceptor) {
    if (subInterceptor == null || subInterceptor.IsTerminated) {
      return;
    }
    if (_assignTargetRequestReceiver == null) {
      // TODO(Joseph): Add failure visibility when an interceptor tries to route an assign-target
      // request without a configured parent receiver, since this likely indicates comms miswiring.
      return;
    }
    if (CommsNode == null) {
      // TODO(Joseph): Add failure visibility when an interceptor cannot send an assign-target
      // request because its CommsNode is missing.
      return;
    }
    Mailbox.GetOrCreateInstance().Send(
        new AssignTargetRequestMessage(CommsNode, _assignTargetRequestReceiver, subInterceptor));
  }

  private void SendAssignTargetResponse(IInterceptor subInterceptor, IHierarchical target) {
    if (subInterceptor?.CommsNode == null || target == null || CommsNode == null) {
      return;
    }
    Mailbox.GetOrCreateInstance().Send(
        new AssignTargetResponseMessage(CommsNode, subInterceptor.CommsNode, target));
  }

  public void SetAssignTargetRequestReceiver(CommsNode receiver) {
    _assignTargetRequestReceiver = receiver;
  }

  public void ReassignTarget(IHierarchical target) {
    // If a target needs to be re-assigned, the interceptor should in the following order:
    //  1. Queue up the unassigned targets in preparation of launching an additional
    //  sub-interceptor.
    //  2. If no existing sub-interceptor has been assigned to pursue the queued target(s), launch
    //  another sub-interceptor(s) to pursue the target(s).
    //  3. Propagate the target re-assignment to the parent interceptor above.
    if (CapacityPlannedRemaining <= 0) {
      SendReassignTargetRequest(target);
      return;
    }

    _unassignedTargets.Add(target);
  }

  protected override void Start() {
    base.Start();
    if (CommsNode != null) {
      CommsNode.OnMessageReceived += HandleMessageReceived;
    }
    _unassignedTargetsCoroutine =
        StartCoroutine(UnassignedTargetsManager(_unassignedTargetsLaunchPeriod));
    OnMiss += RegisterMiss;
    OnDestroyed += RegisterDestroyed;
  }

  private void HandleMessageReceived(Message message) {
    switch (message) {
      case AssignTargetRequestMessage assignTargetRequestMessage: {
        AssignSubInterceptor(assignTargetRequestMessage.PayloadData.SubInterceptor);
        break;
      }
      case AssignTargetResponseMessage assignTargetResponseMessage: {
        EvaluateReassignedTarget(assignTargetResponseMessage.PayloadData.Target);
        break;
      }
      case ReassignTargetRequestMessage reassignTargetRequestMessage: {
        ReassignTarget(reassignTargetRequestMessage.PayloadData.Target);
        break;
      }
    }
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
        SendReassignTargetRequest(target);
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

  private void RequestReassignment(IInterceptor interceptor) {
    if (interceptor.IsReassignable) {
      // Request a new target from the parent interceptor.
      SendAssignTargetRequest(interceptor);
    }
  }

  private void SendReassignTargetRequest(IHierarchical target) {
    if (target == null) {
      return;
    }
    if (_assignTargetRequestReceiver == null) {
      // TODO(Joseph): Add failure visibility when interceptor tries to propagate reassign-target
      // request without a configured parent receiver, indicating error.
      return;
    }
    if (CommsNode == null) {
      // TODO(Joseph): Add failure visibility when interceptor cannot send reassign-target
      // request because its CommsNode is missing.
      return;
    }
    Mailbox.GetOrCreateInstance().Send(
        new ReassignTargetRequestMessage(CommsNode, _assignTargetRequestReceiver, target));
  }

  protected override void OnDestroy() {
    if (CommsNode != null) {
      CommsNode.OnMessageReceived -= HandleMessageReceived;
    }
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
    switch (AgentConfig.DynamicConfig?.GuidanceConfig?.ControllerType) {
      case Configs.ControllerType.Static: {
        Controller = new StaticController(this);
        break;
      }
      case Configs.ControllerType.ProportionalNavigation: {
        Controller = new PnController(this, _proportionalNavigationGain);
        break;
      }
      case Configs.ControllerType.AugmentedProportionalNavigation: {
        Controller = new ApnController(this, _proportionalNavigationGain);
        break;
      }
      case Configs.ControllerType.Waypoint: {
        Controller = new WaypointController(this);
        break;
      }
      default: {
        Debug.LogWarning(
            $"Controller type {AgentConfig.DynamicConfig?.GuidanceConfig?.ControllerType} not found.");
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
    if (CheckGroundCollision(other)) {
      OnDestroyed?.Invoke(this);
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
      bool isHit = UnityEngine.Random.value <= killProbability;
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
    RequestTargetReassignment(interceptor);

    // Request a new target from the parent interceptor.
    SendAssignTargetRequest(interceptor);
  }

  private void RegisterDestroyed(IInterceptor interceptor) {
    RequestTargetReassignment(interceptor);
  }

  private void RequestTargetReassignment(IInterceptor interceptor) {
    // Request the parent interceptor to re-assign the target to another interceptor if there are no
    // other pursuers.
    IHierarchical target = interceptor.HierarchicalAgent.Target;
    if (target == null || target.IsTerminated) {
      return;
    }
    List<IHierarchical> targetHierarchicals =
        target.LeafHierarchicals(activeOnly: true, withTargetOnly: false);
    foreach (var targetHierarchical in targetHierarchicals) {
      SendReassignTargetRequest(targetHierarchical);
    }

    RequestReassignment(interceptor);
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
          SendReassignTargetRequest(target);
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
