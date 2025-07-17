using UnityEngine;

// Runtime representation of an interceptor origin in the simulation.
// This component manages the actual GameObject state and provides access to current
// position/velocity. The associated InterceptorOriginConfig provides the static configuration data.
public class InterceptorOrigin : MonoBehaviour {
  private InterceptorOriginConfig _originConfig;
  private Vector3 _startPosition;
  private float _startTime;
  private Rigidbody _rigidbody;

  // Gets the unique identifier for this origin.
  public string OriginId => _originConfig?.id ?? "Unknown";

  // Sets the origin configuration for this GameObject.
  // Parameters:
  //   config: The origin configuration
  public void SetOriginConfig(InterceptorOriginConfig config) {
    _originConfig = config;
    _startPosition = transform.position;
    _startTime = Time.time;
    _rigidbody = GetComponent<Rigidbody>();
  }

  // Gets the origin configuration.
  // Returns: The origin configuration
  public InterceptorOriginConfig GetOriginConfig() {
    return _originConfig;
  }

  // Gets the current position of this origin in world space.
  // This is the actual GameObject position, not a calculated value.
  // Returns: Current position
  public Vector3 GetPosition() {
    return transform.position;
  }

  // Gets the current velocity of this origin.
  // For static origins, returns Vector3.zero.
  // For moving origins, returns the actual Rigidbody velocity.
  // Returns: Current velocity
  public Vector3 GetVelocity() {
    if (_rigidbody != null && !_rigidbody.isKinematic) {
      return _rigidbody.linearVelocity;
    }
    return Vector3.zero;
  }

  // Gets the distance from this origin to a target position.
  // Parameters:
  //   targetPosition: Target position in world space
  // Returns: Distance in meters
  public float GetDistanceToTarget(Vector3 targetPosition) {
    return Vector3.Distance(GetPosition(), targetPosition);
  }

  // Checks if this origin supports launching the specified interceptor type.
  // Parameters:
  //   interceptorType: Interceptor type to check
  // Returns: True if supported
  public bool SupportsInterceptorType(string interceptorType) {
    return _originConfig?.SupportsInterceptorType(interceptorType) ?? false;
  }

  // Checks if this origin has available capacity.
  // Returns: True if capacity available
  public bool HasCapacity() {
    return _originConfig?.HasCapacity() ?? false;
  }

  // Gets the available interceptor capacity.
  // Returns: Number of interceptors that can still be allocated
  public int GetAvailableCapacity() {
    return _originConfig?.GetAvailableCapacity() ?? 0;
  }

  // Allocates an interceptor from this origin.
  // Returns: True if allocation successful
  public bool AllocateInterceptor() {
    return _originConfig?.AllocateInterceptor() ?? false;
  }

  // Releases an interceptor back to this origin.
  public void ReleaseInterceptor() {
    _originConfig?.ReleaseInterceptor();
  }

  void Update() {
    // Update the displayed name or other visual elements if needed
    if (_originConfig != null) {
      // No longer need drift detection since we use actual GameObject positions
      // The position is always accurate because we read it directly from the transform
    }
  }

  // Called when the origin is selected in the editor or for debugging.
  void OnDrawGizmosSelected() {
    if (_originConfig != null) {
      // Draw capacity indicator
      Gizmos.color = HasCapacity() ? Color.green : Color.red;
      Gizmos.DrawWireSphere(transform.position, 50f);

      // Draw velocity vector for moving origins using actual rigidbody velocity
      Vector3 actualVelocity = GetVelocity();

      if (actualVelocity.magnitude > 0) {
        Gizmos.color = Color.blue;
        Vector3 velocityVector = actualVelocity.normalized * 200f;
        Gizmos.DrawRay(transform.position, velocityVector);

        // Draw movement trail
        Gizmos.color = Color.cyan;
        Vector3 startPos = _startPosition;
        Vector3 currentPos = transform.position;
        Gizmos.DrawLine(startPos, currentPos);

        // Draw predicted future position using actual velocity
        Gizmos.color = Color.yellow;
        Vector3 futurePos = transform.position + actualVelocity * 60f;  // 1 minute ahead
        Gizmos.DrawWireCube(futurePos, Vector3.one * 30f);
        Gizmos.DrawLine(currentPos, futurePos);
      }

      // Draw origin info text
      Vector3 labelPos = transform.position + Vector3.up * 100f;
#if UNITY_EDITOR
      UnityEditor.Handles.Label(
          labelPos, $"{OriginId}\n" +
                        $"Capacity: {GetAvailableCapacity()}/{_originConfig.max_interceptors}\n" +
                        $"Velocity: {actualVelocity.magnitude:F1} m/s\n" +
                        $"Types: {_originConfig.interceptor_types.Count}");
#endif
    }
  }

  // Draws gizmos for all origins, even when not selected.
  void OnDrawGizmos() {
    if (_originConfig != null) {
      // Draw a simple marker for the origin
      Gizmos.color = HasCapacity() ? Color.green : Color.red;
      Gizmos.DrawSphere(transform.position, 10f);

      // Draw direction indicator for moving origins using actual velocity
      Vector3 actualVelocity = GetVelocity();

      if (actualVelocity.magnitude > 0) {
        Gizmos.color = Color.blue;
        Vector3 forward = actualVelocity.normalized * 50f;
        Gizmos.DrawRay(transform.position, forward);
      }
    }
  }
}
