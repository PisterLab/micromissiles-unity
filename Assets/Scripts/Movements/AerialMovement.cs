using UnityEngine;

// Aerial movement.
//
// Interceptors and threats have different movements since threats are not affected by drag and
// gravity and have specified attack behaviors.
public abstract class AerialMovement : MovementBase {
  public AerialMovement(IAgent agent) : base(agent) {}

  // TODO(titan): Methods to calculate gravity and drag based on the agent as well as the total
  // acceleration.

  protected float GetDynamicPressure() {
    var airDensity = (float)Constants.CalculateAirDensityAtAltitude(Agent.Position.y);
    var flowSpeed = Agent.Speed;
    return 0.5f * airDensity * (flowSpeed * flowSpeed);
  }

  protected float CalculateDrag() {
    var staticConfig = Agent.StaticConfig;
    var dragCoefficient = staticConfig.LiftDragConfig?.DragCoefficient ?? 0;
    var crossSectionalArea = staticConfig.BodyConfig?.CrossSectionalArea ?? 0;
    var mass = staticConfig.BodyConfig?.Mass ?? 1;
    var dynamicPressure = GetDynamicPressure();
    var dragForce = dragCoefficient * dynamicPressure * crossSectionalArea;
    return dragForce / mass;
  }

  protected float CalculateLiftInducedDrag(in Vector3 accelerationInput) {
    var staticConfig = Agent.StaticConfig;
    var liftAcceleration = Vector3.ProjectOnPlane(accelerationInput, transform.forward).magnitude;
    var liftDragRatio = staticConfig.LiftDragConfig?.LiftDragRatio ?? 1;
    return Mathf.Abs(liftAcceleration / liftDragRatio);
  }
}
