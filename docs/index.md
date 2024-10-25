---
# https://vitepress.dev/reference/default-theme-home-page
layout: home

hero:
  name: "micromissiles-unity"
  
  tagline: "Swarm-on-swarm simulator using micromissiles for point defense"
  actions:
    - theme: brand
      text: Documentation
      link: /Simulator_Overview
    - theme: alt
      text: Development Guide
      link: /Development_Guide

---

# Quick Start

We generate pre-built standalone binaries for Windows and Mac users from the `release` branch. These binaries are intended for non-development users who just want to run the application and modify a few configurations along the way.

You can find the latest release [here](https://github.com/PisterLab/micromissiles-unity/releases/latest).

## Windows

1. Download the zip file for Windows: `micromissiles-<version>-windows_x86_64.zip`.
2. Unzip the zip file. The zip file should contain a single directory called `micromissiles-<version>-windows_x86_64`.
3. In the `micromissiles-<version>-windows_x86_64` directory, run `micromissiles-<version>-StandaloneWindows64.exe`.

## Mac

1. Download the tarball file for Darwin: `micromissiles-<version>-darwin.tar.gz`.
2. Untar the tarball. The tarball should contain a single directory called `micromissiles-<version>-darwin`.
3. In the `micromissiles-<version>-darwin` directory, run the app file.


# Next Steps

- Familiarize yourself with the [**Keybinds and Controls**](Keybinds_and_Controls.md) to navigate and interact with the simulation.
- Learn how to configure the simulation settings by reading the [**Simulation Configuration Guide**](Simulation_Config_Guide.md).
