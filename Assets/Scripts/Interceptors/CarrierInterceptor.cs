using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using System.Linq;

public class CarrierInterceptor : Interceptor {
  private bool _submunitionsLaunched = false;

  public override void SetDynamicAgentConfig(DynamicAgentConfig config) {
    base.SetDynamicAgentConfig(config);
    if (!HasAssignedTarget()) {
      const float DummyTargetPosition = 100 * 1000f;
      // Create a dummy target that is projected 100 km out from the initial rotation.
      Vector3 initialPosition = config.initial_state.position;
      Quaternion initialRotation = Quaternion.Euler(config.initial_state.rotation);
      Vector3 forwardDirection = initialRotation * Vector3.forward;
      Vector3 dummyTargetPosition = initialPosition + forwardDirection * DummyTargetPosition;
      Vector3 dummyTargetVelocity = Vector3.zero;

      // Create the dummy agent.
      Agent dummyAgent =
          SimManager.Instance.CreateDummyAgent(dummyTargetPosition, dummyTargetVelocity);

      // Assign the dummy agent as the target.
      AssignTarget(dummyAgent);
    }
  }

  public override bool IsAssignable() {
    return false;
  }

  protected override void FixedUpdate() {
    base.FixedUpdate();
    // Check whether to launch submunitions.
    if ((GetFlightPhase() == FlightPhase.MIDCOURSE || GetFlightPhase() == FlightPhase.BOOST) &&
        !_submunitionsLaunched && IADS.Instance.ShouldLaunchSubmunitions(this)) {
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

  private void SpawnSubmunitions() {
    List<Interceptor> submunitions = new List<Interceptor>();
    for (int i = 0; i < dynamicAgentConfig.submunitions_config.num_submunitions; i++) {
      DynamicAgentConfig convertedConfig = DynamicAgentConfig.FromSubmunitionDynamicAgentConfig(
          dynamicAgentConfig.submunitions_config.dynamic_agent_config);
      convertedConfig.initial_state = new InitialState();
      convertedConfig.initial_state.position = transform.position;

      // Fan the submunitions radially outwards by 60 degrees from the carrier interceptor's
      // velocity vector.
      const float SubmunitionsAngularDeviation = 60.0f * Mathf.Deg2Rad;
      Vector3 velocity = GetComponent<Rigidbody>().linearVelocity;
      Vector3 perpendicularDirection = Vector3.Cross(velocity, Vector3.up);
      Vector3 lateralDirection =
          Quaternion.AngleAxis(i * 360 / dynamicAgentConfig.submunitions_config.num_submunitions,
                               velocity) *
          perpendicularDirection;
      convertedConfig.initial_state.velocity = Vector3.RotateTowards(
          velocity, lateralDirection, maxRadiansDelta: SubmunitionsAngularDeviation,
          maxMagnitudeDelta: Mathf.Cos(SubmunitionsAngularDeviation));

      Interceptor submunition = SimManager.Instance.CreateInterceptor(convertedConfig);
      submunition.SetFlightPhase(FlightPhase.READY);
      // Launch the submunitions with the same velocity as the carrier interceptor's.
      submunition.SetVelocity(GetComponent<Rigidbody>().linearVelocity);
      submunitions.Add(submunition);
    }
    IADS.Instance.AssignSubmunitionsToThreats(this, submunitions);
    UnassignTarget();

    SimManager.Instance.AddSubmunitionsSwarm(
        submunitions.ConvertAll(submunition => submunition as Agent));
  }
}
