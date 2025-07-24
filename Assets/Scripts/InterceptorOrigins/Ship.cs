using UnityEngine;

// Represents a naval vessel capable of launching interceptors.
// Ships are mobile interceptor origins that can move across the water surface.
public class Ship : InterceptorOrigin {
  
  protected override void Start() {
    base.Start();
    
    // Ships should always be able to move
    var rigidbody = GetComponent<Rigidbody>();
    if (rigidbody != null) {
      rigidbody.useGravity = false; // Ships float
      rigidbody.constraints = RigidbodyConstraints.FreezeRotation | 
                             RigidbodyConstraints.FreezePositionY; // Keep on water surface
    }
  }
  
  protected override void FixedUpdate() {
    base.FixedUpdate();
    
    // Ships maintain constant velocity from their configuration
    var config = GetOriginConfig();
    if (config != null && config.velocity.magnitude > 0) {
      // Maintain configured velocity
      SetVelocity(config.velocity);
      
      // Orient ship in direction of movement
      if (config.velocity.magnitude > 0.1f) {
        transform.rotation = Quaternion.LookRotation(config.velocity.normalized, Vector3.up);
      }
    }
  }
  
  // Override gizmo drawing for ship-specific visualization
  protected override void OnDrawGizmos() {
    base.OnDrawGizmos();
    
    // Draw ship-specific elements (e.g., wake, heading indicator)
    if (GetVelocity().magnitude > 0) {
      Gizmos.color = new Color(0, 0.5f, 1, 0.5f); // Light blue for water
      Vector3 wakeStart = transform.position - GetVelocity().normalized * 20f;
      Vector3 wakeEnd = transform.position - GetVelocity().normalized * 100f;
      Gizmos.DrawLine(wakeStart, wakeEnd);
    }
  }
} 