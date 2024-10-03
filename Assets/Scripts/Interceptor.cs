using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interceptor : Agent {
  [SerializeField]
  protected bool _showDebugVectors = true;

  // Return whether a target can be assigned to the interceptor.
  public override bool IsAssignable() {
    bool assignable = !HasLaunched() && !HasAssignedTarget();
    return assignable;
  }

  // Assign the given target to the interceptor.
  public override void AssignTarget(Agent target) {
    base.AssignTarget(target);
  }

  // Unassign the target from the interceptor.
  public override void UnassignTarget() {
    base.UnassignTarget();
  }

  protected override void UpdateReady(double deltaTime) {
    Vector3 accelerationInput = Vector3.zero;
    Vector3 acceleration = CalculateAcceleration(accelerationInput);
    // GetComponent<Rigidbody>().AddForce(acceleration, ForceMode.Acceleration);
  }

  protected override void FixedUpdate() {
    base.FixedUpdate();
    if (_showDebugVectors) {
      DrawDebugVectors();
    }
  }

  protected override void UpdateBoost(double deltaTime) {
    // The interceptor only accelerates along its roll axis (forward in Unity)
    Vector3 rollAxis = transform.forward;

    // Calculate boost acceleration
    float boostAcceleration =
        (float)(_staticAgentConfig.boostConfig.boostAcceleration * Constants.kGravity);
    Vector3 accelerationInput = boostAcceleration * rollAxis;

    // Calculate the total acceleration
    Vector3 acceleration = CalculateAcceleration(accelerationInput);

    // Apply the acceleration force
    GetComponent<Rigidbody>().AddForce(acceleration, ForceMode.Acceleration);
  }

  protected override void UpdateMidCourse(double deltaTime) {}



  private void OnTriggerEnter(Collider other) {
    if (other.gameObject.name == "Floor") {
      this.HandleInterceptMiss();
    }
    // Check if the collision is with another Agent
    Agent otherAgent = other.gameObject.GetComponentInParent<Agent>();
    if (otherAgent != null && otherAgent.GetComponent<Threat>() != null) {
      // Check kill probability before marking as hit
      float killProbability = _staticAgentConfig.hitConfig.killProbability;
      GameObject markerObject = Instantiate(Resources.Load<GameObject>("Prefabs/HitMarkerPrefab"),
                                            transform.position, Quaternion.identity);
      if (Random.value <= killProbability) {
        // Set green for hit
        markerObject.GetComponent<Renderer>().material.color = new Color(0, 1, 0, 0.15f);
        // Mark both this agent and the other agent as hit
        this.HandleInterceptHit(otherAgent);
        otherAgent.HandleInterceptHit(otherAgent);

      } else {
        // Set red for miss
        markerObject.GetComponent<Renderer>().material.color = new Color(1, 0, 0, 0.15f);
        this.HandleInterceptMiss();
        // otherAgent.MarkAsMiss();
      }
    }
  }



  protected virtual void DrawDebugVectors() {
    if (_target != null) {
      // Line of sight
      Debug.DrawLine(transform.position, _target.transform.position, new Color(1, 1, 1, 0.15f));

      // Velocity vector
      Debug.DrawRay(transform.position, GetVelocity() * 0.01f, new Color(0, 0, 1, 0.15f));

      // Current forward direction
      Debug.DrawRay(transform.position, transform.forward * 5f, Color.yellow);

      // Pitch axis (right)
      Debug.DrawRay(transform.position, transform.right * 5f, Color.red);

      // Yaw axis (up)
      Debug.DrawRay(transform.position, transform.up * 5f, Color.magenta);
    }
  }
}
