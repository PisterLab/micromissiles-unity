// Vessel.
//
// The vessel represents a DDG or any other ship that moves along the ground and launches carrier
// interceptors.
public class Vessel : LauncherBase {
  protected override void Awake() {
    base.Awake();

    Movement = new GroundMovement(this);
  }
}
