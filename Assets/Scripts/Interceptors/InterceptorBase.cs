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

  public int Capacity { get; protected set; }
  public int CapacityPerSubInterceptor { get; protected set; }
  public virtual int CapacityRemaining => CapacityPerSubInterceptor * NumSubInterceptorsRemaining;
  public int NumSubInterceptorsRemaining { get; protected set; }

  public void AssignSubInterceptor(IInterceptor subInterceptor) {
    // Assign a new target to the sub-interceptor within the parent interceptor's assigned targets.
    // TODO(titan): The capacity remaining should be used instead.
    if (!HierarchicalAgent.AssignNewTarget(subInterceptor.HierarchicalAgent,
                                           subInterceptor.Capacity)) {
      // Propagate the sub-interceptor target assignment to the parent interceptor above.
      OnAssignSubInterceptor?.Invoke(subInterceptor);
    }
  }

  public void ReassignTarget(IHierarchical target) {
    // If a target needs to be re-assigned, the interceptor should in the following order:
    //  1. Re-assign the target to another sub-interceptor without leaving another target uncovered.
    //  2. Launch another sub-interceptor to pursue the target.
    //  3. Propagate the target re-assignment to the parent interceptor above.
    if (!HierarchicalAgent.ReassignTarget(target)) {
      // Check if it is possible to launch another sub-interceptor.
      // Otherwise, propagate the target re-assignment to the parent interceptor above.
      OnReassignTarget?.Invoke(target);
    }
  }

  protected override void Start() {
    base.Start();

    OnMiss += RegisterMiss;
  }

  protected override void FixedUpdate() {
    base.FixedUpdate();

    // Check whether the interceptor has a target. If not, request a new target from the parent
    // interceptor.
    if (HierarchicalAgent.Target == null || HierarchicalAgent.Target.IsTerminated) {
      OnAssignSubInterceptor?.Invoke(this);
    }

    // Navigate towards the target.
    _accelerationInput = Controller?.Plan() ?? Vector3.zero;
    _acceleration = Movement?.Act(_accelerationInput) ?? Vector3.zero;
    _rigidbody.AddForce(_acceleration, ForceMode.Acceleration);
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
    NumSubInterceptorsRemaining = (int)(AgentConfig.SubAgentConfig?.NumSubAgents ?? 0);

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

  private void RegisterMiss(IInterceptor interceptor) {
    // Request the parent interceptor to re-assign the target to another interceptor if there are no
    // other pursuers.
    IHierarchical target = interceptor.HierarchicalAgent.Target;
    if (target != null) {
      bool targetIsCovered =
          target.Pursuers.Any(pursuer => pursuer != interceptor.HierarchicalAgent);
      if (!targetIsCovered) {
        OnReassignTarget?.Invoke(target);
      }
    }
    // Request a new target from the parent interceptor.
    OnAssignSubInterceptor?.Invoke(interceptor);
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
}
