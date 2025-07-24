using UnityEngine;

// Component that tracks which origin an interceptor was launched from.
// This enables proper capacity management and statistics tracking.
//
// This component is automatically attached to interceptors when they are launched
// and handles origin capacity release when the interceptor is destroyed.
public class InterceptorOriginReference : MonoBehaviour {
  private InterceptorOrigin _origin;
  private bool _capacityReleased = false;

  // Sets the origin reference for this interceptor.
  // This should be called immediately after the interceptor is created.
  //   origin: The origin this interceptor was launched from
  public void SetOrigin(InterceptorOrigin origin) {
    _origin = origin;
  }

  // Gets the origin this interceptor was launched from.
  // Returns: Origin configuration, or null if not set
  public InterceptorOrigin GetOrigin() {
    return _origin;
  }

  // Manually releases the interceptor capacity back to the origin.
  // This is automatically called when the interceptor is destroyed,
  // but can be called manually for early release scenarios.
  public void ReleaseCapacity() {
    if (_origin != null && !_capacityReleased) {
      _origin.ReleaseInterceptor();
      _capacityReleased = true;

      Debug.Log($"Released interceptor capacity back to origin {_origin.OriginId}. " +
                $"Available capacity: {_origin.GetAvailableCapacity()}/{_origin.GetOriginConfig().max_interceptors}");
    }
  }

  // Automatically release capacity when the interceptor is destroyed.
  // This ensures proper cleanup and prevents capacity leaks.
  private void OnDestroy() {
    ReleaseCapacity();
  }

  // Gets a string representation for debugging.
  // Returns: Debug information about the origin reference
  public override string ToString() {
    if (_origin == null) {
      return "InterceptorOriginReference[No Origin Set]";
    }

    return $"InterceptorOriginReference[{_origin.OriginId}] - " +
           $"Capacity: {_origin.GetAvailableCapacity()}/{_origin.GetOriginConfig().max_interceptors}, " +
           $"Released: {_capacityReleased}";
  }
}
