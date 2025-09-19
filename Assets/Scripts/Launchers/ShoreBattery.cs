using UnityEngine;

// Represents a shore-based interceptor launch installation.
// Shore batteries are static launchers with typically higher capacity.
public class ShoreBattery : Launcher {
  protected override void Start() {
    base.Start();

    // Shore batteries are always static
    var rigidbody = GetComponent<Rigidbody>();
    if (rigidbody != null) {
      rigidbody.isKinematic = true;  // Immovable
      rigidbody.useGravity = false;
    }
  }

  // Override gizmo drawing for shore battery-specific visualization
  protected override void OnDrawGizmosSelected() {
    base.OnDrawGizmosSelected();

    // Draw coverage radius indicator
    var config = GetLauncherConfig();
    if (config != null) {
      Gizmos.color = new Color(0, 1, 0, 0.1f);  // Transparent green

      // Typical engagement range visualization (example: 100km)
      const float EngagementRange = 100000f;  // 100km in meters
      Gizmos.DrawWireSphere(transform.position, EngagementRange);

      // Draw ground installation marker
      Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);  // Gray
      Vector3 baseSize = new Vector3(100f, 20f, 100f);
      Gizmos.DrawCube(transform.position - Vector3.up * 10f, baseSize);
    }
  }

  // Override base gizmo to show shore battery is static
  protected override void OnDrawGizmos() {
    base.OnDrawGizmos();

    // Draw a foundation/base to indicate static installation
    Gizmos.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    Vector3 baseSize = new Vector3(50f, 10f, 50f);
    Gizmos.DrawCube(transform.position - Vector3.up * 5f, baseSize);
  }
}
