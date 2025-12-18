// Fixed wing threat.
//
// The fixed wing threat uses proportional navigation to follow its attack behavior.
public class FixedWingThreat : ThreatBase {
  // Default proportional navigation controller gain.
  private const float _proportionalNavigationGain = 50f;

  protected override void Awake() {
    base.Awake();

    Movement = new IdealMovement(this);
    Controller = new PnController(this, _proportionalNavigationGain);
  }
}
