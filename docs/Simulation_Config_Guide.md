---
outline: [2,4]
---

# Simulation Config Guide

## Table of Contents

[[toc]]


## Introduction

![Simulation config files](./images/agent_config_files.png)

In this guide, we will explore the different types of configuration files used in the simulation and how to modify them to customize your simulation scenarios.
You can customize interceptor and threat behaviors, simulation parameters, and more to suit your needs.
There are three main types of configuration files:

1. **Simulation Configurations** define the overall setup of the simulation, including agents, their quantities, initial states, and behaviors.
2. **Static Agent Configurations (Agent Models)** define the physical and performance characteristics of individual agent types (e.g., mass, acceleration, aerodynamic properties).
3. **Attack Behavior Configurations** define the behavior of threat agents during the simulation (e.g., movement patterns, attack strategies).

Understanding these configurations will enable you to set up complex simulations that meet your specific requirements.

> [!DANGER]
> Always back up configuration files before making significant changes. Incorrect configurations can lead to simulation errors.


## Configuration Files

The main configuration files you will work with are located in the `Assets/StreamingAssets/Configs/` directory.

> [!NOTE]
> In a deployment context (i.e. you downloaded the binary release from the [releases page](https://github.com/PisterLab/micromissiles-unity/releases)), these files are located in the `micromissiles_Data/StreamingAssets/Configs/` directory.
>
> On Windows, this directory is co-located with the executable.
>
> On MacOS, you need to right click the `micromissiles-v*.*` Application > `Show Package Contents` > `Contents/Resources/Data/StreamingAssets/Configs/`.

Several example configuration files are provided to help you get started.

```bash
Configs/
├── Simulation Configurations
│   ├── 1_salvo_1_hydra_7_drones.json        # Simple 7v7 scenario
│   ├── 1_salvo_3_sfrj_30_ascm.json          # Anti-ship missile defense
│   ├── 1_salvo_4_hydra_10_brahmos.json      # Cruise missile defense
│   ├── 2_salvo_4_hydra_7_quad_15_ucav.json  # Mixed threat scenario
│   └── 3_salvo_10_hydra_200_drones.json     # Large-scale drone swarm
│   └── 3_salvo_6_sfrj_50_fateh110b.json     # Large-scale ballistic missile defense
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
│       └── {YOUR NEW THREAT}.json            # New threat model
│
└── Behaviors/
    └── Attack/
        ├── default_direct_attack.json       # Standard attack profile
        ├── brahmos_direct_attack.json       # Cruise missile profile
        └── fateh110b_direct_attack.json     # Ballistic missile profile
```

| **Scenario Type**          | **File Name**                        | **Description**                                      |
|---------------------------|--------------------------------------|------------------------------------------------------|
| **Basic Scenarios**       | `1_salvo_1_hydra_7_drones.json`      | Simple 7v7 scenario with one carrier rocket          |
|                          | `2_salvo_4_hydra_7_quad_15_ucav.json` | Mixed threat scenario with drones and UCAVs          |
| **Large-Scale Scenarios** | `3_salvo_10_hydra_200_drones.json`   | Large-scale defense against drone swarm (210v200)    |
|                          | `3_salvo_6_sfrj_50_fateh110b.json`    | Large-scale ballistic missile defense (72v50)        |
| **Advanced Threats**     | `1_salvo_4_hydra_10_brahmos.json`     | Defense against supersonic cruise missiles           |
|                          | `1_salvo_3_sfrj_30_ascm.json`         | Anti-ship missile defense scenario                   |

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
> While the simulation is running, you can load a new Simulation Configuration JSON file at runtime by opening the panel with 'L'.
>
> The simulator will detect any new or updated files in the `StreamingAssets/Configs/` directory and present them in the dropdown menu.

### Structure

A simulation configuration file is structured as follows:

- **`timeScale`**: Adjusts the speed of the simulation.
- **`endTime`**: Specifies when the simulation should end.
- **`interceptor_swarm_configs`**: An array of interceptor swarms with their configurations.
- **`threat_swarm_configs`**: An array of threat swarms with their configurations.

Each swarm configuration includes:

- **`num_agents`**: Number of agents in the swarm.
- **`dynamic_agent_config`**: Configuration for agents, including:
  - **`agent_model`**: Reference to a static agent configuration file.
  - **`attack_behavior`** (for threats): Reference to an attack behavior configuration file.
  - **`initial_state`**: Starting position, rotation, and velocity.
  - **`standard_deviation`**: Variability in initial states.
  - **`dynamic_config`**: Time-dependent settings (launch times, sensor configurations, flight configurations).
  - **`submunitions_config`** (for interceptors): Configuration for any submunitions deployed.


### Examples

The simulation configurations are defined in JSON files that specify the initial setup for missiles and targets.

When preparing your own simulation configuration, it is important to keep in mind that small missiles have limited
range. In the video below, notice the difference in successful intercepts of an evading target when
the micromissiles are launched at 2 km vs 6 km from the incoming threats

![Short range intercept ](./images/short_range_interceptors.gif)

#### Example 1: `1_salvo_1_hydra_7_drones.json`

A simple simulation with one interceptor (`Hydra 70`) and seven threat drones.

```json
{
  "endTime": 300,
  "timeScale": 1,
  "interceptor_swarm_configs": [
    {
      "num_agents": 1,
      "dynamic_agent_config": {
        "agent_model": "hydra70.json",
        "initial_state": {
          "position": { "x": 0, "y": 20, "z": 0 },
          "velocity": { "x": 0, "y": 10, "z": 10 }
        },
        "standard_deviation": {
          "position": { "x": 10, "y": 0, "z": 10 },
          "velocity": { "x": 5, "y": 0, "z": 1 }
        },
        "dynamic_config": {
          "launch_config": { "launch_time": 0 },
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
          "dispense_time": 4,
          "dynamic_agent_config": {
            "agent_model": "micromissile.json",
            "initial_state": { /* ... */ },
            "standard_deviation": { /* ... */ },
            "dynamic_config": { /* ... */ }
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
        "initial_state": { /* ... */ },
        "standard_deviation": { /* ... */ },
        "dynamic_config": { /* ... */ }
      }
    }
  ]
}
```

**Core Settings:**
- `endTime: 300` - Sets a 5-minute simulation duration
- `timeScale: 1` - Runs in real-time (increase for faster simulation)

**Interceptor Configuration (`interceptor_swarm_configs`):**
- Deploys 1 Hydra-70 rocket with:
  - Initial position and velocity defined in `initial_state`
  - Random variations added via `standard_deviation` (SimManager adds this noise on agent creation)
  - Sensor settings: Uses ideal sensor at 100Hz update rate
  - Submunitions capability: Releases 7 micromissiles at t=4 seconds

> [!IMPORTANT]
> The initial rotation of an agent is determined by the initial velocity vector.
>
> Reminder: +X is right, +Y is up, +Z is forward.

**Threat Configuration (`threat_swarm_configs`):**
- Deploys 7 drones with:
  - Uses quadcopter model and direct attack behavior
  - Initial positions/velocities also get random variations
  - Each drone follows attack_behavior defined in "default_direct_attack.json"


#### Example 2: `3_salvo_2_sfrj_5_fateh110b.json`

A complex simulation that involves multiple solid fuel ramjet interceptor salvos launched at different times to engage a large number of ballistic missile threats.
This scenario demonstrates a layered defense approach against a large-scale ballistic missile attack.
The staggered launch times and varied dispense times help ensure continuous defensive coverage as the threats approach their targets.

```json
{
  "endTime": 300,
  "timeScale": 2,
  "interceptor_swarm_configs": [
    {
      "num_agents": 2,
      "dynamic_agent_config": {
        "agent_model": "sfrj.json",
        "initial_state": {
          "position": { "x": 0, "y": 20, "z": 0 },
          "velocity": { "x": 0, "y": 450, "z": 180 }
        },
        // ... other config ...
        "submunitions_config": {
          "num_submunitions": 12,
          "dispense_time": 34,
          "dynamic_agent_config": {
            "agent_model": "micromissile.json"
            // ... micromissile config ...
          }
        }
      }
    },
    {
      "num_agents": 2,
      "dynamic_agent_config": {
        "agent_model": "sfrj.json",
        "initial_state": {
          "position": { "x": 0, "y": 20, "z": 0 },
          "velocity": { "x": 0, "y": 400, "z": 200 }
        },
        // ... other config ...
        "dynamic_config": {
          "launch_config": { "launch_time": 8 }
          // ... other dynamic config ...
        },
        "submunitions_config": {
          "num_submunitions": 12,
          "dispense_time": 36,
          "dynamic_agent_config": {
            "agent_model": "micromissile.json"
            // ... micromissile config ...
          }
        }
      }
    },
    {
      "num_agents": 2,
      "dynamic_agent_config": {
        "agent_model": "sfrj.json",
        "initial_state": {
          "position": { "x": 0, "y": 20, "z": 0 },
          "velocity": { "x": 0, "y": 440, "z": 280 }
        },
        // ... other config ...
        "dynamic_config": {
          "launch_config": { "launch_time": 22 }
          // ... other dynamic config ...
        },
        "submunitions_config": {
          "num_submunitions": 12,
          "dispense_time": 40,
          "dynamic_agent_config": {
            "agent_model": "micromissile.json"
            // ... micromissile config ...
          }
        }
      }
    }
  ],
  "threat_swarm_configs": [
    {
      "num_agents": 50,
      "dynamic_agent_config": {
        "agent_model": "fateh110b.json",
        "attack_behavior": "fateh110b_direct_attack.json",
        "initial_state": {
          "position": { "x": 0, "y": 100000, "z": 100000 },
          "velocity": { "x": 0, "y": -2317, "z": -2317 }
        },
        "standard_deviation": {
          "position": { "x": 3000, "y": 1000, "z": 1000 },
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
        }
      }
    }
  ]
}
```

This simulation involves **three salvos** of SFRJ interceptors, each launched at different times to provide overlapping interception windows against a large number of ballistic missile threats.

  - **Salvo 1: Launch Time = 0 seconds**
    - **Number of Agents:** 2 SFRJs
    - **Initial Velocity:** `{ "x": 0, "y": 450, "z": 180 }` meters/second
    - **Dispense Time:** 34 seconds
    - **Submunitions:** Each SFRJ releases 12 micromissiles with augmented proportional navigation enabled

  - **Salvo 2: Launch Time = 8 seconds**
    - **Number of Agents:** 2 SFRJs
    - **Initial Velocity:** `{ "x": 0, "y": 400, "z": 200 }` meters/second
    - **Dispense Time:** 36 seconds
    - **Submunitions:** Each SFRJ releases 12 micromissiles

  - **Salvo 3: Launch Time = 22 seconds**
    - **Number of Agents:** 2 SFRJs
    - **Initial Velocity:** `{ "x": 0, "y": 440, "z": 280 }` meters/second
    - **Dispense Time:** 40 seconds
    - **Submunitions:** Each SFRJ releases 12 micromissiles

The SFRJs are initialized with substantial velocities (`y: 300`, `z: 200`) to compensate for their lower acceleration rates.
Remember this also determines the initial orientation of the agent. It helps to set the initial velocity to point the agent
towards the threat.

> [!TIP]
> SFRJs have extended propulsion phases, this requires attention to the initial velocity settings to ensure they reach effective interception velocities and do not "topple over" during initial acceleration.

**Ballistic Missile Threats (`fateh110b`):**
  - **Number of Agents:** 50 ballistic missiles
  - **Initial Position:** `{ "x": 0, "y": 100000, "z": 100000 }` meters (high-altitude)
  - **Initial Velocity:** `{ "x": 0, "y": -2317, "z": -2317 }` meters/second
  - **Terminal Navigation & Guidance Phase:** These missiles are initialized assuming they've entered their terminal phase, characterized by high velocities and primarily influenced by gravity (`only gravity acceleration`).
  - Includes terminal evasive maneuvers within 1km of targets

> [!TIP]
> Ballistic missile threats are best modeled assuming they've entered their terminal phase, characterized by high velocities and primarily influenced by gravity (and drag)
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
         "launch_config": { "launch_time": 15 },
         // Other dynamic settings...
       }
     }
   }
   ```

   - **`launch_time`** in `launch_config` controls when this swarm (or salvo) is deployed.

2. **Modify Existing Configurations**:

   Adjust parameters like `num_agents`, `initial_state`, or `dynamic_config` to change the behavior of existing agents or salvos.

### Relevant C# Scripts

The [Assets/Scripts/Config/SimulationConfig.cs](https://github.com/PisterLab/micromissiles-unity/blob/master/Assets/Scripts/Config/SimulationConfig.cs) script defines the
data structures used to interpret the JSON simulation configuration files.

## Static Agent Configurations / Agent "Models"

The model configurations define the physical and performance characteristics of interceptor and threat models. The default models provided can be customized to suit your simulation goals.

> [!IMPORTANT]
> To understand the impact of aerodynamics/flight model parameters, refer to the [Simulator Physics section of the Simulator Overview](Simulator_Overview.md#simulator-physics).

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
  - No forward acceleration (`maxForwardAcceleration`: 0) - relies on initial boost
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
> This parameter is crucial as it defines the likelihood of the drone being destroyed when hit.

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

---

*This guide aims to help you set up and customize the simulation project effectively. If you encounter any issues or have questions, please reach out to the project maintainers.*