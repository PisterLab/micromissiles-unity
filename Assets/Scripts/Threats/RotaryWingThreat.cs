// Rotary wing threat.
//
// The rotary wing threat uses a waypoint controller to follow its attack behavior.
public class RotaryWingThreat : ThreatBase {
  protected override void Awake() {
    base.Awake();

    Movement = new IdealMovement(this);
    Controller = new WaypointController(this);
  }
}
