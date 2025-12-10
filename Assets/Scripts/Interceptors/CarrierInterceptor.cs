// Carrier interceptor.
//
// The carrier interceptor is a missile that carries missile interceptors as submunitions.
public class CarrierInterceptor : CarrierBase {
  protected override void Awake() {
    base.Awake();

    Movement = new MissileMovement(this);
    var assignment = new MaxSpeedAssignment(Assignment.Assignment_EvenAssignment_Assign);
    ReleaseStrategy = new ProximityReleaseStrategy(this, assignment);
  }
}
