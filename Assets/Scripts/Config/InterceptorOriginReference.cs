using UnityEngine;

/// <summary>
/// Component that tracks which origin an interceptor was launched from.
/// This enables proper capacity management and statistics tracking.
///
/// This component is automatically attached to interceptors when they are launched
/// and handles origin capacity release when the interceptor is destroyed.
/// </summary>
public class InterceptorOriginReference : MonoBehaviour {
  private InterceptorOriginConfig _origin;
  private bool _capacityReleased = false;

  /// <summary>
  /// Sets the origin reference for this interceptor.
  /// This should be called immediately after the interceptor is created.
  /// </summary>
  /// <param name="origin">The origin this interceptor was launched from</param>
  public void SetOrigin(InterceptorOriginConfig origin) {
    _origin = origin;
  }

  /// <summary>
  /// Gets the origin this interceptor was launched from.
  /// </summary>
  /// <returns>Origin configuration, or null if not set</returns>
  public InterceptorOriginConfig GetOrigin() {
    return _origin;
  }

  /// <summary>
  /// Manually releases the interceptor capacity back to the origin.
  /// This is automatically called when the interceptor is destroyed,
  /// but can be called manually for early release scenarios.
  /// </summary>
  public void ReleaseCapacity() {
    if (_origin != null && !_capacityReleased) {
      _origin.ReleaseInterceptor();
      _capacityReleased = true;

      Debug.Log($"Released interceptor capacity back to origin {_origin.id}. " +
                $"Available capacity: {_origin.GetAvailableCapacity()}/{_origin.max_interceptors}");
    }
  }

  /// <summary>
  /// Automatically release capacity when the interceptor is destroyed.
  /// This ensures proper cleanup and prevents capacity leaks.
  /// </summary>
  private void OnDestroy() {
    ReleaseCapacity();
  }

  /// <summary>
  /// Gets a string representation for debugging.
  /// </summary>
  /// <returns>Debug information about the origin reference</returns>
  public override string ToString() {
    if (_origin == null) {
      return "InterceptorOriginReference[No Origin Set]";
    }

    return $"InterceptorOriginReference[{_origin.id}] - " +
           $"Capacity: {_origin.GetAvailableCapacity()}/{_origin.max_interceptors}, " +
           $"Released: {_capacityReleased}";
  }
}
