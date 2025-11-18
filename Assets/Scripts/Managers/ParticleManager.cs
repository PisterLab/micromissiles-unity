using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : MonoBehaviour {
  public static ParticleManager Instance { get; private set; }

  private Queue<GameObject> _missileTrailPool;
  [SerializeField]
  private int _missileTrailPoolCount;
  [SerializeField]
  private Queue<GameObject> _missileExplosionPool;
  [SerializeField]
  private int _missileExplosionPoolCount;

  [SerializeField]
  private List<TrailRenderer> _agentTrailRenderers = new List<TrailRenderer>();

  [SerializeField, Tooltip("The material to use for commandeered interceptor trail renderers")]
  public Material InterceptorTrailMatFaded;
  [SerializeField, Tooltip("The material to use for commandeered threat trail renderers")]
  public Material ThreatTrailMatFaded;

  [SerializeField]
  private List<GameObject> _hitMarkerList = new List<GameObject>();

  public void ClearHitMarkers() {
    foreach (var hitMarker in _hitMarkerList) {
      Destroy(hitMarker);
    }
    _hitMarkerList.Clear();
  }

  private void Awake() {
    if (Instance != null && Instance != this) {
      Destroy(gameObject);
      return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject);
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
    // Grab from Resources/Prefabs/Effects.
    GameObject missileTrailPrefab =
        Resources.Load<GameObject>("Prefabs/Effects/InterceptorSmokeEffect");

    // Pre-instantiate 10 missile trail particles.
    for (int i = 0; i < 10; ++i) {
      InstantiateMissileTrail(missileTrailPrefab);
    }
    // Instantiate over an interval.
    StartCoroutine(InstantiateMissileTrailsOverTime(missileTrailPrefab, 200, 0.05f));
  }

  private GameObject InstantiateMissileTrail(GameObject prefab) {
    GameObject trail = Instantiate(prefab, transform);
    trail.GetComponent<ParticleSystem>().Stop();
    _missileTrailPool.Enqueue(trail);
    return trail;
  }

  private IEnumerator InstantiateMissileTrailsOverTime(GameObject prefab, int count,
                                                       float duration) {
    float interval = duration / count;
    for (int i = 0; i < count; ++i) {
      InstantiateMissileTrail(prefab);
      yield return new WaitForSeconds(interval);
    }
  }

  private void InitializeMissileExplosionParticlePool() {
    GameObject missileExplosionPrefab =
        Resources.Load<GameObject>("Prefabs/Effects/InterceptExplosionEffect");
    // Pre-instantiate 10 missile trail particles.
    for (int i = 0; i < 10; ++i) {
      InstantiateMissileExplosion(missileExplosionPrefab);
    }
    StartCoroutine(InstantiateMissileExplosionsOverTime(missileExplosionPrefab, 200, 0.05f));
  }

  private GameObject InstantiateMissileExplosion(GameObject prefab) {
    GameObject explosion = Instantiate(prefab, transform);
    explosion.GetComponent<ParticleSystem>().Stop();
    _missileExplosionPool.Enqueue(explosion);
    return explosion;
  }

  private IEnumerator InstantiateMissileExplosionsOverTime(GameObject prefab, int count,
                                                           float duration) {
    float interval = duration / count;
    for (int i = 0; i < count; ++i) {
      InstantiateMissileExplosion(prefab);
      yield return new WaitForSeconds(interval);
    }
  }

  private void RegisterNewInterceptor(IInterceptor interceptor) {
    interceptor.OnHit += RegisterInterceptorHit;
    interceptor.OnMiss += RegisterInterceptorMiss;
    interceptor.OnTerminated += RegisterAgentTerminated;
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
    GameObject hitMarkerObject = SpawnHitMarker(interceptor.transform.position);
    hitMarkerObject.GetComponent<UIEventMarker>().SetEventMiss();
    _hitMarkerList.Add(hitMarkerObject);
  }

  private void RegisterNewThreat(IThreat threat) {
    threat.OnTerminated += RegisterAgentTerminated;
  }

  private void RegisterAgentTerminated(IAgent agent) {
    if (SimManager.Instance.SimulatorConfig.PersistentFlightTrails) {
      CommandeerAgentTrailRenderer(agent);
    }
  }

  private void RegisterSimulationEnded() {
    foreach (var trailRenderer in _agentTrailRenderers) {
      Destroy(trailRenderer.gameObject);
    }
    _agentTrailRenderers.Clear();
  }

  private GameObject SpawnHitMarker(in Vector3 position) {
    GameObject hitMarker = Instantiate(Resources.Load<GameObject>("Prefabs/HitMarkerPrefab"),
                                       position, Quaternion.identity);
    _hitMarkerList.Add(hitMarker);
    return hitMarker;
  }

  private GameObject PlayMissileExplosion(in Vector3 position) {
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

  private void CommandeerAgentTrailRenderer(IAgent agent) {
    // Take the TrailRenderer component off of the agent, so it can be destroyed at the end of the
    // simulation.
    TrailRenderer trailRenderer = agent.gameObject.GetComponentInChildren<TrailRenderer>();
    if (trailRenderer != null) {
      trailRenderer.transform.parent = transform;
      _agentTrailRenderers.Add(trailRenderer);
      trailRenderer.material = (agent is IThreat) ? ThreatTrailMatFaded : InterceptorTrailMatFaded;
    } else {
      Debug.LogWarning("Agent has no TrailRenderer component.");
    }
  }
}
