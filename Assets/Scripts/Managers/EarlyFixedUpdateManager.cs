using UnityEngine;

// The early fixed update manager allows actions to be executed before the main fixed update call
// due to its position in the script execution order.
[DefaultExecutionOrder(-50)]
public class EarlyFixedUpdateManager : MonoBehaviour {
  // Simulation events.
  public delegate void UpdateHandler();
  public event UpdateHandler OnEarlyFixedUpdate;

  public static EarlyFixedUpdateManager Instance { get; private set; }

  private void Awake() {
    if (Instance != null && Instance != this) {
      Destroy(gameObject);
      return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject);
  }

  private void FixedUpdate() {
    OnEarlyFixedUpdate?.Invoke();
  }
}
