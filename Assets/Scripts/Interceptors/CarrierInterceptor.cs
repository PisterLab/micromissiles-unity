using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using System.Linq;

public class CarrierInterceptor : Interceptor {
  private bool _submunitionsLaunched = false;

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
    for (int i = 0; i < agentConfig.SubmunitionsConfig.NumSubmunitions; ++i) {
      Configs.AgentConfig submunitionsConfig = agentConfig.SubmunitionsConfig.AgentConfig;
      Simulation.State initialState = new Simulation.State();
      initialState.Position = Coordinates3.ToProto(transform.position);

      // Fan the submunitions radially outwards by 60 degrees from the carrier interceptor's
      // velocity vector.
      const float SubmunitionsAngularDeviation = 60.0f * Mathf.Deg2Rad;
      Vector3 velocity = GetComponent<Rigidbody>().linearVelocity;
      Vector3 perpendicularDirection = Vector3.Cross(velocity, Vector3.up);
      Vector3 lateralDirection =
          Quaternion.AngleAxis(i * 360 / agentConfig.SubmunitionsConfig.NumSubmunitions, velocity) *
          perpendicularDirection;
      initialState.Velocity = Coordinates3.ToProto(Vector3.RotateTowards(
          velocity, lateralDirection, maxRadiansDelta: SubmunitionsAngularDeviation,
          maxMagnitudeDelta: Mathf.Cos(SubmunitionsAngularDeviation)));

      Interceptor submunition =
          SimManager.Instance.CreateInterceptor(submunitionsConfig, initialState);
      submunition.SetFlightPhase(FlightPhase.READY);
      // Launch the submunitions with the same velocity as the carrier interceptor's.
      submunition.SetVelocity(GetComponent<Rigidbody>().linearVelocity);
      submunitions.Add(submunition);
    }
    UnassignTarget();
    IADS.Instance.AssignSubmunitionsToThreats(this, submunitions);

    SimManager.Instance.AddSubmunitionsSwarm(
        submunitions.ConvertAll(submunition => submunition as Agent));
  }
}
