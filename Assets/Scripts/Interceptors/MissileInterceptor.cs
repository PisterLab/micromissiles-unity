// Missile interceptor.
//
// The missile interceptor is a small, intelligent missile that tries to intercept a single threat.
public class MissileInterceptor : InterceptorBase {
  protected override void Awake() {
    base.Awake();

    Movement = new MissileMovement(this);
  }
}
