# Simulation Configuration Guide

This guide provides instructions on how to configure the simulation by editing the configuration files. You can customize interceptor and threat behaviors, simulation parameters, and more to suit your needs.

## Configuration Files

The main configuration files you will work with are located in the `Assets/StreamingAssets/Configs/` directory. In a deployment context, these files are located in the `micromissiles_Data/StreamingAssets/Configs/` directory.

- **Simulation Configurations**:
  - **`1_salvo_1_hydra_7_drones.json`**: A simple, barebones example of a simulation configuration featuring a single salvo in a 7-on-7 scenario.
  - **`3_salvo_10_hydra_200_drones.json`**: A more complex example with three salvos, illustrating a 210-on-200 scenario. This demonstrates how to set up multiple salvos within the simulation.
  - **C# Script**: [`SimulationConfig.cs`](https://github.com/PisterLab/micromissiles-unity/blob/master/Assets/Scripts/Config/StaticAgentConfig.cs)

- **Model Configurations** (found in `Assets/StreamingAssets/Configs/Models/`):
  - **`micromissile.json`**
  - **`hydra70.json`**
  - **`drone.json`**
  - **C# Script**: [`StaticAgentConfig.cs`](https://github.com/PisterLab/micromissiles-unity/blob/master/Assets/Scripts/Config/StaticAgentConfig.cs)

### File Locations

Development context:
- **Simulation Configurations**: `Assets/StreamingAssets/Configs/`
- **Model Configurations**: `Assets/StreamingAssets/Configs/Models/`

Deployment context:
- **Simulation Configurations**: `micromissiles_Data/StreamingAssets/Configs/`
- **Model Configurations**: `micromissiles_Data/StreamingAssets/Configs/Models/`

## Overview of Simulation Configurations

### Simulation Configuration Examples

The simulation configurations are defined in JSON files that specify the initial setup for missiles and targets.

#### `1_salvo_1_hydra_7_drones.json`

This is a basic configuration featuring a single salvo with one interceptor type (`HYDRA_70`) and seven threat drones.

```json
{
  "timeScale": 1,
  "interceptor_swarm_configs": [
    {
      "num_agents": 1,
      "dynamic_agent_config": {
        "interceptor_type": "HYDRA_70",
        "initial_state": {
          "position": { "x": 0, "y": 20, "z": 0 },
          "rotation": { "x": -45, "y": 0, "z": 0 },
          "velocity": { "x": 0, "y": 10, "z": 10 }
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
          "launch_config": { "launch_time": 4 },
          "dynamic_agent_config": {
            "interceptor_type": "MICROMISSILE",
            // Submunition configuration...
          }
        }
      }
    }
  ],
  "threat_swarm_configs": [
    {
      "num_agents": 7,
      "dynamic_agent_config": {
        "threat_type": "DRONE",
        "initial_state": {
          "position": { "x": 0, "y": 600, "z": 6000 },
          "rotation": { "x": 90, "y": 0, "z": 0 },
          "velocity": { "x": 0, "y": 0, "z": -50 }
        },
        // Other threat configurations...
      }
    }
  ]
}
```

#### `3_salvo_10_hydra_200_drones.json`

This configuration demonstrates a more complex scenario with three salvos, each launching ten `HYDRA_70` missiles at different times against 200 threat drones. This results in a total of 210 missiles (including submunitions) engaging 200 targets.

```json
{
  "timeScale": 1,
  "interceptor_swarm_configs": [
    {
      "num_agents": 10,
      "dynamic_agent_config": {
        "interceptor_type": "HYDRA_70",
        "initial_state": {
          "position": { "x": 0, "y": 20, "z": 0 },
          "rotation": { "x": -45, "y": 0, "z": 0 },
          "velocity": { "x": 0, "y": 10, "z": 10 }
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
          "launch_config": { "launch_time": 4 },
          "dynamic_agent_config": {
            "interceptor_type": "MICROMISSILE",
            // Submunition configuration...
          }
        }
      }
    },
    // Two more similar interceptor_swarm_configs with different launch times...
  ],
  "threat_swarm_configs": [
    {
      "num_agents": 200,
      "dynamic_agent_config": {
        "threat_type": "DRONE",
        "initial_state": {
          "position": { "x": 0, "y": 600, "z": 6000 },
          "rotation": { "x": 90, "y": 0, "z": 0 },
          "velocity": { "x": 0, "y": 0, "z": -50 }
        },
        // Other threat configurations...
      }
    }
  ]
}
```

**Key Differences Between the Examples**:

The key difference between the examples is that the **Number of Salvos** in `3_salvo_10_hydra_200_drones.json` file includes multiple salvos by adding multiple entries in the `interceptor_swarm_configs` array, each with its own `launch_time`.

**Achieving Multiple Salvos**:

Multiple salvos are achieved by:

- Adding multiple configurations in the `interceptor_swarm_configs` array.
- Specifying different `launch_time` values in the `launch_config` of the `dynamic_config` for each salvo to control when they launch.

### Key Configuration Parameters

- **`timeScale`**: Adjusts the speed of the simulation.
- **`interceptor_swarm_configs`**: Contains settings for interceptor swarms. Each entry represents a salvo.
- **`threat_swarm_configs`**: Contains settings for threat swarms.

#### Within Each Swarm Configuration

- **`num_agents`**: Number of agents (missiles or targets) in the swarm.
- **`dynamic_agent_config`**: Settings for each agent, including:

  - **`interceptor_type`** / **`threat_type`**: Defines the type of interceptor or threat.
  - **`initial_state`**: Sets the starting position, rotation, and velocity.
  - **`standard_deviation`**: Adds random noise to initial states for variability.
  - **`dynamic_config`**: Time-dependent settings like launch configurations, sensor configurations, and in-flight configurations.
  - **`submunitions_config`**: Details for any submunitions (e.g., micromissiles deployed by a larger interceptor).

### Adding or Modifying Agents

1. **Add a New Swarm Configuration**:

   To introduce a new interceptor or threat swarm (or an additional salvo), create a new entry in `interceptor_swarm_configs` or `threat_swarm_configs`.

   ```json
   {
     "num_agents": 5,
     "dynamic_agent_config": {
       "interceptor_type": "MICROMISSILE",
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

## Model Configurations

The model configurations define the physical and performance characteristics of interceptor and threat models. The default models provided can be customized to suit your research needs.

### Available Models

The `Models` directory contains the following default model configurations:

- **`micromissile.json`**
- **`hydra70.json`**
- **`drone.json`**
- **`srfj.json`**

These JSON files serve as templates and can be tweaked to modify the behavior of the corresponding models.

### Editing Model Configurations

#### Example: `micromissile.json`

This file defines parameters for the micromissile model.

```json:Assets/StreamingAssets/Configs/Models/micromissile.json
{
  "accelerationConfig": {
    "maxReferenceNormalAcceleration": 300,
    "referenceSpeed": 1000,
    "maxForwardAcceleration": 0
  },
  "boostConfig": {
    "boostTime": 0.3,
    "boostAcceleration": 350
  },
  "liftDragConfig": {
    "liftCoefficient": 0.2,
    "dragCoefficient": 0.7,
    "liftDragRatio": 5
  },
  // Other configurations...
}
```

**Configurable Parameters**:

- **`accelerationConfig`**: Controls acceleration characteristics.
- **`boostConfig`**: Settings for the boost phase of the craft.
- **`liftDragConfig`**: Aerodynamic properties.
- **`bodyConfig`**: Physical attributes like mass and area.
- **`hitConfig`**: Collision detection and damage properties.

### Modifying Parameters

You can tweak the parameters in these model files to adjust performance. For example:

- **Increase Normal Acceleration**: Modify `maxReferenceNormalAcceleration` in `accelerationConfig`.
- **Change Mass**: Adjust the `mass` value in `bodyConfig`.
- **Alter Aerodynamics**: Tweak `liftCoefficient` and `dragCoefficient` in `liftDragConfig`.

### Adding New Models

To define a new inte or threat model:

1. **Create a New JSON File** in `Assets/StreamingAssets/Configs/Models/`.

2. **Define Model Parameters** similar to the existing model files.

3. **Update the Code** to recognize and load the new model if necessary.

**Note**: Ensure that any new parameters added to the model configuration are reflected in the corresponding C# classes.

## Relevant C# Scripts

### `SimulationConfig.cs`

This script defines the data structures used to interpret the JSON simulation configuration files.

[Assets/Scripts/Config/SimulationConfig.cs](https://github.com/PisterLab/micromissiles-unity/blob/master/Assets/Scripts/Config/SimulationConfig.cs)

**Classes**:

- `SimulationConfig`: Contains all simulation settings.
- `SwarmConfig`: Represents a group of agents (missiles or targets).
- `DynamicAgentConfig`: Configuration for individual agents.

**Enums**:

- `InterceptorType`, `ThreatType`, and `SensorType` define available types.

### `StaticAgentConfig.cs`

This script defines the classes corresponding to the model configuration JSON structure.

[Assets/Scripts/Config/StaticAgentConfig.cs](https://github.com/PisterLab/micromissiles-unity/blob/master/Assets/Scripts/Config/StaticAgentConfig.cs)

For example:
```csharp
[Serializable]
public class StaticAgentConfig {
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

  // Other configuration classes...

  public AccelerationConfig accelerationConfig;
  public BoostConfig boostConfig;
  public LiftDragConfig liftDragConfig;
  public BodyConfig bodyConfig;
  public HitConfig hitConfig;
}
```

**Updating Classes**:

If you add new parameters to the JSON model files, ensure the corresponding classes in `StaticAgentConfig.cs` are updated to include these new fields.

## Using the Deployment Build

When using the deployment build:

- **Include Required Configuration Files**: Ensure all necessary JSON configuration files are present in the `StreamingAssets/Configs/` directory.
- **Adjust Simulations Without Rebuilding**: Modify the JSON files to change simulation parameters without needing to rebuild the application.

While the simulation is running, you can load a new Simulation Configuration JSON file at runtime by opening the panel with 'L'.

---

**Note**: Always back up configuration files before making significant changes. Incorrect configurations can lead to simulation errors.

For further assistance, refer to the comments and documentation within the code files:

- [`SimManager.cs`](https://github.com/PisterLab/micromissiles-unity/blob/master/Assets/Scripts/SimManager.cs): Manages simulation state and agent creation.
- [`InputManager.cs`](https://github.com/PisterLab/micromissiles-unity/blob/master/Assets/Scripts/Managers/InputManager.cs): Handles user input and interactions.

---

*This guide aims to help you set up and customize the simulation project effectively. If you encounter any issues or have questions, please reach out to the project maintainers.*
```