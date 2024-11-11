using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interceptor : Agent {
  [SerializeField]
  private float _navigationGain = 3f;  // Typically 3-5

  [SerializeField]
  protected bool _showDebugVectors = true;

  private GameObject _missileTrailEffect;
  private bool _missileTrailEffectAttached = false;

  private Coroutine _returnParticleToManagerCoroutine;

  private SensorOutput _sensorOutput;
  private Vector3 _accelerationInput;

  private double _elapsedTime = 0;

  protected override void Awake() {
    base.Awake();
    SetFlightPhase(FlightPhase.INITIALIZED);
  }

  // Return whether a target can be assigned to the interceptor.
  public override bool IsAssignable() {
    bool assignable = !HasAssignedTarget();
    return assignable;
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

    // Calculate boost acceleration
    float boostAcceleration =
        (float)(staticAgentConfig.boostConfig.boostAcceleration * Constants.kGravity);
    Vector3 boostAccelerationVector = boostAcceleration * transform.forward;

    // Add PN acceleration to boost acceleration
    Vector3 pnAcceleration = CalculatePnAcceleration(deltaTime);
    Vector3 accelerationInput = boostAccelerationVector + pnAcceleration;

    // Calculate the total acceleration
    Vector3 acceleration = CalculateAcceleration(accelerationInput);

    // Apply the acceleration force
    GetComponent<Rigidbody>().AddForce(acceleration, ForceMode.Acceleration);
  }

  protected override void UpdateMidCourse(double deltaTime) {
    UpdateMissileTrailEffect();

    _elapsedTime += deltaTime;
    Vector3 accelerationInput = CalculatePnAcceleration(deltaTime);

    // Calculate and set the total acceleration
    Vector3 acceleration = CalculateAcceleration(accelerationInput);
    GetComponent<Rigidbody>().AddForce(acceleration, ForceMode.Acceleration);
  }

  private Vector3 CalculatePnAcceleration(double deltaTime) {
    if (!HasAssignedTarget()) {
      return Vector3.zero;
    }

    UpdateTargetModel(deltaTime);

    // Check whether the threat should be considered a miss
    SensorOutput sensorOutput = GetComponent<Sensor>().Sense(_target);
    // DL: This causes trouble with Fateh110B (high-speed threats)
    // if (sensorOutput.velocity.range > 1000f) {
    //   this.HandleInterceptMiss();
    //   return Vector3.zero;
    // }

    _sensorOutput = GetComponent<Sensor>().Sense(_targetModel);
    return CalculateAccelerationInput(_sensorOutput);
  }

  private void UpdateTargetModel(double deltaTime) {
    _elapsedTime += deltaTime;
    float sensorUpdatePeriod = 1f / dynamicAgentConfig.dynamic_config.sensor_config.frequency;
    if (_elapsedTime >= sensorUpdatePeriod) {
      // TODO: Implement guidance filter to estimate state from sensor output
      // For now, we'll use the threat's actual state
      _targetModel.SetPosition(_target.GetPosition());
      _targetModel.SetVelocity(_target.GetVelocity());
      _targetModel.SetAcceleration(_target.GetAcceleration());
      _elapsedTime = 0;
    }
  }

  private Vector3 CalculateAccelerationInput(SensorOutput sensorOutput) {
    IController controller;
    if (dynamicAgentConfig.dynamic_config.flight_config.augmentedPnEnabled) {
      controller = new ApnController(this, _navigationGain);
    } else {
      controller = new PnController(this, _navigationGain);
    }
    Vector3 accelerationInput = controller.Plan(sensorOutput);

    // Clamp the normal acceleration input to the maximum normal acceleration
    float maxNormalAcceleration = CalculateMaxNormalAcceleration();
    accelerationInput = Vector3.ClampMagnitude(accelerationInput, maxNormalAcceleration);
    _accelerationInput = accelerationInput;
    return accelerationInput;
  }

  private void OnTriggerEnter(Collider other) {
    if (other.gameObject.name == "Floor") {
      this.HandleInterceptMiss();
    }
    // Check if the collision is with another Agent
    Agent otherAgent = other.gameObject.GetComponentInParent<Agent>();
    if (otherAgent != null && otherAgent.GetComponent<Threat>() != null) {
      // Check kill probability before marking as hit
      float killProbability = otherAgent.staticAgentConfig.hitConfig.killProbability;
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
        ParticleSystem particleSystem = _missileTrailEffect.GetComponent<ParticleSystem>();
        float duration = particleSystem.main.duration;

        // Extend the duration of the missile trail effect to be the same as the boost time
        if (duration < staticAgentConfig.boostConfig.boostTime) {
          ParticleSystem.MainModule mainModule = particleSystem.main;
          mainModule.duration = staticAgentConfig.boostConfig.boostTime;
        }

        _returnParticleToManagerCoroutine = StartCoroutine(ReturnParticleToManager(duration * 2f));
        particleSystem.Play();
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

      if (_accelerationInput != null) {
        Debug.DrawRay(transform.position, _accelerationInput * 1f, Color.green);
      }
    }
  }
}
