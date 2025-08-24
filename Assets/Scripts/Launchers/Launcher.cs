using UnityEngine;

// Runtime representation of a launcher in the simulation.
// This component manages the actual GameObject state and provides access to current
// position/velocity. The associated LauncherConfig provides the static configuration data.
public class Launcher : Agent {
  private LauncherConfig _launcherConfig;
  private Vector3 _startPosition;
  private float _startTime;

  // Gets the unique identifier for this launcher.
  public string LauncherId => _launcherConfig?.id ?? "Unknown";

  // Override IsAssignable to indicate launchers are not assignable as targets
  public override bool IsAssignable() {
    return false;
  }

  // Sets the launcher configuration for this GameObject.
  /// <param name="config">The launcher configuration</param>
  public void SetLauncherConfig(LauncherConfig config) {
    _launcherConfig = config;
    _startPosition = transform.position;
    _startTime = Time.time;
  }

  // Gets the launcher configuration.
  /// <returns>The launcher configuration</returns>
  public LauncherConfig GetLauncherConfig() {
    return _launcherConfig;
  }

  // Gets the distance from this launcher to a target position.
  /// <param name="targetPosition">Target position in world space</param>
  /// <returns>Distance in meters</returns>
  public float GetDistanceToTarget(Vector3 targetPosition) {
    return Vector3.Distance(GetPosition(), targetPosition);
  }

  // Checks if this launcher supports launching the specified interceptor type.
  /// <param name="interceptorType">Interceptor type to check</param>
  /// <returns>True if supported</returns>
  public bool SupportsInterceptorType(string interceptorType) {
    return _launcherConfig?.SupportsInterceptorType(interceptorType) ?? false;
  }

  // Checks if this launcher has available capacity.
  /// <returns>True if capacity available</returns>
  public bool HasCapacity() {
    return _launcherConfig?.HasCapacity() ?? false;
  }

  // Gets the available interceptor capacity.
  /// <returns>Number of interceptors that can still be allocated</returns>
  public int GetAvailableCapacity() {
    return _launcherConfig?.GetAvailableCapacity() ?? 0;
  }

  // Allocates an interceptor from this launcher.
  /// <returns>True if allocation successful</returns>
  public bool AllocateInterceptor() {
    return _launcherConfig?.AllocateInterceptor() ?? false;
  }

  // Releases an interceptor back to this launcher.
  public void ReleaseInterceptor() {
    _launcherConfig?.ReleaseInterceptor();
  }

  protected override void Start() {
    base.Start();
    _startPosition = transform.position;
    _startTime = Time.time;
  }

  // Called when the launcher is selected in the editor or for debugging.
  protected virtual void OnDrawGizmosSelected() {
    if (_launcherConfig != null) {
      // Draw capacity indicator
      Gizmos.color = HasCapacity() ? Color.green : Color.red;
      Gizmos.DrawWireSphere(transform.position, 50f);

      // Draw velocity vector for moving launchers using actual rigidbody velocity
      Vector3 actualVelocity = GetVelocity();

      if (actualVelocity.magnitude > 0) {
        Gizmos.color = Color.blue;
        Vector3 velocityVector = actualVelocity.normalized * 200f;
        Gizmos.DrawRay(transform.position, velocityVector);

        // Draw movement trail
        Gizmos.color = Color.cyan;
        Vector3 startPosition = _startPosition;
        Vector3 currentPos = transform.position;
        Gizmos.DrawLine(startPosition, currentPos);

        // Draw predicted future position using actual velocity
        Gizmos.color = Color.yellow;
        Vector3 futurePos = transform.position + actualVelocity * 60f;  // 1 minute ahead
        Gizmos.DrawWireCube(futurePos, Vector3.one * 30f);
        Gizmos.DrawLine(currentPos, futurePos);
      }

      // Draw launcher info text
      Vector3 labelPos = transform.position + Vector3.up * 100f;
#if UNITY_EDITOR
      UnityEditor.Handles.Label(
          labelPos, $"{LauncherId}\n" +
                        $"Capacity: {GetAvailableCapacity()}/{_launcherConfig.max_interceptors}\n" +
                        $"Velocity: {actualVelocity.magnitude:F1} m/s\n" +
                        $"Types: {_launcherConfig.interceptor_types.Count}");
#endif
    }
  }

  // Draws gizmos for all launchers, even when not selected.
  protected virtual void OnDrawGizmos() {
    if (_launcherConfig != null) {
      // Draw a simple marker for the launcher
      Gizmos.color = HasCapacity() ? Color.green : Color.red;
      Gizmos.DrawSphere(transform.position, 10f);

      // Draw direction indicator for moving launchers using actual velocity
      Vector3 actualVelocity = GetVelocity();

      if (actualVelocity.magnitude > 0) {
        Gizmos.color = Color.blue;
        Vector3 forward = actualVelocity.normalized * 50f;
        Gizmos.DrawRay(transform.position, forward);
      }
    }
  }
}
