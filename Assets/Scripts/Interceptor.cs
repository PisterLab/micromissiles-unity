using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interceptor : AerialAgent {
  [SerializeField]
  private float _navigationGain = 3f;  // Typically 3-5.

  [SerializeField]
  protected bool _showDebugVectors = true;

  private GameObject _missileTrailEffect;
  private bool _missileTrailEffectAttached = false;

  private Coroutine _returnParticleToManagerCoroutine;

  private Vector3 _accelerationInput;

  private double _elapsedTime = 0;

  protected override void Awake() {
    base.Awake();
    SetFlightPhase(FlightPhase.INITIALIZED);
  }

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

    // Calculate the boost acceleration.
    float boostAcceleration =
        (float)(staticConfig.BoostConfig.BoostAcceleration * Constants.kGravity);
    Vector3 boostAccelerationVector = boostAcceleration * transform.forward;

    // Add the PN acceleration to the boost acceleration.
    Vector3 controllerAcceleration = CalculateAccelerationInput(deltaTime);
    Vector3 accelerationInput = boostAccelerationVector + controllerAcceleration;

    // Calculate the total acceleration.
    Vector3 acceleration = CalculateAcceleration(accelerationInput);

    // Apply the acceleration.
    GetComponent<Rigidbody>().AddForce(acceleration, ForceMode.Acceleration);
  }

  protected override void UpdateMidCourse(double deltaTime) {
    UpdateMissileTrailEffect();

    _elapsedTime += deltaTime;
    Vector3 accelerationInput = CalculateAccelerationInput(deltaTime);

    // Calculate and set the total acceleration.
    Vector3 acceleration = CalculateAcceleration(accelerationInput);
    GetComponent<Rigidbody>().AddForce(acceleration, ForceMode.Acceleration);
  }

  private Vector3 CalculateAccelerationInput(double deltaTime) {
    Vector3 accelerationInput = Vector3.zero;
    float maxNormalAcceleration = CalculateMaxNormalAcceleration();

    if (!HasAssignedTarget()) {
      // No assigned target: do not generate control input.
      // Gravity and drag are applied centrally in AerialAgent.CalculateAcceleration,
      // so returning zero here ensures idle missiles/carriers still experience gravity
      // and do not artificially hover.
      _accelerationInput = Vector3.zero;
      return _accelerationInput;
    }

    UpdateTargetModel(deltaTime);

    // Check whether the threat should be considered a miss.
    SensorOutput sensorOutput = GetComponent<Sensor>().Sense(_target);
    // TODO(dlovell): This causes trouble with the Fateh 110B (high-speed threats).
    // if (sensorOutput.velocity.range > 1000f) {
    //   HandleInterceptMiss();
    //   return Vector3.zero;
    // }

    IController controller;
    if (dynamicAgentConfig.dynamic_config.flight_config.augmentedPnEnabled) {
      controller = new ApnController(this, _navigationGain);
    } else {
      controller = new PnController(this, _navigationGain);
    }
    accelerationInput = controller.Plan();

    // Clamp the normal acceleration input to the maximum normal acceleration.
    accelerationInput = Vector3.ClampMagnitude(accelerationInput, maxNormalAcceleration);
    _accelerationInput = accelerationInput;
    return accelerationInput;
  }

  private void UpdateTargetModel(double deltaTime) {
    // Skip if target/model/sensor is unavailable. This prevents null dereferences
    // during phases before assignment or when configs are incomplete.
    if (_targetModel == null || _target == null) {
      return;
    }

    var sensorConfig = dynamicAgentConfig?.dynamic_config?.sensor_config;
    if (sensorConfig == null || sensorConfig.frequency <= 0f) {
      return;
    }

    _elapsedTime += deltaTime;
    float sensorUpdatePeriod = 1f / sensorConfig.frequency;
    if (_elapsedTime >= sensorUpdatePeriod) {
      // TODO: Implement guidance filter to estimate state from the sensor output.
      // For now, we'll use the threat's actual state.
      _targetModel.SetPosition(_target.GetPosition());
      _targetModel.SetVelocity(_target.GetVelocity());
      _targetModel.SetAcceleration(_target.GetAcceleration());
      _elapsedTime = 0;
    }
  }

  private void OnTriggerEnter(Collider other) {
    // Check if the interceptor hit the floor with a negative vertical speed.
    if (other.gameObject.name == "Floor" && Vector3.Dot(GetVelocity(), Vector3.up) < 0) {
      HandleHitGround();
    }

    // Check if the collision is with another agent.
    Agent otherAgent = other.gameObject.GetComponentInParent<Agent>();
    if (otherAgent != null && otherAgent.GetComponent<Threat>() != null &&
        _target == otherAgent as Threat) {
      // Check kill probability before marking as hit.
      float killProbability = otherAgent.staticConfig.HitConfig.KillProbability;

      if (Random.value <= killProbability) {
        // Mark both this agent and the other agent as hit.
        HandleInterceptHit(otherAgent);
        otherAgent.HandleTargetIntercepted();
      } else {
        HandleInterceptMiss();
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

        // Extend the duration of the missile trail effect to be the same as the boost time.
        if (duration < staticConfig.BoostConfig.BoostTime) {
          ParticleSystem.MainModule mainModule = particleSystem.main;
          mainModule.duration = staticConfig.BoostConfig.BoostTime;
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

    // Get the particle effect duration time.
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
      // Stop emitting particles.
      ParticleSystem particleSystem = _missileTrailEffect.GetComponent<ParticleSystem>();
      particleSystem.Stop();
    }
  }

  protected virtual void DrawDebugVectors() {
    if (_target != null) {
      // Line of sight.
      Debug.DrawLine(transform.position, _target.transform.position, new Color(1, 1, 1, 0.15f));

      // Velocity vector.
      Debug.DrawRay(transform.position, GetVelocity() * 0.01f, new Color(0, 0, 1, 0.15f));

      // Current forward direction.
      Debug.DrawRay(transform.position, transform.forward * 5f, Color.yellow);

      // Pitch axis (right).
      Debug.DrawRay(transform.position, transform.right * 5f, Color.red);

      // Yaw axis (up).
      Debug.DrawRay(transform.position, transform.up * 5f, Color.magenta);

      if (_accelerationInput != null) {
        Debug.DrawRay(transform.position, _accelerationInput * 1f, Color.green);
      }
    }
  }
}
