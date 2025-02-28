# micromissiles-unity

![Sim Salvo Animation](docs/images/sim_salvo_animation.gif)

[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/PisterLab/micromissiles-unity/build.yaml?link=https%3A%2F%2Fgithub.com%2FPisterLab%2Fmicromissiles-unity%2Factions%2Fworkflows%2Fbuild.yaml)](https://github.com/PisterLab/micromissiles-unity/actions/workflows/build.yaml)
[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/PisterLab/micromissiles-unity/test.yaml?label=tests&link=https%3A%2F%2Fgithub.com%2FPisterLab%2Fmicromissiles-unity%2Factions%2Fworkflows%2Ftest.yaml)](https://github.com/PisterLab/micromissiles-unity/actions/workflows/test.yaml)
[![GitHub Release](https://img.shields.io/github/v/release/PisterLab/micromissiles-unity?link=https%3A%2F%2Fgithub.com%2FPisterLab%2Fmicromissiles-unity%2Freleases%2Flatest)](https://github.com/PisterLab/micromissiles-unity/releases/latest)
[![Static Badge](https://img.shields.io/badge/%F0%9F%93%93-Documentation-blue?labelColor=white)](https://pisterlab.github.io/micromissiles-unity/)

# Documentation

Documentation is hosted on the [micromissiles-unity GitHub Pages site](https://pisterlab.github.io/micromissiles-unity/).

# Quick Start

We generate pre-built standalone binaries for Windows and Mac users from the `release` branch. These binaries are intended for non-development users who just want to run the application and modify a few configurations along the way.

You can find the latest release [here](https://github.com/PisterLab/micromissiles-unity/releases/latest).

## Windows

1. Download the zip file for Windows: `micromissiles-<version>-windows-x86_64.zip`.
2. Unzip the zip file. The zip file should contain a single directory called `micromissiles-<version>-windows-x86_64`.
3. In the `micromissiles-<version>-windows-x86_64` directory, run `micromissiles-<version>-StandaloneWindows64.exe`.

## Mac

1. Download the tarball file for Darwin: `micromissiles-<version>-darwin-x86_64.tar.gz`.
2. Untar the tarball. The tarball should contain a single directory called `micromissiles-<version>-darwin-x86_64`.
3. In the `micromissiles-<version>-darwin-x86_64` directory, run the app file.
4. If you get a warning that Apple cannot check the application for malicious software:
     * Open `System Preferences`.
     * Navigate to `Privacy & Security`.
     * Click on `Open Anyway` to bypass Apple's developer check.

# Next Steps

- To get started with development, see the [**Development Guide**](https://pisterlab.github.io/micromissiles-unity/Development_Guide.html).
- Familiarize yourself with the [**Keybinds and Controls**](https://pisterlab.github.io/micromissiles-unity/Keybinds_and_Controls.html) to navigate and interact with the simulation.
- Learn how to configure the simulation settings by reading the [**Simulation Configuration Guide**](https://pisterlab.github.io/micromissiles-unity/Simulation_Config_Guide.html).
- Learn how to analyze the simulation logs by reading the [**Simulation Logging Guide**](https://pisterlab.github.io/micromissiles-unity/Simulation_Logging.html).