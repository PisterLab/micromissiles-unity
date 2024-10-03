using System;

[Serializable]
public class StaticAgentConfig {
  public string name;
  public string agentClass;
  [Serializable]
  public class AccelerationConfig {
    public float maxReferenceAcceleration = 300f;
    public float referenceSpeed = 1000f;
  }

  [Serializable]
  public class BoostConfig {
    public float boostTime = 0.3f;
    public float boostAcceleration = 350f;
  }

  [Serializable]
  public class LiftDragConfig {
    public float liftCoefficient = 0.2f;
    public float dragCoefficient = 0.7f;
    public float liftDragRatio = 5f;
  }

  [Serializable]
  public class BodyConfig {
    public float mass = 0.37f;
    public float crossSectionalArea = 3e-4f;
    public float finArea = 6e-4f;
    public float bodyArea = 1e-2f;
  }

  [Serializable]
  public class HitConfig {
    public float hitRadius = 1f;
    public float killProbability = 0.9f;
  }

  [Serializable]
  public class PowerTable {
    public float IDLE;
    public float LOW;
    public float CRUISE;
    public float MIL;
    public float MAX;
  }

  public enum PowerSetting {
    IDLE,
    LOW,
    CRUISE,
    MIL,
    MAX
  }

  public PowerTable powerTable;

  public AccelerationConfig accelerationConfig;
  public BoostConfig boostConfig;
  public LiftDragConfig liftDragConfig;
  public BodyConfig bodyConfig;
  public HitConfig hitConfig;
}