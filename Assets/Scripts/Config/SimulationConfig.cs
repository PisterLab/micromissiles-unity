using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[Serializable]
public class SimulationConfig {
  [Header("Simulation Settings")]
  public float timeScale = 0.05f;

  public float endTime = 300f;  // 5 minutes by default

  [Header("Interceptor Origins")]
  public List<InterceptorOriginConfig> interceptor_origins = new List<InterceptorOriginConfig>();

  [Header("Origin Assignment Strategy")]
  public OriginAssignmentStrategy origin_assignment_strategy = OriginAssignmentStrategy.CLOSEST;

  [Header("Interceptor Swarm Configurations")]
  public List<SwarmConfig> interceptor_swarm_configs = new List<SwarmConfig>();

  [Header("Threat Swarm Configurations")]
  public List<SwarmConfig> threat_swarm_configs = new List<SwarmConfig>();
}

[Serializable]
public class DynamicConfig {
  public LaunchConfig launch_config;
  public SensorConfig sensor_config;
  public FlightConfig flight_config;
}

[Serializable]
public class SwarmConfig {
  public int num_agents;
  public DynamicAgentConfig dynamic_agent_config;

  /// <summary>
  /// Optional origin ID for manual origin assignment.
  /// When specified and using MANUAL assignment strategy, interceptors will be assigned to this origin.
  /// If null or empty, the configured assignment strategy will be used.
  /// </summary>
  public string origin_id;
}

[Serializable]
public class DynamicAgentConfig {
  public string name;
  public string agent_model;
  public string attack_behavior;
  public InitialState initial_state;
  public StandardDeviation standard_deviation;
  public DynamicConfig dynamic_config;
  public PlottingConfig plotting_config;
  public SubmunitionsConfig submunitions_config;

  public static DynamicAgentConfig FromSubmunitionDynamicAgentConfig(
      SubmunitionDynamicAgentConfig submunitionConfig) {
    return new DynamicAgentConfig {
      agent_model = submunitionConfig.agent_model, initial_state = submunitionConfig.initial_state,
      standard_deviation = submunitionConfig.standard_deviation,
      dynamic_config = submunitionConfig.dynamic_config,
      plotting_config = submunitionConfig.plotting_config,
      // Set other fields as needed, using default values if not present in
      // SubmunitionDynamicAgentConfig
      submunitions_config = null,  // Or a default value if needed
    };
  }
}

[Serializable]
public class InitialState {
  public Vector3 position;
  public Vector3 rotation;
  public Vector3 velocity;
}

[Serializable]
public class StandardDeviation {
  public Vector3 position;
  public Vector3 velocity;
}

[Serializable]
public class LaunchConfig {
  public float launch_time;
}

[Serializable]
public class PlottingConfig {
  public ConfigColor color;
  public LineStyle linestyle;
  public Marker marker;
}

[Serializable]
public class SubmunitionsConfig {
  public int num_submunitions;
  public double dispense_time;
  public SubmunitionDynamicAgentConfig dynamic_agent_config;
}

[Serializable]
public class SubmunitionDynamicAgentConfig {
  public string agent_model;
  public InitialState initial_state;
  public StandardDeviation standard_deviation;
  public DynamicConfig dynamic_config;
  public PlottingConfig plotting_config;
}

[Serializable]
public class SensorConfig {
  public SensorType type;
  public float frequency;
}

[Serializable]
public class FlightConfig {
  public bool augmentedPnEnabled;
  public bool evasionEnabled;
  public float evasionRangeThreshold;
}

[Serializable]
public class ThreatConfig {
  public AgentClass threat_class;
  public InitialState initial_state;
  public PlottingConfig plotting_config;
  public string prefabName;
}

// Enums

[JsonConverter(typeof(StringEnumConverter))]
public enum AgentClass { NONE, FIXEDWING, ROTARYWING, BALLISTIC }
[JsonConverter(typeof(StringEnumConverter))]
public enum ConfigColor { BLUE, GREEN, RED }
[JsonConverter(typeof(StringEnumConverter))]
public enum LineStyle { DOTTED, SOLID }
[JsonConverter(typeof(StringEnumConverter))]
public enum Marker { TRIANGLE_UP, TRIANGLE_DOWN, SQUARE }
[JsonConverter(typeof(StringEnumConverter))]
public enum SensorType { IDEAL }
