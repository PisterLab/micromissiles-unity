using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Threat : Agent {

  protected AttackBehavior _attackBehavior;
  [SerializeField]
  protected Vector3 _currentWaypoint;
  [SerializeField]
  protected StaticAgentConfig.PowerSetting _currentPowerSetting;

  protected float PowerTableLookup(StaticAgentConfig.PowerSetting powerSetting) {
    switch (powerSetting)
    {
        case StaticAgentConfig.PowerSetting.IDLE:
            return _staticAgentConfig.powerTable.IDLE;
        case StaticAgentConfig.PowerSetting.LOW:
            return _staticAgentConfig.powerTable.LOW;
        case StaticAgentConfig.PowerSetting.CRUISE:
            return _staticAgentConfig.powerTable.CRUISE;
        case StaticAgentConfig.PowerSetting.MIL:
            return _staticAgentConfig.powerTable.MIL;
        case StaticAgentConfig.PowerSetting.MAX:
            return _staticAgentConfig.powerTable.MAX;
        default:
            Debug.LogError("Invalid power setting");
            return 0f;
    }
  }

  public override bool IsAssignable() {
    return false;
  }

  protected override void Start() {
    base.Start();
  }

  protected override void FixedUpdate() {
    base.FixedUpdate();
  }
}