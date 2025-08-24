using UnityEngine;

// Component that tracks which launcher an interceptor was launched from.
// This enables proper capacity management and statistics tracking.
//
// This component is automatically attached to interceptors when they are launched
// and handles launcher capacity release when the interceptor is destroyed.
public class LauncherReference : MonoBehaviour {
  private Launcher _launcher;
  private bool _capacityReleased = false;

  // Sets the launcher reference for this interceptor.
  // This should be called immediately after the interceptor is created.
  /// <param name="launcher">The launcher this interceptor was launched from</param>
  public void SetLauncher(Launcher launcher) {
    _launcher = launcher;
  }

  // Gets the launcher this interceptor was launched from.
  /// <returns>Launcher, or null if not set</returns>
  public Launcher GetLauncher() {
    return _launcher;
  }

  // Manually releases the interceptor capacity back to the launcher.
  // This is automatically called when the interceptor is destroyed,
  // but can be called manually for early release scenarios.
  public void ReleaseCapacity() {
    if (_launcher != null && !_capacityReleased) {
      _launcher.ReleaseInterceptor();
      _capacityReleased = true;

      Debug.Log(
          $"Released interceptor capacity back to launcher {_launcher.LauncherId}. " +
          $"Available capacity: {_launcher.GetAvailableCapacity()}/{_launcher.GetLauncherConfig().max_interceptors}");
    }
  }

  // Automatically release capacity when the interceptor is destroyed.
  // This ensures proper cleanup and prevents capacity leaks.
  private void OnDestroy() {
    ReleaseCapacity();
  }

  // Gets a string representation for debugging.
  /// <returns>Debug information about the launcher reference</returns>
  public override string ToString() {
    if (_launcher == null) {
      return "LauncherReference[No Launcher Set]";
    }

    return $"LauncherReference[{_launcher.LauncherId}] - " +
           $"Capacity: {_launcher.GetAvailableCapacity()}/{_launcher.GetLauncherConfig().max_interceptors}, " +
           $"Released: {_capacityReleased}";
  }
}