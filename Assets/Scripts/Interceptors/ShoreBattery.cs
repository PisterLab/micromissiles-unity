// Shore battery.
//
// The shore battery launches carrier interceptors from a stationary, ground-based launcher.
public class ShoreBattery : LauncherBase {
  protected override void Awake() {
    base.Awake();

    Movement = new NoMovement(this);
  }
}
