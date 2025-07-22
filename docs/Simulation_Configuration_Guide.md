# Simulation Configuration Guide

In this guide, we will explore the different types of configuration files used in the simulation and how to modify them to customize your simulation scenarios.

## Table of Contents

[[toc]]

## Introduction

![Simulation configuration files](./images/agent_configuration_files.png){width=80%}

You can customize interceptor and threat behaviors, simulation parameters, and more to suit your needs.
There are three main types of configuration files:

1. **Simulation Configurations** define the overall setup of the simulation, including agents, their quantities, initial states, and behaviors.
2. **Static Agent Configurations (Agent Models)** define the physical and performance characteristics of individual agent types (e.g., mass, acceleration, aerodynamic properties).
3. **Attack Behavior Configurations** define the behavior of threat agents during the simulation (e.g., movement patterns, attack strategies).

Understanding these configurations will enable you to set up complex simulations that meet your specific requirements.

> [!WARNING]
> Always back up configuration files before making significant changes. Incorrect configurations can lead to simulation errors.

## Configuration Files

The main configuration files you will work with are located in the `Assets/StreamingAssets/Configs/` directory.

> [!NOTE]
> In a deployment context (i.e. you downloaded the binary release from the [releases page](https://github.com/PisterLab/micromissiles-unity/releases)), these files are located in the `micromissiles_Data/StreamingAssets/Configs/` directory.
>
> On Windows and Linux, this directory is located alongside the executable.
>
> On MacOS, you need to right-click the `micromissiles-v*.*` Application > `Show Package Contents` > `Contents/Resources/Data/StreamingAssets/Configs/`.

Several example configuration files are provided to help you get started.

```bash
Configs/
├── Simulation Configurations
│   ├── 7_quadcopters.json                   # Seven incoming quadcopter drones
│   ├── 7_ucav.json                          # Seven incoming combat drones
│   ├── 7_brahmos.json                       # Seven incoming supersonic cruise missiles
│   ├── 5_swarms_7_quadcopters.json          # Five incoming swarms of seven incoming quadcopter drones each
│   ├── 5_swarms_7_ucav.json                 # Five incoming swarms of seven incoming combat drones each
│   ├── 5_swarms_7_brahmos.json              # Five incoming swarms of seven incoming supersonic cruise missiles each
│   ├── 5_swarms_100_ucav.json               # 100 incoming combat drones stacked into five swarms
│   ├── 5_swarms_500_ucav.json               # 500 incoming combat drones stacked into five swarms
│   └── 7_quadcopters_15_ucav.json           # Seven quadcopters followed by 15 combat drones
│
├── Models/
│   ├── Interceptors/
│   │   ├── hydra70.json                     # Carrier rocket
│   │   ├── micromissile.json                # Main micromissile interceptor
│   │   └── sfrj.json                        # Solid fuel ramjet carrier missile
│   │
│   └── Threats/
│       ├── ascm.json                        # Anti-ship cruise missile
│       ├── brahmos.json                     # Supersonic cruise missile
│       ├── fateh110b.json                   # Tactical ballistic missile
│       ├── quadcopter.json                  # Simple quadcopter drone
│       ├── ucav.json                        # Simple fixed-wing combat drone
│       └── {YOUR NEW THREAT}.json           # New threat model
│
└── Behaviors/
    └── Attack/
        ├── default_direct_attack.json       # Standard attack profile
        ├── brahmos_direct_attack.json       # Cruise missile profile
        └── fateh110b_direct_attack.json     # Ballistic missile profile
```

### File Locations

Development context:
- **Simulation Configurations**: `Assets/StreamingAssets/Configs/`
- **Model Configurations**: `Assets/StreamingAssets/Configs/Models/`
- **Behaviors**: `Assets/StreamingAssets/Configs/Behaviors/`

Deployment context:
- **Simulation Configurations**: `micromissiles_Data/StreamingAssets/Configs/`
- **Model Configurations**: `micromissiles_Data/StreamingAssets/Configs/Models/`
- **Behaviors**: `micromissiles_Data/StreamingAssets/Configs/Behaviors/`

## Simulation Configurations

These files dictate how the simulation initializes and runs. They specify the agents involved, their initial conditions, and their behaviors.

> [!IMPORTANT]
> While the simulation is running, you can load a new Simulation Configuration JSON file at runtime by opening the panel with `L`.
>
> The simulator will detect any new or updated files in the `StreamingAssets/Configs/` directory and present them in the dropdown menu.

### Structure

A simulation configuration file is structured as follows:

- **`timeScale`**: Adjusts the speed of the simulation.
- **`endTime`**: Specifies when the simulation should end.
- **`interceptor_swarm_configs`**: An array of interceptor swarms with their configurations.
- **`threat_swarm_configs`**: An array of threat swarms with their configurations.

Each swarm configuration includes:

- **`num_agents`** (for threats): Number of agents in the swarm.
- **`dynamic_agent_config`**: Configuration for agents, including:
  - **`agent_model`**: Reference to a static agent configuration file.
  - **`attack_behavior`** (for threats): Reference to an attack behavior configuration file.
  - **`initial_state`** (for threats): Starting position, rotation, and velocity.
  - **`standard_deviation`** (for threats): Variability in initial states.
  - **`dynamic_config`**: Time-dependent settings (sensor configurations, flight configurations).
  - **`submunitions_config`** (for interceptors): Configuration for any submunitions deployed.
    - **`num_submunitions`**: Number of submunitions that each interceptor in the swarm will release.

Note that the number of agents in the swarm, the initial states, and the launch times are not specified for interceptor swarms.
The IADS will automatically determine how many interceptors to launch and when to launch them against the incoming threats by [clustering](./Simulator_Overview.md#clustering) them.
The carrier interceptors will then independently determine [when to release the submunitions](./Simulator_Overview.md#submunitions-release).

### Examples

The simulation configurations are defined in JSON files that specify the initial setup for missiles and targets.

#### Example 1: `7_quadcopters.json`

A simple simulation with one interceptor type (Hydra 70) launched against seven quadcopter threats.

```json
{
  "endTime": 300,
  "timeScale": 1,
  "interceptor_swarm_configs": [
    {
      "dynamic_agent_config": {
        "agent_model": "hydra70.json",
        "dynamic_config": {
          "sensor_config": {
            "type": "IDEAL",
            "frequency": 100
          },
          "flight_config": {
            "augmentedPnEnabled": false
          }
        },
        "submunitions_config": {
          "num_submunitions": 7,
          "dynamic_agent_config": {
            "agent_model": "micromissile.json",
            "dynamic_config": {
              "sensor_config": {
                "type": "IDEAL",
                "frequency": 100
              },
              "flight_config": {
                "augmentedPnEnabled": false
              }
            }
          }
        }
      }
    }
  ],
  "threat_swarm_configs": [
    {
      "num_agents": 7,
      "dynamic_agent_config": {
        "agent_model": "quadcopter.json",
        "attack_behavior": "default_direct_attack.json",
        "initial_state": {
          "position": { "x": 0, "y": 800, "z": 6000 },
          "velocity": { "x": 0, "y": 0, "z": -50 }
        },
        "standard_deviation": {
          "position": { "x": 1000, "y": 200, "z": 100 },
          "velocity": { "x": 0, "y": 0, "z": 25 }
        },
        "dynamic_config": {
          "launch_config": { "launch_time": 0 },
          "sensor_config": {
            "type": "IDEAL",
            "frequency": 100
          },
          "flight_config": {
            "evasionEnabled": true,
            "evasionRangeThreshold": 1000
          }
        },
        "submunitions_config": {
          "num_submunitions": 0
        }
      }
    }
  ]
}
```

**Core Settings:**
- `endTime: 300` - Sets a 5-minute simulation duration
- `timeScale: 1` - Runs in real-time (increase for faster simulation)

**Interceptor Configuration (`interceptor_swarm_configs`):**
- Deploys Hydra 70 rockets with:
  - Sensor settings: Uses ideal sensor at an update frequency of 100 Hz
  - Submunitions capability: Releases 7 micromissiles

**Threat Configuration (`threat_swarm_configs`):**
- Deploys 7 quadcopter drones with:
  - Uses quadcopter model and direct attack behavior
  - Initial positions/velocities are randomly perturbed
  - Each drone follows attack_behavior defined in `default_direct_attack.json`

#### Example 2: `5_swarms_7_brahmos.json`

A complex scenario that involves multiple swarms of supersonic cruise missiles that are spaced 5 km apart.
This scenario demonstrates how the IADS will stagger the interceptor launch times to defeat the layered threat swarms.

```json
{
  "endTime": 300,
  "timeScale": 1,
  "interceptor_swarm_configs": [
    {
      "dynamic_agent_config": {
        "agent_model": "hydra70.json",
        "dynamic_config": {
          "sensor_config": {
            "type": "IDEAL",
            "frequency": 100
          },
          "flight_config": {
            "augmentedPnEnabled": false
          }
        },
        "submunitions_config": {
          "num_submunitions": 7,
          "dynamic_agent_config": {
            "agent_model": "micromissile.json",
            "dynamic_config": {
              "sensor_config": {
                "type": "IDEAL",
                "frequency": 100
              },
              "flight_config": {
                "augmentedPnEnabled": false
              }
            }
          }
        }
      }
    }
  ],
  "threat_swarm_configs": [
    {
      "num_agents": 7,
      "dynamic_agent_config": {
        "agent_model": "brahmos.json",
        "attack_behavior": "brahmos_direct_attack.json",
        "initial_state": {
          "position": { "x": 0, "y": 50, "z": 30000 },
          "velocity": { "x": 0, "y": 0, "z": -800 }
        },
        // Threat configurations...
      }
    },
    {
      "num_agents": 7,
      "dynamic_agent_config": {
        "agent_model": "brahmos.json",
        "attack_behavior": "brahmos_direct_attack.json",
        "initial_state": {
          "position": { "x": 0, "y": 50, "z": 35000 },
          "velocity": { "x": 0, "y": 0, "z": -800 }
        },
        // Threat configurations...
      }
    },
    {
      "num_agents": 7,
      "dynamic_agent_config": {
        "agent_model": "brahmos.json",
        "attack_behavior": "brahmos_direct_attack.json",
        "initial_state": {
          "position": { "x": 0, "y": 50, "z": 40000 },
          "velocity": { "x": 0, "y": 0, "z": -800 }
        },
        // Threat configurations...
      }
    },
    {
      "num_agents": 7,
      "dynamic_agent_config": {
        "agent_model": "brahmos.json",
        "attack_behavior": "brahmos_direct_attack.json",
        "initial_state": {
          "position": { "x": 0, "y": 50, "z": 45000 },
          "velocity": { "x": 0, "y": 0, "z": -800 }
        },
        // Threat configurations...
      }
    },
    {
      "num_agents": 7,
      "dynamic_agent_config": {
        "agent_model": "brahmos.json",
        "attack_behavior": "brahmos_direct_attack.json",
        "initial_state": {
          "position": { "x": 0, "y": 50, "z": 50000 },
          "velocity": { "x": 0, "y": 0, "z": -800 }
        },
        // Threat configurations...
      }
    }
  ]
}
```

> [!TIP]
> SFRJs have extended propulsion phases, this requires attention to the initial velocity settings to ensure they reach effective interception velocities and do not "topple over" during initial acceleration.

> [!TIP]
> Ballistic missile threats are best modeled as already being in their terminal phase, characterized by high velocities and primarily influenced by gravity (and drag).
>
> This means the initial velocity is critically important to good simulation results of ballistic missile threats.

### Adding/Modifying Agents to a Simulation Configuration

1. **Add a New Swarm Configuration**:

   To introduce a new interceptor or threat swarm (or an additional salvo), create a new entry in `interceptor_swarm_configs` or `threat_swarm_configs`.

   ```json
   // ... previous interceptor_swarm_configs array entries ...
   {
     "num_agents": 5,
     "dynamic_agent_config": {
       "agent_model": "micromissile.json",
       // Additional configurations...
       "dynamic_config": {
         // Other dynamic configurations...
       }
     }
   }
   ```

2. **Modify Existing Configurations**:

   Adjust parameters like `num_agents`, `initial_state`, or `dynamic_config` to change the behavior of existing agents or salvos.

### Relevant C# Scripts

The [Assets/Scripts/Config/SimulationConfig.cs](https://github.com/PisterLab/micromissiles-unity/blob/master/Assets/Scripts/Config/SimulationConfig.cs) script defines the
data structures used to interpret the JSON simulation configuration files.

## Static Agent Configurations (Agent Models)

The model configurations define the physical and performance characteristics of interceptor and threat models. The default models provided can be customized to suit your simulation goals.

> [!IMPORTANT]
> To understand the impact of aerodynamics/flight model parameters, refer to the [Simulator Physics](./Simulator_Overview.md#physics).

### Structure

- **`name`**: Name of the agent.
- **`agentClass`**: Class/type of the agent (e.g., `MissileInterceptor`, `FixedWingThreat`).
- **`unitCost`**: Cost per unit. This is used in calculating the costs of all interceptors and threats in a simulation.
- **`accelerationConfig`**: Acceleration parameters.
- **`boostConfig`**: Boost phase characteristics.
- **`liftDragConfig`**: Aerodynamic properties.
- **`bodyConfig`**: Physical properties like mass and area.
- **`hitConfig`**: Collision detection parameters such as hit radius and probability of a kill once inside the kill radius.
- **`powerTable`** (for threats): Power settings for various flight modes.

### Available Models

The `Models` directory contains the following default model configurations:

| **Type**        | **Model**            | **Description**                                |
|-----------------|----------------------|------------------------------------------------|
| Interceptors    | `hydra70.json`       | Carrier rocket for deploying micromissiles     |
|                 | `micromissile.json`  | Primary Micromissile interceptor               |
|                 | `sfrj.json`          | Solid fuel ramjet carrier missile              |
| Threats         | `ascm.json`          | Anti-ship cruise missile                       |
|                 | `brahmos.json`       | Supersonic cruise missile                      |
|                 | `fateh110b.json`     | Tactical ballistic missile                     |
|                 | `quadcopter.json`    | Generic rotary-wing drone                      |
|                 | `ucav.json`          | Fixed-wing combat drone modeled roughly after the Shahed-136 |

These JSON files serve as templates of archetypes and can be tweaked to modify the behavior of the corresponding models.

### Examples

#### Example 1: `hydra70.json` (Carrier Interceptor)

```json
{
  "name": "Hydra 70",
  "agentClass": "CarrierInterceptor",
  "unitCost": 30000,
  "accelerationConfig": {
    "maxReferenceNormalAcceleration": 300,
    "referenceSpeed": 1000,
    "maxForwardAcceleration": 0
  },
  "boostConfig": {
    "boostTime": 1,
    "boostAcceleration": 100
  },
  "liftDragConfig": {
    "liftCoefficient": 0.2,
    "dragCoefficient": 1,
    "liftDragRatio": 5
  },
  "bodyConfig": {
    "mass": 15.8,
    "crossSectionalArea": 0.004,
    "finArea": 0.007,
    "bodyArea": 0.12
  }
}
```
`agentClass: "CarrierInterceptor"` - Identifies this as a carrier-type interceptor
- **unitCost**: 30000 - Cost per unit for simulation analysis

**Performance Characteristics:**

- **Acceleration Settings:**
  - High normal acceleration (300 m/s²) at reference speed
  - No forward acceleration (`maxForwardAcceleration: 0`) - relies on initial boost
  - Reference speed of 1000 m/s for scaling acceleration

- **Boost Phase:**
  - Short boost duration (1 second)
  - Strong initial acceleration (100 m/s²)

- **Aerodynamics:**
  - Moderate lift coefficient (0.2)
  - High drag coefficient (1.0)
  - Lift-to-drag ratio of 5

- **Physical Properties:**
  - Mass: 15.8 kg
  - Small cross-sectional area (0.004 m²)
  - Larger body area (0.12 m²) for stability

#### Example 2: `quadcopter.json` (RotaryWingThreat)

```json
{
  "name": "Quadcopter",
  "agentClass": "RotaryWingThreat",
  "unitCost": 1000,
  "accelerationConfig": {
    "maxReferenceNormalAcceleration": 300,
    "referenceSpeed": 1000,
    "maxForwardAcceleration": 50
  },
  "boostConfig": { "boostTime": 0 },
  "liftDragConfig": { "dragCoefficient": 0 },
  "bodyConfig": { "mass": 1 },
  "hitConfig": {
    "hitRadius": 1,
    "killProbability": 0.9
  },
  "powerTable": {
    "IDLE": 0,
    "LOW": 20,
    "CRUISE": 40,
    "MIL": 50,
    "MAX": 75
  }
}
```

This configuration represents a small drone threat:

**Core Parameters:**
- `agentClass`: "RotaryWingThreat" - Identifies this as a rotary-wing aircraft

**Performance Characteristics:**

**Acceleration Settings:**
- High maneuverability (300 m/s² normal acceleration)
- Moderate forward acceleration (50 m/s²)
- Reference speed of 1000 m/s

**Simplified Physics:**
- Quadcopters have no boost phase (`boostTime`: 0)
- No drag coefficient (simplified aerodynamics)
- Light mass (1 kg)

**Combat Properties:**
- 1-meter hit radius
- 90% kill probability when hit

> [!IMPORTANT]
> **Kill Probability:** Ensure the `killProbability` is set as desired in the threat model.
> This parameter is crucial as it defines the likelihood of the threat being destroyed when hit.

The power settings for threats determine their speed and maneuverability at different flight phases. These correspond to the power settings used in their attack behavior configurations.

### Modifying Parameters

You can tweak the parameters in these model files to adjust performance. For example:

- **Increase Normal Acceleration**: Modify `maxReferenceNormalAcceleration` in `accelerationConfig`.
- **Change Mass**: Adjust the `mass` value in `bodyConfig`.
- **Alter Aerodynamics**: Tweak `liftCoefficient` and `dragCoefficient` in `liftDragConfig`.

### Adding New Models

To define a new interceptor or threat model:

1. **Create a New JSON File** in `Assets/StreamingAssets/Configs/Models/`.

2. **Define Model Parameters** similar to the existing model files.

3. **Reference the New Model** in your simulation configuration files.

### Relevant C# Scripts

The [Assets/Scripts/Config/StaticConfig.cs](https://github.com/PisterLab/micromissiles-unity/blob/master/Assets/Scripts/Config/StaticConfig.cs) script defines the classes corresponding to the model configuration JSON structure.

**Classes**:

- `SimulationConfig`: Contains all simulation settings.
- `SwarmConfig`: Represents a group of agents (missiles or targets).
- `DynamicAgentConfig`: Configuration for individual agents.

**Enums**:

- `InterceptorType`, `ThreatType`, and `SensorType` define available types.

## Attack Behavior Configurations

These files define how threat agents behave during the simulation, specifying movement patterns and attack strategies.

### Structure

Key components include:

- **`name`**: Name of the attack behavior.
- **`attackBehaviorType`**: Type of attack (e.g., `DIRECT_ATTACK`). Must correspond to a valid and implemented attack behavior archetype.
- **`targetPosition`**: Coordinates of the initial target position. The threat will seek to destroy this target. Often this represents the "carrier" that is being defended.
- **`targetVelocity`**: Target's initial velocity (if applicable).
- **`flightPlan`**: Defines waypoints and strategies for the threat's attack.

### Examples

#### Example: `default_direct_attack.json`

```json
{
  "name": "DefaultDirectAttack",
  "attackBehaviorType": "DIRECT_ATTACK",
  "targetPosition": { "x": 0.01, "y": 0.0, "z": 0.0 },
  "targetVelocity": { "x": 0.0001, "y": 0.0, "z": 0.0 },
  "flightPlan": {
    "type": "DistanceToTarget",
    "waypoints": [
      {
        "distance": 10000.0,
        "altitude": 500.0,
        "power": "MIL"
      },
      {
        "distance": 4000.0,
        "altitude": 500.0,
        "power": "MIL"
      },
      {
        "distance": 2000.0,
        "altitude": 100.0,
        "power": "MAX"
      }
    ]
  }
}
```

This configuration defines a direct attack behavior where threats navigate toward a target position using waypoints based on distance-to-target.

1. The behavior uses a `DTTFlightPlan` (Distance-To-Target) with waypoints that specify:
   - `distance`: How far from the target this waypoint applies
   - `altitude`: The height to maintain at this distance
   - `power`: Engine power setting to use (`MIL` = Military Power, `MAX` = Maximum Power)

2. The waypoints are processed in descending distance order. In this example:
   - At 10,000m: Maintain 500m altitude at military power
   - At 4,000m: Continue at 500m altitude at military power
   - At 2,000m: Descend to 100m altitude and increase to maximum power for terminal attack

This creates a phased attack profile where threats:
1. Initially approach at a higher altitude with efficient power
2. Maintain altitude through mid-range
3. Make a final aggressive descent at maximum power for terminal attack

## Troubleshooting Guide

### Common Configuration Errors

1. **JSON Parsing Errors**
```json
{
  "interceptor_swarm_configs": [
    {
      "num_agents": 1,               // Common error: trailing comma
      "dynamic_agent_config": {},    // <- Remove this comma
    }
  ]
}
```
**Solution**: Remove trailing commas and validate JSON syntax using a tool like [JSONLint](https://jsonlint.com/)

2. **Missing Referenced Files**
```json
{
  "agent_model": "nonexistent_model.json",  // Error: File not found
  "attack_behavior": "missing_behavior.json"
}
```
**Solution**: Ensure all referenced files exist in the correct directories

3. **Invalid Parameter Values**
```json
{
  "timeScale": -1,  // Error: Must be positive
  "endTime": 0      // Error: Must be greater than 0
}
```
**Solution**: Check parameter constraints in the sections below

### Common Runtime Issues

1. **Interceptors Not Launching**
   - Check `launch_time` values
   - Verify `initial_state` positions are valid
   - Ensure `num_agents` > 0

2. **Unexpected Agent Behavior**
   - Validate `accelerationConfig` parameters
   - Check `powerTable` settings for threats
   - Review `sensor_config` settings

3. **Performance Issues**
   - Reduce `num_agents` in large scenarios
   - Adjust `sensor_config.frequency`
   - Lower `timeScale` if simulation is unstable

For further assistance, refer to the comments and documentation within the code files:

- [`SimManager.cs`](https://github.com/PisterLab/micromissiles-unity/blob/master/Assets/Scripts/SimManager.cs): Manages simulation state and agent creation.
- [`InputManager.cs`](https://github.com/PisterLab/micromissiles-unity/blob/master/Assets/Scripts/Managers/InputManager.cs): Handles user input and interactions.
