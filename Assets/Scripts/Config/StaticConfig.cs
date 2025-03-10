using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[Serializable]
public class StaticAgentConfig {
  public string name;
  public string agentClass;
  public string symbolPresent;
  public string symbolDestroyed;
  public float unitCost;

  [Serializable]
  public class AccelerationConfig {
    public float maxReferenceNormalAcceleration = 300f;
    public float referenceSpeed = 1000f;
    public float maxForwardAcceleration = 50f;
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

  public AccelerationConfig accelerationConfig;
  public BoostConfig boostConfig;
  public LiftDragConfig liftDragConfig;
  public BodyConfig bodyConfig;
  public HitConfig hitConfig;
  public PowerTable powerTable;
}

[JsonConverter(typeof(StringEnumConverter))]
public enum PowerSetting { IDLE, LOW, CRUISE, MIL, MAX }
