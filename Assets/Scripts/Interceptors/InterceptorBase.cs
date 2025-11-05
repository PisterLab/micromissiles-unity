using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Base implementation of an interceptor.
public abstract class InterceptorBase : AgentBase, IInterceptor {
  public event InterceptorEventHandler OnHit;
  public event InterceptorEventHandler OnMiss;
  public event InterceptorEventHandler OnAssignSubInterceptor;
  public event TargetEventHandler OnReassignTarget;

  // Default proportional navigation controller gain.
  private const float _proportionalNavigationGain = 5f;

  // Time to accumulate unassigned targets before launching additional sub-interceptors.
  private const float _unassignedTargetsLaunchPeriod = 2.5f;

  public IEscapeDetector EscapeDetector { get; set; }

  public int Capacity { get; protected set; }
  public int CapacityPerSubInterceptor { get; protected set; }
  public virtual int CapacityPlannedRemaining => CapacityPerSubInterceptor *
                                                 NumSubInterceptorsPlannedRemaining;
  public virtual int CapacityRemaining => CapacityPerSubInterceptor * NumSubInterceptorsRemaining;
  public int NumSubInterceptors { get; protected set; }
  public int NumSubInterceptorsPlannedRemaining { get; protected set; }
  public int NumSubInterceptorsRemaining { get; protected set; }

  // List of unassigned targets for which an additional sub-interceptor should be launched.
  private List<IHierarchical> _unassignedTargets = new List<IHierarchical>();

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

    if (!_unassignedTargets.Contains(target)) {
      _unassignedTargets.Add(target);
    }
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
      OnAssignSubInterceptor?.Invoke(this);
    }

    // Check whether any targets are escaping from the interceptor.
    if (EscapeDetector != null && HierarchicalAgent.Target != null &&
        !HierarchicalAgent.Target.IsTerminated) {
      List<IHierarchical> targetHierarchicals =
          HierarchicalAgent.Target.LeafHierarchicals(activeOnly: true, withTargetOnly: false);
      int numEscapingTargets = 0;
      foreach (var targetHierarchical in targetHierarchicals) {
        if (EscapeDetector.IsEscaping(targetHierarchical)) {
          OnReassignTarget?.Invoke(targetHierarchical);
          ++numEscapingTargets;
        }
      }
      if (numEscapingTargets == targetHierarchicals.Count) {
        OnAssignSubInterceptor?.Invoke(this);
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
    _accelerationInput = Controller?.Plan() ?? Vector3.zero;
    _acceleration = Movement?.Act(_accelerationInput) ?? Vector3.zero;
    _rigidbody.AddForce(_acceleration, ForceMode.Acceleration);
  }

  protected override void OnDestroy() {
    base.OnDestroy();

    if (_unassignedTargetsCoroutine != null) {
      StopCoroutine(_unassignedTargetsCoroutine);
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

      // Forward direction.
      Gizmos.color = Color.blue;
      Gizmos.DrawRay(Position, transform.forward * axisLength);

      // Right direction.
      Gizmos.color = Color.red;
      Gizmos.DrawRay(Position, transform.right * axisLength);

      // Upwards direction.
      Gizmos.color = Color.yellow;
      Gizmos.DrawRay(Position, transform.up * axisLength);
    }
  }

  // If the interceptor collides with the ground or another agent, it will be terminated. It is
  // possible for an interceptor to collide with another interceptor or with a non-target threat.
  // The interceptor records a hit only if it collides with a threat and destroys it with the
  // threat's kill probability.
  private void OnTriggerEnter(Collider other) {
    // Check if the interceptor hit the floor with a negative vertical speed.
    if (other.gameObject.name == "Floor" && Vector3.Dot(Velocity, Vector3.up) < 0) {
      OnMiss?.Invoke(this);
      Terminate();
    }

    IAgent otherAgent = other.gameObject.GetComponentInParent<IAgent>();
    // Dummy agents are virtual targets and should not trigger collisions.
    if (otherAgent == null || otherAgent is DummyAgent) {
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
      } else {
        OnMiss?.Invoke(this);
      }
      Terminate();
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

    // Request a new target from the parent interceptor.
    OnAssignSubInterceptor?.Invoke(interceptor);
  }

  private IEnumerator UnassignedTargetsManager(float period) {
    while (true) {
      yield return new WaitUntil(() => _unassignedTargets.Count > 0);
      yield return new WaitForSeconds(period);

      IEnumerable<IHierarchical> unassignedTargets = _unassignedTargets.ToList();
      _unassignedTargets.Clear();

      // Check whether the unassigned targets are still unassigned or are escaping the assigned
      // pursuers.
      unassignedTargets = unassignedTargets.Where(
          target => !target.IsTerminated && target.ActivePursuers.All(pursuer => {
            var pursuerAgent = pursuer as HierarchicalAgent;
            var interceptor = pursuerAgent?.Agent as IInterceptor;
            return interceptor?.EscapeDetector?.IsEscaping(target) ?? true;
          }));
      if (unassignedTargets.Count() > CapacityPlannedRemaining) {
        // If there are more unassigned targets than the capacity remaining, propagate the target
        // re-assignment to the parent interceptor for the excess targets.
        unassignedTargets =
            unassignedTargets.OrderBy(target => Vector3.Distance(Position, target.Position));
        var excessTargets = unassignedTargets.Skip(CapacityPlannedRemaining);
        foreach (var target in excessTargets) {
          OnReassignTarget?.Invoke(target);
        }
        unassignedTargets = unassignedTargets.Take(CapacityPlannedRemaining);
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
