using UnityEngine;

// Base implementation of an interceptor.
public abstract class InterceptorBase : AgentBase, IInterceptor {
  public event InterceptEventHandler OnHit;
  public event InterceptEventHandler OnMiss;

  // Default proportional navigation controller gain.
  private const float _proportionalNavigationGain = 5f;

  public virtual int Capacity { get; private set; }

  protected override void FixedUpdate() {
    base.FixedUpdate();

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
      HierarchicalAgent.HandleMiss();
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
        OnHit?.Invoke(this);
        threat.HandleIntercept();
        HierarchicalAgent.HandleHit();
      } else {
        OnMiss?.Invoke(this);
        HierarchicalAgent.HandleMiss();
      }
      Terminate();
    }
  }
}
