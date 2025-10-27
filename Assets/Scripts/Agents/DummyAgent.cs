using UnityEngine;

// Dummy agent.
//
// A dummy agent simply integrates its own acceleration with no acceleration input.
public class DummyAgent : AgentBase {
  protected override void FixedUpdate() {
    base.FixedUpdate();

    _rigidbody.AddForce(Acceleration, ForceMode.Acceleration);
  }
}
