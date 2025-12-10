using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ParticleManager : MonoBehaviour {
  public static ParticleManager Instance { get; private set; }

  private Queue<GameObject> _missileTrailPool;
  [SerializeField]
  // Queues cannot be serialized, so if we want to see it in the inspector,
  // we need to serialize the count.
  private int _missileTrailPoolCount;
  [SerializeField]
  private Queue<GameObject> _missileExplosionPool;
  [SerializeField]
  private int _missileExplosionPoolCount;

  [SerializeField]
  private List<TrailRenderer> _agentTrailRenderers = new List<TrailRenderer>();

  [SerializeField, Tooltip("The material to use for commandeered interceptor trail renderers")]
  public Material interceptorTrailMatFaded;
  [SerializeField, Tooltip("The material to use for commandeered threat trail renderers")]
  public Material threatTrailMatFaded;

  [SerializeField]
  private List<GameObject> _hitMarkerList = new List<GameObject>();

  private void Awake() {
    if (Instance == null) {
      Instance = this;
      DontDestroyOnLoad(gameObject);
    } else {
      Destroy(gameObject);
    }
  }

  private void Start() {
    _missileTrailPool = new Queue<GameObject>();
    _missileExplosionPool = new Queue<GameObject>();
    _hitMarkerList = new List<GameObject>();

    if (SimManager.Instance.SimulatorConfig.EnableMissileTrailEffect) {
      InitializeMissileTrailParticlePool();
    }
    if (SimManager.Instance.SimulatorConfig.EnableExplosionEffect) {
      InitializeMissileExplosionParticlePool();
    }

    // Subscribe to events.
    SimManager.Instance.OnNewInterceptor += RegisterNewInterceptor;
    SimManager.Instance.OnNewThreat += RegisterNewThreat;

    SimManager.Instance.OnSimulationEnded += RegisterSimulationEnded;
  }

  private void Update() {
    _missileTrailPoolCount = _missileTrailPool.Count;
    _missileExplosionPoolCount = _missileExplosionPool.Count;
  }

  private void InitializeMissileTrailParticlePool() {
    // Grab from Resources/Prefabs/Effects
    GameObject missileTrailPrefab =
        Resources.Load<GameObject>("Prefabs/Effects/InterceptorSmokeEffect");

    // Pre-instantiate 10 missile trail particles
    for (int i = 0; i < 10; ++i) {
      InstantiateMissileTrail(missileTrailPrefab);
    }
    // Instantiate over an interval
    StartCoroutine(InstantiateMissileTrailsOverTime(missileTrailPrefab, 200, 0.05f));
  }

  private void InitializeMissileExplosionParticlePool() {
    GameObject missileExplosionPrefab =
        Resources.Load<GameObject>("Prefabs/Effects/InterceptExplosionEffect");
    // Pre-instantiate 10 missile trail particles
    for (int i = 0; i < 10; ++i) {
      InstantiateMissileExplosion(missileExplosionPrefab);
    }
    StartCoroutine(InstantiateMissileExplosionsOverTime(missileExplosionPrefab, 200, 0.05f));
  }

  private void RegisterSimulationEnded() {
    foreach (var trailRenderer in _agentTrailRenderers) {
      Destroy(trailRenderer.gameObject);
    }
    _agentTrailRenderers.Clear();
  }

  private void RegisterNewInterceptor(IInterceptor interceptor) {
    interceptor.OnHit += RegisterInterceptorHit;
    interceptor.OnMiss += RegisterInterceptorMiss;
    interceptor.OnTerminated += RegisterAgentTerminated;
  }

  private void RegisterNewThreat(IThreat threat) {
    threat.OnHit += RegisterThreatHit;
    threat.OnMiss += RegisterThreatMiss;
    threat.OnTerminated += RegisterAgentTerminated;
  }

  private void RegisterAgentTerminated(IAgent agent) {
    if (SimManager.Instance.SimulatorConfig.PersistentFlightTrails) {
      CommandeerAgentTrailRenderer(agent);
    }
  }

  private void RegisterInterceptorHit(IInterceptor interceptor) {
    if (SimManager.Instance.SimulatorConfig.EnableExplosionEffect) {
      PlayMissileExplosion(interceptor.transform.position);
    }
    GameObject hitMarkerObject = SpawnHitMarker(interceptor.transform.position);
    hitMarkerObject.GetComponent<UIEventMarker>().SetEventHit();
    _hitMarkerList.Add(hitMarkerObject);
  }

  private void RegisterInterceptorMiss(IInterceptor interceptor) {
    // It does not make sense to commandeer the TrailRenderer for a miss.
    // As the interceptor remains in flight
    GameObject hitMarkerObject = SpawnHitMarker(interceptor.transform.position);
    hitMarkerObject.GetComponent<UIEventMarker>().SetEventMiss();
    _hitMarkerList.Add(hitMarkerObject);
  }

  private void RegisterThreatHit(IThreat threat) {}

  private void RegisterThreatMiss(IThreat threat) {}

  private void CommandeerAgentTrailRenderer(IAgent agent) {
    // Take the TrailRenderer component off of the agent, onto us, and store it
    // (so we can destroy it later on simulation end)
    TrailRenderer trailRenderer = agent.gameObject.GetComponentInChildren<TrailRenderer>();
    if (trailRenderer != null) {
      trailRenderer.transform.parent = transform;
      _agentTrailRenderers.Add(trailRenderer);
      trailRenderer.material = (agent is IThreat) ? threatTrailMatFaded : interceptorTrailMatFaded;
    } else {
      Debug.LogWarning("Agent has no TrailRenderer component");
    }
  }

  private GameObject SpawnHitMarker(in Vector3 position) {
    GameObject hitMarker = Instantiate(Resources.Load<GameObject>("Prefabs/HitMarkerPrefab"),
                                       position, Quaternion.identity);
    _hitMarkerList.Add(hitMarker);
    return hitMarker;
  }

  public void ClearHitMarkers() {
    foreach (var hitMarker in _hitMarkerList) {
      Destroy(hitMarker);
    }
    _hitMarkerList.Clear();
  }

  /// Returns a missile explosion particle prefab from the pool and plays it at the specified
  /// location. If the pool is empty, it returns null.
  public GameObject PlayMissileExplosion(in Vector3 position) {
    if (_missileExplosionPool.Count > 0) {
      GameObject explosion = _missileExplosionPool.Dequeue();
      explosion.transform.position = position;

      ParticleSystem particleSystem = explosion.GetComponent<ParticleSystem>();
      if (particleSystem != null) {
        particleSystem.Clear();
        particleSystem.Play();
        StartCoroutine(ReturnExplosionAfterDelay(explosion, particleSystem.main.duration));
      } else {
        Debug.LogError("Missile explosion particle has no ParticleSystem component.");
        _missileExplosionPool.Enqueue(explosion);
        return null;
      }

      return explosion;
    }
    return null;
  }

  private IEnumerator ReturnExplosionAfterDelay(GameObject explosion, float delay) {
    yield return new WaitForSeconds(delay);
    ReturnMissileExplosionParticle(explosion);
  }

  private void ReturnMissileExplosionParticle(GameObject explosion) {
    if (explosion == null) {
      Debug.LogError("Attempted to return a null missile explosion particle.");
      return;
    }

    explosion.transform.parent = transform;
    explosion.transform.localPosition = Vector3.zero;
    ParticleSystem particleSystem = explosion.GetComponent<ParticleSystem>();
    if (particleSystem != null) {
      particleSystem.Stop();
      particleSystem.Clear();
    } else {
      Debug.LogError("Attempted to return a missile explosion particle with no particle system.");
    }

    _missileExplosionPool.Enqueue(explosion);
  }

  private GameObject InstantiateMissileTrail(GameObject prefab) {
    GameObject trail = Instantiate(prefab, transform);
    trail.GetComponent<ParticleSystem>().Stop();
    _missileTrailPool.Enqueue(trail);
    return trail;
  }

  private GameObject InstantiateMissileExplosion(GameObject prefab) {
    GameObject explosion = Instantiate(prefab, transform);
    explosion.GetComponent<ParticleSystem>().Stop();
    _missileExplosionPool.Enqueue(explosion);
    return explosion;
  }

  private IEnumerator InstantiateMissileTrailsOverTime(GameObject prefab, int count,
                                                       float duration) {
    float interval = duration / count;
    for (int i = 0; i < count; ++i) {
      InstantiateMissileTrail(prefab);
      yield return new WaitForSeconds(interval);
    }
  }

  private IEnumerator InstantiateMissileExplosionsOverTime(GameObject prefab, int count,
                                                           float duration) {
    float interval = duration / count;
    for (int i = 0; i < count; ++i) {
      InstantiateMissileExplosion(prefab);
      yield return new WaitForSeconds(interval);
    }
  }

  /// Returns a missile trail particle prefab from the pool. If the pool is empty, it returns null.
  public GameObject RequestMissileTrailParticle() {
    if (_missileTrailPool.Count > 0 &&
        SimManager.Instance.SimulatorConfig.EnableMissileTrailEffect) {
      GameObject trail = _missileTrailPool.Dequeue();

      return trail;
    }
    return null;
  }

  public void ReturnMissileTrailParticle(GameObject trail) {
    if (trail == null) {
      Debug.LogError("Attempted to return a null missile trail particle.");
      return;
    }

    trail.transform.parent = transform;
    trail.transform.localPosition = Vector3.zero;
    ParticleSystem particleSystem = trail.GetComponent<ParticleSystem>();
    if (particleSystem != null) {
      particleSystem.Stop();
      particleSystem.Clear();
      var emission = particleSystem.emission;
      emission.enabled = true;
    } else {
      Debug.LogError("Attempted to return a missile trail particle with no particle system.");
    }

    _missileTrailPool.Enqueue(trail);
  }
}
