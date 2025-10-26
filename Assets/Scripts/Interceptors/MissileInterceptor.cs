// Missile interceptor.
//
// The missile interceptor is a small, intelligent missile that tries to intercept a single threat.
public class MissileInterceptor : InterceptorBase {
  // Maximum number of threats that this interceptor can target.
  public override int Capacity => 1;

  protected override void Awake() {
    base.Awake();

    Movement = new MissileMovement(this);
  }
}
