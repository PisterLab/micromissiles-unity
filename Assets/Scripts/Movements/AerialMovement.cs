using UnityEngine;

// Aerial movement.
//
// Interceptors and threats have different movements since threats are not affected by drag and
// gravity and have specified attack behaviors.
public abstract class AerialMovement : MovementBase {
  public AerialMovement(IAgent agent) : base(agent) {}

  // Calculate the dynamic pressure on the agent.
  protected float GetDynamicPressure() {
    float airDensity = Constants.CalculateAirDensityAtAltitude(Agent.Position.y);
    float flowSpeed = Agent.Speed;
    return 0.5f * airDensity * (flowSpeed * flowSpeed);
  }

  // Calculate the air drag acting on the agent.
  protected float CalculateDrag() {
    var staticConfig = Agent.StaticConfig;
    float dragCoefficient = staticConfig.LiftDragConfig?.DragCoefficient ?? 0;
    float crossSectionalArea = staticConfig.BodyConfig?.CrossSectionalArea ?? 0;
    float mass = staticConfig.BodyConfig?.Mass ?? 1;
    float dynamicPressure = GetDynamicPressure();
    float dragForce = dragCoefficient * dynamicPressure * crossSectionalArea;
    return dragForce / mass;
  }

  // Calculate the lift-induced drag acting on the agent. Since the agent is flying, any
  // acceleration normal to the velocity vector is considered "lift".
  protected float CalculateLiftInducedDrag(in Vector3 accelerationInput) {
    var staticConfig = Agent.StaticConfig;
    float liftAcceleration =
        Vector3.ProjectOnPlane(accelerationInput, Agent.transform.forward).magnitude;
    float liftDragRatio = staticConfig.LiftDragConfig?.LiftDragRatio ?? 1;
    return Mathf.Abs(liftAcceleration / liftDragRatio);
  }
}
