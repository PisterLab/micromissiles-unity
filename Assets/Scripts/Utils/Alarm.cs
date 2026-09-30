using System;

// A one-shot alarm driven by a caller-provided time source.
public sealed class Alarm {
  private float _fireAt;

  public bool IsArmed { get; private set; }

  // Arm the alarm to fire after the given delay.
  public void Set(float currentTime, float delaySeconds) {
    _fireAt = currentTime + Math.Max(0f, delaySeconds);
    IsArmed = true;
  }

  // Disarm the alarm without firing it.
  public void Clear() {
    _fireAt = 0f;
    IsArmed = false;
  }

  // Fire once when the deadline is reached. A fired alarm becomes disarmed.
  public bool TryFire(float currentTime) {
    if (!IsArmed || currentTime < _fireAt) {
      return false;
    }

    Clear();
    return true;
  }
}
