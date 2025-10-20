using UnityEngine;

// Base implementation of an interceptor.
public abstract class InterceptorBase : AgentBase, IInterceptor {
  // Default proportional navigation controller gain.
  private const float _proportionalNavigationGain = 5f;

  public virtual int Capacity { get; private set; }

  protected override void Awake() {
    base.Awake();
  }

  protected override void FixedUpdate() {
    base.FixedUpdate();

    // Navigate towards the target.
    var accelerationInput = Controller?.Plan() ?? Vector3.zero;
    var acceleration = Movement?.Act(accelerationInput) ?? Vector3.zero;
    _rigidbody.AddForce(acceleration, ForceMode.Acceleration);
  }

  protected virtual void UpdateAgentConfig() {
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
        Debug.LogError(
            $"Controller type {AgentConfig.DynamicConfig?.FlightConfig?.ControllerType} not found.");
        break;
      }
    }
  }
}
