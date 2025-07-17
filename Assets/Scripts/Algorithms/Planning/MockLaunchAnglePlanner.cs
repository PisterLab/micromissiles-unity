using UnityEngine;

// Mock implementation of ILaunchAnglePlanner for testing purposes.
// Allows controlled responses for testing launch planning logic.
public class MockLaunchAnglePlanner : ILaunchAnglePlanner {
  private float _mockLaunchAngle = 45f;
  private float _mockTimeToPosition = 10f;
  private Vector3 _lastOriginUsed;
  private int _callCount = 0;
  private bool _convergentMode = false;
  private Vector3 _lastInterceptPosition = Vector3.zero;

  public void SetMockResponse(float launchAngle, float timeToPosition) {
    _mockLaunchAngle = launchAngle;
    _mockTimeToPosition = timeToPosition;
  }

  public void SetMockConvergentResponse() {
    _convergentMode = true;
    _callCount = 0;  // Reset call count for convergence simulation
  }

  // Required interface method
  public LaunchAngleOutput Plan(in LaunchAngleInput input) {
    _callCount++;

    if (_convergentMode) {
      float convergenceFactor = 1f / _callCount;
      return new LaunchAngleOutput(_mockLaunchAngle + convergenceFactor,
                                   _mockTimeToPosition + convergenceFactor);
    }

    return new LaunchAngleOutput(_mockLaunchAngle, _mockTimeToPosition);
  }

  public LaunchAngleOutput Plan(Vector3 targetPosition) {
    return Plan(targetPosition, Vector3.zero);
  }

  public LaunchAngleOutput Plan(Vector3 targetPosition, Vector3 originPosition) {
    _lastOriginUsed = originPosition;
    _callCount++;

    float distance = Vector3.Distance(originPosition, targetPosition);

    if (_convergentMode) {
      // Simulate convergence by returning slightly different values that converge
      float convergenceFactor = 1f / _callCount;
      return new LaunchAngleOutput(_mockLaunchAngle + convergenceFactor,
                                   _mockTimeToPosition + convergenceFactor);
    }

    // Make launch angle and time dependent on origin position to ensure different results
    float originBasedVariation = (originPosition.x + originPosition.z) * 0.01f;
    float adjustedLaunchAngle = _mockLaunchAngle + originBasedVariation;
    float adjustedTimeToPosition = _mockTimeToPosition + originBasedVariation;

    return new LaunchAngleOutput(adjustedLaunchAngle, adjustedTimeToPosition);
  }

  public Vector3 GetInterceptPosition(Vector3 targetPosition, Vector3 originPosition) {
    if (_convergentMode) {
      // Return positions that converge over iterations
      // Start with a larger offset and converge to target position
      float convergenceOffset = Mathf.Max(1f, 50f / _callCount);  // Starts at 50, converges to 1
      Vector3 direction = (targetPosition - originPosition).normalized;
      Vector3 offset = direction * convergenceOffset;
      _lastInterceptPosition = targetPosition + offset;
      return _lastInterceptPosition;
    }

    // Simple but realistic intercept calculation for testing
    // The intercept position should be close to the predicted target position
    // with origin-dependent variation to ensure different behavior for different origins

    float originVariation = (originPosition.x + originPosition.z) * 0.001f;

    // Return the target position with slight variation
    // This simulates a simple intercept calculation
    return targetPosition + Vector3.right * originVariation;
  }

  public bool WasCalledWithOrigin(Vector3 expectedOrigin) {
    return Vector3.Distance(_lastOriginUsed, expectedOrigin) < 0.001f;
  }

  public int GetCallCount() {
    return _callCount;
  }
}
