using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class CarrierInterceptor : Interceptor {
  private bool _submunitionsLaunched = false;

  public override void SetDynamicAgentConfig(DynamicAgentConfig config) {
    base.SetDynamicAgentConfig(config);
    if (!HasAssignedTarget()) {
      // Create a DummyTarget that is projected 100km out from the initialRotation
      Vector3 initialPosition = config.initial_state.position;
      Quaternion initialRotation = Quaternion.Euler(config.initial_state.rotation);
      Vector3 forwardDirection = initialRotation * Vector3.forward;
      Vector3 dummyTargetPosition =
          initialPosition + forwardDirection * 100000f;  // 100km in meters

      // Calculate a reasonable velocity for the dummy target
      Vector3 dummyTargetVelocity = Vector3.zero;  // Assuming 1000 m/s speed

      // Create the dummy agent using SimManager
      Agent dummyAgent =
          SimManager.Instance.CreateDummyAgent(dummyTargetPosition, dummyTargetVelocity);

      // Assign the dummy agent as the target
      AssignTarget(dummyAgent);
    }
  }

  public override bool IsAssignable() {
    return false;
  }

  protected override void FixedUpdate() {
    base.FixedUpdate();
    float launchTimeVariance = 0.5f;
    float launchTimeNoise = Random.Range(-launchTimeVariance, launchTimeVariance);
    float launchTimeWithNoise =
        dynamicAgentConfig.submunitions_config.launch_config.launch_time + launchTimeNoise;
    // Check if it's time to launch submunitions
    if (!_submunitionsLaunched &&
        (GetFlightPhase() == FlightPhase.MIDCOURSE || GetFlightPhase() == FlightPhase.BOOST) &&
        SimManager.Instance.GetElapsedSimulationTime() >= launchTimeWithNoise) {
      SpawnSubmunitions();
      _submunitionsLaunched = true;
    }
  }

  protected override void UpdateMidCourse(double deltaTime) {
    base.UpdateMidCourse(deltaTime);
  }

  protected override void DrawDebugVectors() {
    base.DrawDebugVectors();
    if (_acceleration != null) {
      Debug.DrawRay(transform.position, _acceleration * 1f, Color.green);
    }
  }

  public void SpawnSubmunitions() {
    List<Interceptor> submunitions = new List<Interceptor>();
    for (int i = 0; i < dynamicAgentConfig.submunitions_config.num_submunitions; i++) {
      DynamicAgentConfig convertedConfig = DynamicAgentConfig.FromSubmunitionDynamicAgentConfig(
          dynamicAgentConfig.submunitions_config.dynamic_agent_config);
      convertedConfig.initial_state.position = transform.position;
      convertedConfig.initial_state.velocity = GetComponent<Rigidbody>().linearVelocity;
      Interceptor submunition = SimManager.Instance.CreateInterceptor(convertedConfig);
      submunition.SetFlightPhase(FlightPhase.READY);
      submunitions.Add(submunition);
    }
    IADS.Instance.RequestThreatAssignment(submunitions);
  }
}
