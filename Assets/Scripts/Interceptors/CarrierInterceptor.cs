using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class CarrierInterceptor : Interceptor {
  private bool _submunitionsLaunched = false;

  protected override void FixedUpdate() {
    base.FixedUpdate();

    // Check if it's time to launch submunitions
    if (!_submunitionsLaunched &&
        (GetFlightPhase() == FlightPhase.MIDCOURSE || GetFlightPhase() == FlightPhase.BOOST) &&
        SimManager.Instance.GetElapsedSimulationTime() >=
            _dynamicAgentConfig.submunitions_config.launch_config.launch_time) {
      SpawnSubmunitions();
      _submunitionsLaunched = true;
    }
  }

  protected override void UpdateMidCourse(double deltaTime) {
    Vector3 accelerationInput = Vector3.zero;
    // Calculate and set the total acceleration
    Vector3 acceleration = CalculateAcceleration(accelerationInput);
    GetComponent<Rigidbody>().AddForce(acceleration, ForceMode.Acceleration);
  }

  protected override void DrawDebugVectors() {
    base.DrawDebugVectors();
    if (_acceleration != null) {
      Debug.DrawRay(transform.position, _acceleration * 1f, Color.green);
    }
  }

  public void SpawnSubmunitions() {
    List<Interceptor> submunitions = new List<Interceptor>();
    for (int i = 0; i < _dynamicAgentConfig.submunitions_config.num_submunitions; i++) {
      DynamicAgentConfig convertedConfig = DynamicAgentConfig.FromSubmunitionDynamicAgentConfig(
          _dynamicAgentConfig.submunitions_config.dynamic_agent_config);
      convertedConfig.initial_state.position = transform.position;
      convertedConfig.initial_state.velocity = GetComponent<Rigidbody>().linearVelocity;
      Interceptor submunition = SimManager.Instance.CreateInterceptor(convertedConfig);
      submunition.SetFlightPhase(FlightPhase.READY);
      submunitions.Add(submunition);
    }
    IADS.Instance.RequestThreatAssignment(submunitions);
  }
}
