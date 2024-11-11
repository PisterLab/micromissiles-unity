using System;
using UnityEngine;

// The controller class is an interface between the agent and its control law.
public class IController {
  // Agent that the controller is controlling.
  protected Agent _agent;

  public IController(Agent agent) {
    _agent = agent;
  }

  // Plan the next optimal control.
  public Vector3 Plan(SensorOutput sensorOutput) {
    // TODO(titan): The controller should instantiate an ideal sensor to sense the target model.
    // This sensor is distinct from the agent's sensor used to feed the guidance filter to create
    // the target model.
    return PlanImpl(sensorOutput);
  }

  // Controller-dependent implementation of the control law.
  protected virtual Vector3 PlanImpl(in SensorOutput sensorOutput) {
    return Vector3.zero;
  }
}
