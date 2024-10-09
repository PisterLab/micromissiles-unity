using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor.Animations;

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

    // Grab from Resources/Prefabs/Effects
    GameObject missileTrailPrefab = Resources.Load<GameObject>("Prefabs/Effects/InterceptorSmokeEffect");

    // Pre-instantiate 10 missile trail particles
    for (int i = 0; i < 10; i++) {
      InstantiateMissileTrail(missileTrailPrefab);
    }

    // Instantiate over an interval
    StartCoroutine(InstantiateMissileTrailsOverTime(missileTrailPrefab, 200, 0.05f));
  }

  private GameObject InstantiateMissileTrail(GameObject prefab) {
    GameObject trail = Instantiate(prefab, transform);
    trail.GetComponent<ParticleSystem>().Stop();
    _missileTrailPool.Enqueue(trail);
    return trail;
  }

  private IEnumerator InstantiateMissileTrailsOverTime(GameObject prefab, int count, float duration) {
    float interval = duration / count;
    for (int i = 0; i < count; i++) {
      InstantiateMissileTrail(prefab);
      yield return new WaitForSeconds(interval);
    }
  }

  /// <summary>
  /// Returns a missile trail particle prefab from the pool. 
  /// If the pool is empty, it returns null
  /// </summary>
  /// <returns></returns>
  public GameObject RequestMissileTrailParticle() {
    if (_missileTrailPool.Count > 0) {
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
    }
    else {
      Debug.LogError("Attempted to return a missile trail particle with no particle system");
    }
    
    _missileTrailPool.Enqueue(trail);
  }
}

