using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Threat : Agent {
  protected AttackBehavior _attackBehavior;
  [SerializeField]
  protected Vector3 _currentWaypoint;
  [SerializeField]
  protected PowerSetting _currentPowerSetting;

  protected SensorOutput _sensorOutput;

  public void SetAttackBehavior(AttackBehavior attackBehavior) {
    _attackBehavior = attackBehavior;
    _target = SimManager.Instance.CreateDummyThreat(attackBehavior.targetPosition,
                                                    attackBehavior.targetVelocity);
  }

  protected float PowerTableLookup(PowerSetting powerSetting) {
    switch (powerSetting) {
      case PowerSetting.IDLE:
        return _staticAgentConfig.powerTable.IDLE;
      case PowerSetting.LOW:
        return _staticAgentConfig.powerTable.LOW;
      case PowerSetting.CRUISE:
        return _staticAgentConfig.powerTable.CRUISE;
      case PowerSetting.MIL:
        return _staticAgentConfig.powerTable.MIL;
      case PowerSetting.MAX:
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