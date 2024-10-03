using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotaryWingThreat : Threat {
  // Start is called before the first frame update
  protected override void Start() {
    base.Start();
  }

  // Update is called once per frame
  protected override void FixedUpdate() {
    base.FixedUpdate();
  }

  protected override void UpdateReady(double deltaTime) {}

  protected override void UpdateBoost(double deltaTime) {}

  protected override void UpdateMidCourse(double deltaTime) {}
}
