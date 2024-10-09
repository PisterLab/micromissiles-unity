using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interceptor : Agent {
  [SerializeField]
  protected bool _showDebugVectors = true;

  private GameObject _missileTrailEffect;
  private bool _missileTrailEffectAttached = false;

  private Coroutine _returnParticleToManagerCoroutine;

  // Return whether a target can be assigned to the interceptor.
  public override bool IsAssignable() {
    bool assignable = !HasAssignedTarget();
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
    GetComponent<Rigidbody>().AddForce(acceleration, ForceMode.Acceleration);
  }

  protected override void FixedUpdate() {
    base.FixedUpdate();
    if (_showDebugVectors) {
      DrawDebugVectors();
    }
  }

  protected override void UpdateBoost(double deltaTime) {
    if (_missileTrailEffect == null) {
      AttachMissileTrailEffect();
    }
    UpdateMissileTrailEffect();

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

  protected override void UpdateMidCourse(double deltaTime) {
    UpdateMissileTrailEffect();
  }

  private void OnTriggerEnter(Collider other) {
    if (other.gameObject.name == "Floor") {
      this.HandleInterceptMiss();
    }
    // Check if the collision is with another Agent
    Agent otherAgent = other.gameObject.GetComponentInParent<Agent>();
    if (otherAgent != null && otherAgent.GetComponent<Threat>() != null) {
      // Check kill probability before marking as hit
      float killProbability = otherAgent._staticAgentConfig.hitConfig.killProbability;
      GameObject markerObject = Instantiate(Resources.Load<GameObject>("Prefabs/HitMarkerPrefab"),
                                            transform.position, Quaternion.identity);
      if (Random.value <= killProbability) {
        markerObject.GetComponent<UIHitMarker>().SetHit();
        // Mark both this agent and the other agent as hit
        this.HandleInterceptHit(otherAgent);
        otherAgent.HandleInterceptHit(otherAgent);

      } else {
        markerObject.GetComponent<UIHitMarker>().SetMiss();
        this.HandleInterceptMiss();
        // otherAgent.MarkAsMiss();
      }
    }
  }

  public override void TerminateAgent() {
    DetatchMissileTrail();
    base.TerminateAgent();
  }

  public void OnDestroy() {
    if (_returnParticleToManagerCoroutine != null) {
      StopCoroutine(_returnParticleToManagerCoroutine);
    }
    if (_missileTrailEffect != null && ParticleManager.Instance != null) {
      ParticleManager.Instance.ReturnMissileTrailParticle(_missileTrailEffect);
      _missileTrailEffect = null;
    }
  }

  private void AttachMissileTrailEffect() {
    if (_missileTrailEffect == null) {
      _missileTrailEffect = ParticleManager.Instance.RequestMissileTrailParticle();
      if (_missileTrailEffect != null) {
        _missileTrailEffect.transform.parent = transform;
        _missileTrailEffect.transform.localPosition = Vector3.zero;
        _missileTrailEffectAttached = true;
        
        float duration = _missileTrailEffect.GetComponent<ParticleSystem>().main.duration;
        _returnParticleToManagerCoroutine = StartCoroutine(ReturnParticleToManager(duration * 2f));
        _missileTrailEffect.GetComponent<ParticleSystem>().Play();
      }
    }
  }

  private IEnumerator ReturnParticleToManager(float delay) {
    yield return new WaitForSeconds(delay);
    if (_missileTrailEffect != null) {
      ParticleManager.Instance.ReturnMissileTrailParticle(_missileTrailEffect);
      _missileTrailEffect = null;
      _missileTrailEffectAttached = false;
    }
  }

  private void UpdateMissileTrailEffect() {
    if (_missileTrailEffect == null || !_missileTrailEffectAttached) {
      return;
    }

    // Get the particle effect duration time
    float duration = _missileTrailEffect.GetComponent<ParticleSystem>().main.duration;
    if (_timeSinceBoost > duration) {
      DetatchMissileTrail();
    }
  }

  private void DetatchMissileTrail() {
    if (_missileTrailEffect != null && _missileTrailEffectAttached) {
      Vector3 currentPosition = _missileTrailEffect.transform.position;
      _missileTrailEffect.transform.SetParent(null);
      _missileTrailEffect.transform.position = currentPosition;
      _missileTrailEffectAttached = false;
      // Stop emitting particles
      ParticleSystem particleSystem = _missileTrailEffect.GetComponent<ParticleSystem>();
      particleSystem.Stop();
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
