---
# https://vitepress.dev/reference/default-theme-home-page
layout: home

hero:
  name: "micromissiles-unity"
  
  tagline: "A large-scale air defense simulation platform for simulating swarm-on-swarm engagements with hierarchical interceptors and micromissiles"
  actions:
    - theme: brand
      text: Simulator Overview
      link: /Simulator_Overview
    - theme: alt
      text: Development Guide
      link: /Development_Guide

---

![Simulator view](./images/simulator_view.png)

# Quick Start

We generate pre-built standalone binaries for Windows and Mac users from the `release` branch. These binaries are intended for non-development users who just want to run the application and modify a few configurations along the way.

You can find the latest release [here](https://github.com/PisterLab/micromissiles-unity/releases/latest).

Follow the instructions below for your operating system to download and run the application.

### Windows

1. Download the zip file for Windows: `micromissiles-<version>-windows_x86_64.zip`.
2. Unzip the zip file. The zip file should contain a single directory called `micromissiles-<version>-windows_x86_64`.
3. In the `micromissiles-<version>-windows_x86_64` directory, run `micromissiles-<version>-StandaloneWindows64.exe`.

### Mac

1. Download the tarball file for Darwin: `micromissiles-<version>-darwin.tar.gz`.
2. Untar the tarball. The tarball should contain a single directory called `micromissiles-<version>-darwin`.
3. In the `micromissiles-<version>-darwin` directory, run the app file.
4. If you get a warning that Apple cannot check the application for malicious software:
   * Open `System Preferences`.
   * Navigate to `Privacy & Security`.
   * Click on `Open Anyway` to bypass Apple's developer check.

## Linux

1. Download the tarball for Linux.
2. Untar the tarball and run the executable.

## Basics

- Use the mouse and arrow keys (or `WASD`) to pan and rotate the camera.
- Use `L` to load a new simulation configuration.
- Use `R` to restart the simulation.
- Use `SPACEBAR` to pause/resume the simulation.
- Use `ESC` to exit the application.

## Next Steps

- Read the [**Simulation Overview**](Simulator_Overview.md) to understand the simulation physics fidelity, agent behaviors, and assumptions.
- Familiarize yourself with the [**Keybinds and Controls**](Keybinds_and_Controls.md) to navigate and interact with the simulation.
- Learn how to configure the simulation settings by reading the [**Simulation Configuration Guide**](Simulation_Config_Guide.md).
