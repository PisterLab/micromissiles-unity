using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ParticleManager : MonoBehaviour {
  public static ParticleManager Instance { get; private set; }

  [SerializeField]
  private Queue<GameObject> _missileTrailPool;
  [SerializeField]
  private Queue<GameObject> _missileExplosionPool;

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

    if (SimManager.Instance.simulatorConfig.enableMissileTrailEffect) {
      InitializeMissileTrailParticlePool();
    }
    if (SimManager.Instance.simulatorConfig.enableExplosionEffect) {
      InitializeMissileExplosionParticlePool();
    }

    SimManager.Instance.OnNewInterceptor += RegisterNewInterceptor;
  }

  private void InitializeMissileTrailParticlePool() {
    // Grab from Resources/Prefabs/Effects
    GameObject missileTrailPrefab =
        Resources.Load<GameObject>("Prefabs/Effects/InterceptorSmokeEffect");

    // Pre-instantiate 10 missile trail particles
    for (int i = 0; i < 10; i++) {
      InstantiateMissileTrail(missileTrailPrefab);
    }
    // Instantiate over an interval
    StartCoroutine(InstantiateMissileTrailsOverTime(missileTrailPrefab, 200, 0.05f));
  }

  private void InitializeMissileExplosionParticlePool() {
    GameObject missileExplosionPrefab =
        Resources.Load<GameObject>("Prefabs/Effects/InterceptExplosionEffect");
    // Pre-instantiate 10 missile trail particles
    for (int i = 0; i < 10; i++) {
      InstantiateMissileExplosion(missileExplosionPrefab);
    }
    StartCoroutine(InstantiateMissileExplosionsOverTime(missileExplosionPrefab, 200, 0.05f));
  }

  private void RegisterNewInterceptor(Interceptor interceptor) {
    interceptor.OnInterceptHit += RegisterInterceptorHit;
  }

  private void RegisterInterceptorHit(Interceptor interceptor, Threat threat) {
    if (SimManager.Instance.simulatorConfig.enableExplosionEffect) {
      PlayMissileExplosion(interceptor.transform.position);
    }
  }

  /// <summary>
  /// Returns a missile explosion particle prefab from the pool and plays it at the specified
  /// location. If the pool is empty, it returns null.
  /// </summary>
  public GameObject PlayMissileExplosion(Vector3 position) {
    if (_missileExplosionPool.Count > 0) {
      GameObject explosion = _missileExplosionPool.Dequeue();
      explosion.transform.position = position;

      ParticleSystem particleSystem = explosion.GetComponent<ParticleSystem>();
      if (particleSystem != null) {
        particleSystem.Clear();
        particleSystem.Play();
        StartCoroutine(ReturnExplosionAfterDelay(explosion, particleSystem.main.duration));
      } else {
        Debug.LogError("Missile explosion particle has no ParticleSystem component");
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
      Debug.LogError("Attempted to return a null missile explosion particle");
      return;
    }

    explosion.transform.parent = transform;
    explosion.transform.localPosition = Vector3.zero;
    ParticleSystem particleSystem = explosion.GetComponent<ParticleSystem>();
    if (particleSystem != null) {
      particleSystem.Stop();
      particleSystem.Clear();
    } else {
      Debug.LogError("Attempted to return a missile explosion particle with no particle system");
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
    for (int i = 0; i < count; i++) {
      InstantiateMissileTrail(prefab);
      yield return new WaitForSeconds(interval);
    }
  }

  private IEnumerator InstantiateMissileExplosionsOverTime(GameObject prefab, int count,
                                                           float duration) {
    float interval = duration / count;
    for (int i = 0; i < count; i++) {
      InstantiateMissileExplosion(prefab);
      yield return new WaitForSeconds(interval);
    }
  }

  /// <summary>
  /// Returns a missile trail particle prefab from the pool.
  /// If the pool is empty, it returns null
  /// </summary>
  /// <returns></returns>
  public GameObject RequestMissileTrailParticle() {
    if (_missileTrailPool.Count > 0 &&
        SimManager.Instance.simulatorConfig.enableMissileTrailEffect) {
      GameObject trail = _missileTrailPool.Dequeue();

      return trail;
    }
    return null;
  }

  public void ReturnMissileTrailParticle(GameObject trail) {
    if (trail == null) {
      Debug.LogError("Attempted to return a null missile trail particle");
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
      Debug.LogError("Attempted to return a missile trail particle with no particle system");
    }

    _missileTrailPool.Enqueue(trail);
  }
}